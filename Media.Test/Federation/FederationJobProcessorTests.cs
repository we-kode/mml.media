using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Federation;

public class FederationJobProcessorTests
{
  private readonly Mock<IFederationJobRepository> jobs = new();
  private readonly Mock<IFederationPeerRepository> peers = new();
  private readonly Mock<IFederationSyncService> sync = new();
  private readonly Mock<IFederationAuditLogger> audit = new();
  private readonly Peer peer = new() { PeerId = Guid.NewGuid(), Instance = "beta", State = PeerState.Linked };

  private FederationJobProcessor CreateProcessor() => new(
    jobs.Object, peers.Object, new Mock<IFederationClient>().Object, new Mock<IFederationService>().Object, sync.Object, audit.Object);

  private FederationJob Due(FederationJobType type, int attempts = 0)
  {
    var job = new FederationJob { JobId = Guid.NewGuid(), PeerId = peer.PeerId, Type = type, RecordId = Guid.NewGuid(), Attempts = attempts };
    peers.Setup(p => p.Get(peer.PeerId)).Returns(peer);
    jobs.Setup(j => j.Due(It.IsAny<DateTime>(), It.IsAny<int>())).Returns(new List<FederationJob> { job });
    return job;
  }

  [Fact]
  public async Task ProcessDue_RemovesSuccessfulOperation()
  {
    var job = Due(FederationJobType.PushRecord);
    sync.Setup(s => s.Execute(job, peer)).ReturnsAsync(FederationJobResult.Success);

    await CreateProcessor().ProcessDue(DateTime.UtcNow);

    jobs.Verify(j => j.Complete(job.JobId), Times.Once);
    jobs.Verify(j => j.Fail(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task ProcessDue_KeepsFailedOperationOfRecord()
  {
    var job = Due(FederationJobType.PushRecord);
    sync.Setup(s => s.Execute(job, peer)).ReturnsAsync(FederationJobResult.Failed("too large"));

    await CreateProcessor().ProcessDue(DateTime.UtcNow);

    jobs.Verify(j => j.Fail(job.JobId, "too large"), Times.Once);
    jobs.Verify(j => j.Complete(It.IsAny<Guid>()), Times.Never);
    audit.Verify(a => a.Log(AuditAction.RecordPushed, AuditStatus.Failed, It.IsAny<string>(), "beta", It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>?>(), "too large"), Times.Once);
  }

  [Fact]
  public async Task ProcessDue_KeepsOperationAfterLastAttempt()
  {
    var job = Due(FederationJobType.UpdateRecord, attempts: 29);
    sync.Setup(s => s.Execute(job, peer)).ReturnsAsync(FederationJobResult.Retry("unreachable"));

    await CreateProcessor().ProcessDue(DateTime.UtcNow);

    jobs.Verify(j => j.Fail(job.JobId, It.Is<string>(e => e.Contains("unreachable"))), Times.Once);
    jobs.Verify(j => j.Retry(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
  }

  [Fact]
  public async Task ProcessDue_RemovesFailedDeletion()
  {
    var job = Due(FederationJobType.DeleteRecord);
    sync.Setup(s => s.Execute(job, peer)).ReturnsAsync(FederationJobResult.Failed("gone"));

    await CreateProcessor().ProcessDue(DateTime.UtcNow);

    jobs.Verify(j => j.Complete(job.JobId), Times.Once);
    jobs.Verify(j => j.Fail(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task ProcessDue_RetriesTemporaryError()
  {
    var job = Due(FederationJobType.PushRecord, attempts: 2);
    sync.Setup(s => s.Execute(job, peer)).ReturnsAsync(FederationJobResult.Retry("unreachable"));

    await CreateProcessor().ProcessDue(DateTime.UtcNow);

    jobs.Verify(j => j.Retry(job.JobId, "unreachable", It.IsAny<DateTime>()), Times.Once);
    jobs.Verify(j => j.Fail(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    audit.Verify(a => a.Log(It.IsAny<AuditAction>(), It.IsAny<AuditStatus>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>?>(), It.IsAny<string?>()), Times.Never);
  }

  [Fact]
  public async Task ProcessDue_LogsFirstFailedAttempt()
  {
    var job = Due(FederationJobType.PushRecord);
    sync.Setup(s => s.Execute(job, peer)).ReturnsAsync(FederationJobResult.Retry("unreachable"));

    await CreateProcessor().ProcessDue(DateTime.UtcNow);

    jobs.Verify(j => j.Retry(job.JobId, "unreachable", It.IsAny<DateTime>()), Times.Once);
    audit.Verify(a => a.Log(AuditAction.RecordPushed, AuditStatus.Failed, It.IsAny<string>(), "beta", It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>?>(), It.Is<string?>(m => m!.StartsWith("unreachable"))), Times.Once);
  }

  [Fact]
  public async Task ProcessDue_UpdatesEntryOfOperationOnEveryFailedAttempt()
  {
    var job = Due(FederationJobType.UpdateRecord, attempts: 3);
    job.AuditEntryId = Guid.NewGuid();
    sync.Setup(s => s.Execute(job, peer)).ReturnsAsync(FederationJobResult.Retry("unreachable"));

    await CreateProcessor().ProcessDue(DateTime.UtcNow);

    audit.Verify(a => a.Log(AuditAction.RecordUpdated, AuditStatus.Failed, It.IsAny<string>(), "beta", It.IsAny<Guid?>(), It.IsAny<IEnumerable<Guid>?>(), It.IsAny<string?>(), job.AuditEntryId), Times.Once);
  }

  [Fact]
  public async Task ProcessDue_ResolvesPendingEntryOfUnnecessaryOperation()
  {
    var job = Due(FederationJobType.PushRecord);
    job.AuditEntryId = Guid.NewGuid();
    sync.Setup(s => s.Execute(job, peer)).ReturnsAsync(FederationJobResult.Success);

    await CreateProcessor().ProcessDue(DateTime.UtcNow);

    audit.Verify(a => a.Resolve(job.AuditEntryId, It.IsAny<string>()), Times.Once);
  }
}
