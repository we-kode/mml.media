using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.Infrastructure.Services;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Federation;

public class FederationAuditLoggerTests
{
  private readonly Mock<IFederationAuditRepository> repository = new();
  private readonly Mock<IFederationNotifier> notifier = new();

  private FederationAuditLogger CreateLogger() => new(repository.Object, notifier.Object);

  private void Stored(Guid entryId, AuditStatus previous, AuditStatus status) =>
    repository.Setup(r => r.Update(entryId, status, It.IsAny<string?>(), It.IsAny<bool>()))
      .ReturnsAsync((new AuditEntry { EntryId = entryId, Status = status }, previous));

  [Fact]
  public async Task Log_UpdatesEntryOfPendingOperation()
  {
    var entryId = Guid.NewGuid();
    Stored(entryId, AuditStatus.Pending, AuditStatus.Success);

    await CreateLogger().Log(AuditAction.RecordPushed, AuditStatus.Success, "alpha", "beta", entryId: entryId);

    repository.Verify(r => r.Add(It.IsAny<AuditEntry>()), Times.Never);
    notifier.Verify(n => n.AuditAdded(It.Is<AuditEntry>(e => e.EntryId == entryId)), Times.Once);
  }

  [Fact]
  public async Task Log_DoesNotNotifyAgainAboutRepeatedFailure()
  {
    var entryId = Guid.NewGuid();
    Stored(entryId, AuditStatus.Failed, AuditStatus.Failed);

    await CreateLogger().Log(AuditAction.RecordPushed, AuditStatus.Failed, "alpha", "beta", message: "again", entryId: entryId);

    notifier.Verify(n => n.AuditAdded(It.IsAny<AuditEntry>()), Times.Never);
  }

  [Fact]
  public async Task Log_AddsEntryIfEntryOfOperationWasRemoved()
  {
    repository.Setup(r => r.Add(It.IsAny<AuditEntry>())).ReturnsAsync((AuditEntry e) => e);

    await CreateLogger().Log(AuditAction.RecordPushed, AuditStatus.Success, "alpha", "beta", entryId: Guid.NewGuid());

    repository.Verify(r => r.Add(It.Is<AuditEntry>(e => e.Status == AuditStatus.Success)), Times.Once);
  }

  [Fact]
  public async Task Resolve_SkipsOnlyPendingEntries()
  {
    var entryId = Guid.NewGuid();

    await CreateLogger().Resolve(entryId, "not necessary");
    await CreateLogger().Resolve(null, "not necessary");

    repository.Verify(r => r.Update(entryId, AuditStatus.Skipped, "not necessary", true), Times.Once);
    repository.Verify(r => r.Update(It.IsAny<Guid>(), It.IsAny<AuditStatus>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Once);
  }
}
