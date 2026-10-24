using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Federation;

public class FederationServiceTests
{
  private readonly Mock<IFederationPeerRepository> peers = new();
  private readonly Mock<IFederationJobRepository> jobs = new();
  private readonly Mock<IFederationInvitationRepository> invitations = new();
  private readonly Mock<IFederationClient> client = new();
  private readonly Mock<IFederationAssertionService> assertions = new();
  private readonly Mock<IFederationKeyStore> keyStore = new();
  private readonly Mock<IFederationAuditLogger> audit = new();
  private readonly Mock<IFederationNotifier> notifier = new();
  private readonly Mock<IRecordRepository> records = new();
  private readonly Mock<IRecordService> recordService = new();
  private readonly Mock<IFederationUploadStore> uploadStore = new();

  private FederationService CreateService() => new(
    new FederationOptions { PublicUrl = "https://alpha.example" },
    peers.Object,
    jobs.Object,
    invitations.Object,
    client.Object,
    assertions.Object,
    keyStore.Object,
    audit.Object,
    notifier.Object,
    records.Object,
    recordService.Object,
    uploadStore.Object,
    new MemoryCache(new MemoryCacheOptions()));

  private Peer AddPeer(PeerState state)
  {
    var peer = new Peer
    {
      PeerId = Guid.NewGuid(),
      Instance = "beta",
      BaseUrl = "https://beta.example",
      Endpoint = $"https://beta.example{FederationOptions.EndpointPath}",
      JwksUri = $"https://beta.example{FederationOptions.EndpointPath}/jwks",
      Jwks = "{\"keys\":[]}",
      State = state,
      RequestId = Guid.NewGuid(),
    };
    peers.Setup(p => p.Get(peer.PeerId)).Returns(peer);
    peers.Setup(p => p.GetByInstance(peer.Instance)).Returns(peer);
    return peer;
  }

  private void PartnerResponds(HttpStatusCode status)
  {
    client
      .Setup(c => c.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<HttpContent?>(), It.IsAny<IDictionary<string, object>?>()))
      .ReturnsAsync(() => new HttpResponseMessage(status));
  }

  private void PartnerUnreachable()
  {
    client
      .Setup(c => c.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<HttpContent?>(), It.IsAny<IDictionary<string, object>?>()))
      .ThrowsAsync(new FederationException(FederationErrors.RemoteUnreachable));
  }

  private void AssertionIsValid(Peer peer, IDictionary<string, object> claims)
  {
    assertions.Setup(a => a.ReadClaim("assertion", "iss")).Returns(peer.Instance);
    assertions.Setup(a => a.Validate("assertion", peer.Instance, peer.Jwks)).ReturnsAsync(new AssertionResult(true, false, claims));
  }

  [Fact]
  public async Task Approve_LinksPartnerWhenItIsInformed()
  {
    var peer = AddPeer(PeerState.PendingApproval);
    PartnerResponds(HttpStatusCode.OK);

    var result = await CreateService().Approve(peer.PeerId);

    Assert.Equal(PeerState.Linked, result.State);
    jobs.Verify(j => j.Enqueue(It.IsAny<Guid>(), It.IsAny<FederationJobType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()), Times.Never);
  }

  [Fact]
  public async Task Approve_RetriesAnswerWhenPartnerIsUnreachable()
  {
    var peer = AddPeer(PeerState.PendingApproval);
    PartnerUnreachable();

    var result = await CreateService().Approve(peer.PeerId);

    Assert.Equal(PeerState.ResponsePending, result.State);
    jobs.Verify(j => j.Enqueue(peer.PeerId, FederationJobType.RegistrationResponse, null, null), Times.Once);
  }

  [Theory]
  [InlineData(PeerState.Requested)]
  [InlineData(PeerState.Linked)]
  public async Task Approve_RejectsPartnerNotWaitingForApproval(PeerState state)
  {
    var peer = AddPeer(state);

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().Approve(peer.PeerId));

    Assert.Equal(FederationErrors.InvalidState, exception.Code);
  }

  [Fact]
  public async Task Reject_RemovesRequestEvenIfPartnerIsUnreachable()
  {
    var peer = AddPeer(PeerState.PendingApproval);
    PartnerUnreachable();

    await CreateService().Reject(peer.PeerId);

    peers.Verify(p => p.Delete(peer.PeerId), Times.Once);
    audit.Verify(a => a.Log(AuditAction.RegistrationRejected, AuditStatus.Failed, TestEnvironment.Instance, peer.Instance, null, null, It.IsAny<string?>()), Times.Once);
  }

  [Theory]
  [InlineData(true, PeerState.Linked)]
  [InlineData(false, PeerState.Rejected)]
  public async Task HandleRegistrationResponse_UpdatesRequestedPartner(bool accepted, PeerState expected)
  {
    var peer = AddPeer(PeerState.Requested);
    AssertionIsValid(peer, new Dictionary<string, object> { ["request_id"] = peer.RequestId.ToString(), ["accepted"] = accepted });

    await CreateService().HandleRegistrationResponse("assertion");

    peers.Verify(p => p.Update(It.Is<Peer>(u => u.PeerId == peer.PeerId && u.State == expected)), Times.Once);
  }

  [Fact]
  public async Task HandleRegistrationResponse_RejectsUnknownRequest()
  {
    var peer = AddPeer(PeerState.Requested);
    AssertionIsValid(peer, new Dictionary<string, object> { ["request_id"] = Guid.NewGuid().ToString(), ["accepted"] = true });

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().HandleRegistrationResponse("assertion"));

    Assert.Equal(FederationErrors.InvalidAssertion, exception.Code);
    peers.Verify(p => p.Update(It.IsAny<Peer>()), Times.Never);
  }

  [Fact]
  public async Task HandleRegistrationResponse_RejectsPartnerNotRequested()
  {
    var peer = AddPeer(PeerState.Linked);
    AssertionIsValid(peer, new Dictionary<string, object> { ["request_id"] = peer.RequestId.ToString(), ["accepted"] = true });

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().HandleRegistrationResponse("assertion"));

    Assert.Equal(FederationErrors.InvalidAssertion, exception.Code);
  }

  private const string InvitationCode = "ABCDE-FGHJK-MNPQR-STVWX";

  private void RequestHasOpenInvitation(string instance, string baseUrl)
  {
    assertions.Setup(a => a.ReadClaim("assertion", "iss")).Returns(instance);
    assertions.Setup(a => a.ReadClaim("assertion", "base_url")).Returns(baseUrl);
    assertions.Setup(a => a.ReadClaim("assertion", "invitation")).Returns(InvitationCode);
    invitations.Setup(i => i.IsOpen(FederationInvitationCode.Hash(InvitationCode)!, It.IsAny<DateTime>())).Returns(true);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("ABCDE-FGHJK-MNPQR-STVWX")]
  public async Task HandleRegistrationRequest_RejectsRequestWithoutOpenInvitationBeforeContactingPartner(string? invitation)
  {
    assertions.Setup(a => a.ReadClaim("assertion", "iss")).Returns("beta");
    assertions.Setup(a => a.ReadClaim("assertion", "base_url")).Returns("https://beta.example");
    assertions.Setup(a => a.ReadClaim("assertion", "invitation")).Returns(invitation);
    invitations.Setup(i => i.IsOpen(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(false);

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().HandleRegistrationRequest("assertion"));

    Assert.Equal(FederationErrors.InvalidInvitation, exception.Code);
    client.Verify(c => c.GetDiscovery(It.IsAny<string>()), Times.Never);
    client.Verify(c => c.GetJwks(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task HandleRegistrationRequest_RedeemsInvitationOnce()
  {
    RequestHasOpenInvitation("beta", "https://beta.example");
    var discovery = new FederationDiscovery("beta", FederationOptions.ProtocolVersion, $"https://beta.example{FederationOptions.EndpointPath}", $"https://beta.example{FederationOptions.EndpointPath}/jwks");
    client.Setup(c => c.GetDiscovery("https://beta.example")).ReturnsAsync(discovery);
    client.Setup(c => c.GetJwks(discovery.JwksUri)).ReturnsAsync("keys");
    assertions.Setup(a => a.Validate("assertion", "beta", "keys")).ReturnsAsync(new AssertionResult(true, false, new Dictionary<string, object> { ["request_id"] = Guid.NewGuid().ToString() }));
    peers.Setup(p => p.Create(It.IsAny<Peer>())).ReturnsAsync((Peer p) => p);
    invitations.SetupSequence(i => i.Redeem(FederationInvitationCode.Hash(InvitationCode)!, It.IsAny<DateTime>(), "beta")).ReturnsAsync(true).ReturnsAsync(false);

    await CreateService().HandleRegistrationRequest("assertion");
    peers.Verify(p => p.Create(It.Is<Peer>(n => n.Instance == "beta" && n.State == PeerState.PendingApproval)), Times.Once);

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().HandleRegistrationRequest("assertion"));
    Assert.Equal(FederationErrors.InvalidInvitation, exception.Code);
    peers.Verify(p => p.Create(It.IsAny<Peer>()), Times.Once);
  }

  [Theory]
  [InlineData("")]
  [InlineData("too-short")]
  [InlineData("ABCDE-FGHJK-MNPQR-STVWU")]
  public async Task RequestRegistration_RejectsInvalidInvitationCodeWithoutContactingPartner(string code)
  {
    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().RequestRegistration("https://beta.example", code));

    Assert.Equal(FederationErrors.InvalidInvitation, exception.Code);
    client.Verify(c => c.GetDiscovery(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task CreateInvitation_StoresOnlyHashOfCodeAndReturnsOwnAddress()
  {
    invitations.Setup(i => i.Create(It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(new Invitation { InvitationId = Guid.NewGuid() });

    var invitation = await CreateService().CreateInvitation(72);

    Assert.NotNull(invitation.Code);
    Assert.Equal("https://alpha.example", invitation.BaseUrl);
    invitations.Verify(i => i.Create(FederationInvitationCode.Hash(invitation.Code)!,
      It.Is<DateTime>(d => d > DateTime.UtcNow.AddHours(71) && d <= DateTime.UtcNow.AddHours(72))), Times.Once);
    invitations.Verify(i => i.DeleteClosed(It.IsAny<DateTime>()), Times.Once);
  }

  [Fact]
  public async Task HandleRegistrationRequest_RejectsOwnInstanceName()
  {
    RequestHasOpenInvitation(TestEnvironment.Instance.ToUpperInvariant(), "https://evil.example");

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().HandleRegistrationRequest("assertion"));

    Assert.Equal(FederationErrors.InstanceConflict, exception.Code);
    client.Verify(c => c.GetDiscovery(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task HandleRegistrationRequest_DoesNotReplacePendingRequestOfOtherUrl()
  {
    var peer = AddPeer(PeerState.PendingApproval);
    RequestHasOpenInvitation(peer.Instance, "https://evil.example");

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().HandleRegistrationRequest("assertion"));

    Assert.Equal(FederationErrors.InstanceConflict, exception.Code);
    peers.Verify(p => p.Update(It.IsAny<Peer>()), Times.Never);
  }

  [Theory]
  [InlineData("beta", true)]
  [InlineData("mml.home-01_test", true)]
  [InlineData("", false)]
  [InlineData("with space", false)]
  [InlineData("<script>", false)]
  [InlineData("a/b", false)]
  public void IsValidInstanceName_AllowsOnlySafeNames(string instance, bool expected)
  {
    Assert.Equal(expected, FederationOptions.IsValidInstanceName(instance));
  }

  [Fact]
  public async Task Unlink_KeepsRegistrationWhenPartnerIsUnreachable()
  {
    var peer = AddPeer(PeerState.Linked);
    PartnerUnreachable();

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().Unlink(peer.PeerId, deleteRecords: true, force: false));

    Assert.Equal(FederationErrors.RemoteUnreachable, exception.Code);
    peers.Verify(p => p.Delete(It.IsAny<Guid>()), Times.Never);
    recordService.Verify(r => r.DeleteRecord(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
  }

  [Fact]
  public async Task Unlink_RemovesLocallyWhenForced()
  {
    var peer = AddPeer(PeerState.Linked);
    var recordIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
    records.Setup(r => r.GetRecordIdsByOwner(peer.Instance)).Returns(recordIds);
    PartnerUnreachable();

    await CreateService().Unlink(peer.PeerId, deleteRecords: true, force: true);

    peers.Verify(p => p.Delete(peer.PeerId), Times.Once);
    uploadStore.Verify(u => u.RemoveAll(peer.PeerId), Times.Once);
    foreach (var recordId in recordIds)
    {
      recordService.Verify(r => r.DeleteRecord(recordId, true), Times.Once);
    }
  }

  [Fact]
  public async Task Unlink_KeepsRecordsWhenNotRequested()
  {
    var peer = AddPeer(PeerState.Linked);
    PartnerResponds(HttpStatusCode.OK);

    await CreateService().Unlink(peer.PeerId, deleteRecords: false, force: false);

    peers.Verify(p => p.Delete(peer.PeerId), Times.Once);
    records.Verify(r => r.GetRecordIdsByOwner(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task HandleUnlink_RemovesRecordsOfPartnerOnlyWhenRequested()
  {
    var peer = AddPeer(PeerState.Linked);
    var recordId = Guid.NewGuid();
    records.Setup(r => r.GetRecordIdsByOwner(peer.Instance)).Returns([recordId]);

    await CreateService().HandleUnlink(peer, deleteOwnerRecords: false);
    recordService.Verify(r => r.DeleteRecord(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);

    await CreateService().HandleUnlink(peer, deleteOwnerRecords: true);
    recordService.Verify(r => r.DeleteRecord(recordId, true), Times.Once);
  }

  [Fact]
  public async Task Authenticate_ReloadsKeysOnceForUnknownKey()
  {
    var peer = AddPeer(PeerState.Linked);
    assertions.Setup(a => a.ReadClaim("assertion", "iss")).Returns(peer.Instance);
    assertions.Setup(a => a.Validate("assertion", peer.Instance, peer.Jwks)).ReturnsAsync(AssertionResult.Failed(unknownKey: true));
    assertions.Setup(a => a.Validate("assertion", peer.Instance, "new-keys")).ReturnsAsync(new AssertionResult(true, false, new Dictionary<string, object>()));
    client.Setup(c => c.GetJwks(peer.JwksUri)).ReturnsAsync("new-keys");

    var service = CreateService();
    var result = await service.Authenticate("assertion", PeerState.Linked);

    Assert.NotNull(result);
    Assert.Equal("new-keys", result.Value.Peer.Jwks);
    audit.Verify(a => a.Log(AuditAction.KeysChanged, AuditStatus.Success, peer.Instance, TestEnvironment.Instance, null, null, It.IsAny<string?>()), Times.Once);

    // further unknown keys within one minute do not reload the keys again
    peer.Jwks = "{\"keys\":[]}";
    await service.Authenticate("assertion", PeerState.Linked);
    client.Verify(c => c.GetJwks(peer.JwksUri), Times.Once);
  }

  [Fact]
  public async Task Authenticate_RejectsPartnerInOtherState()
  {
    var peer = AddPeer(PeerState.Requested);
    AssertionIsValid(peer, new Dictionary<string, object>());

    Assert.Null(await CreateService().Authenticate("assertion", PeerState.Linked));
  }

  [Fact]
  public async Task RotateKeys_InformsLinkedPartnersAboutNewKey()
  {
    var linked = AddPeer(PeerState.Linked);
    keyStore.Setup(k => k.Rotate(It.IsAny<DateTime>())).Returns(true);
    peers.Setup(p => p.ListByState(PeerState.Linked, PeerState.ResponsePending)).Returns([linked]);

    await CreateService().RotateKeys();

    jobs.Verify(j => j.Enqueue(linked.PeerId, FederationJobType.KeysChanged, null, null), Times.Once);
  }
}
