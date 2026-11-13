using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.Infrastructure.IO;
using Media.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
  private readonly Mock<ISettingRepository> settings = new();

  private FederationService CreateService(IFederationAssertionService? assertionService = null) => new(
    new FederationOptions { PublicUrl = "https://alpha.example" },
    peers.Object,
    jobs.Object,
    invitations.Object,
    client.Object,
    assertionService ?? assertions.Object,
    keyStore.Object,
    audit.Object,
    notifier.Object,
    records.Object,
    recordService.Object,
    uploadStore.Object,
    settings.Object,
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

  [Theory]
  [InlineData("https://old.example", true)]
  [InlineData("https://ALPHA.example", false)]
  [InlineData("", false)]
  public async Task AnnounceAddress_InformsPartnersOnlyWhenPublishedUrlChanged(string published, bool informed)
  {
    var linked = AddPeer(PeerState.Linked);
    settings.Setup(s => s.Get("FederationPublishedUrl", string.Empty)).Returns(published);
    peers.Setup(p => p.ListByState(PeerState.Requested, PeerState.ResponsePending, PeerState.Linked)).Returns([linked]);

    await CreateService().AnnounceAddress();

    jobs.Verify(j => j.Enqueue(linked.PeerId, FederationJobType.AddressChanged, null, null), informed ? Times.Once : Times.Never);
    settings.Verify(s => s.Save("FederationPublishedUrl", "https://alpha.example"), published == "https://ALPHA.example" ? Times.Never : Times.Once);
  }

  private static FederationAssertionService CreateAssertions(string instance, out string jwks)
  {
    var store = new FederationKeyStore(new FederationOptions { PublicUrl = $"https://{instance}.example", KeysPath = TestEnvironment.CreateTempFolder() });
    jwks = store.GetPublicJwks();
    return new FederationAssertionService(store, new MemoryCache(new MemoryCacheOptions()));
  }

  // the server at the new address signs the nonce of the proof request with the given keys. Assertions are issued by
  // the instance of the test environment, so the partner has its name.
  private void PartnerMovesTo(string baseUrl, FederationAssertionService signer, string instance = TestEnvironment.Instance, bool otherNonce = false)
  {
    var endpoint = $"{baseUrl}{FederationOptions.EndpointPath}";
    client.Setup(c => c.GetDiscovery(baseUrl)).ReturnsAsync(new FederationDiscovery(instance, FederationOptions.ProtocolVersion, endpoint, $"{endpoint}/jwks"));
    client.Setup(c => c.GetJwks($"{endpoint}/jwks")).ReturnsAsync("new-jwks");
    client
      .Setup(c => c.Send(endpoint, TestEnvironment.Instance, HttpMethod.Post, "proof", It.IsAny<HttpContent?>(), null))
      .ReturnsAsync((string _, string _, HttpMethod _, string _, HttpContent? content, IDictionary<string, object>? _) =>
      {
        var nonce = otherNonce ? "other" : JsonDocument.Parse(content!.ReadAsStringAsync().Result).RootElement.GetProperty("nonce").GetString()!;
        var proof = signer.Create(TestEnvironment.Instance, new Dictionary<string, object> { ["nonce"] = nonce });
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { assertion = proof }) };
      });
  }

  private Peer AddPeerWithKeys(out FederationAssertionService partner)
  {
    var peer = AddPeer(PeerState.Linked);
    partner = CreateAssertions("beta", out var jwks);
    peer.Instance = TestEnvironment.Instance;
    peer.Jwks = jwks;
    return peer;
  }

  private FederationService CreateValidatingService() => CreateService(CreateAssertions("alpha", out _));

  [Fact]
  public async Task ChangeAddress_UpdatesPartnerWhichProvesItsKeyAndRetriesOperations()
  {
    var peer = AddPeerWithKeys(out var partner);
    PartnerMovesTo("https://beta.new.example", partner);

    var result = await CreateValidatingService().ChangeAddress(peer, "https://beta.new.example/");

    Assert.Equal("https://beta.new.example", result.BaseUrl);
    Assert.Equal($"https://beta.new.example{FederationOptions.EndpointPath}", result.Endpoint);
    Assert.Equal("new-jwks", result.Jwks);
    peers.Verify(p => p.Update(It.Is<Peer>(u => u.BaseUrl == "https://beta.new.example")), Times.Once);
    jobs.Verify(j => j.Reschedule(peer.PeerId, It.IsAny<DateTime>()), Times.Once);
    notifier.Verify(n => n.PeerChanged(peer), Times.Once);
  }

  [Theory]
  [InlineData(false, false)]
  [InlineData(true, true)]
  public async Task ChangeAddress_RejectsServerWithCopiedKeysOrOtherNonce(bool genuineKeys, bool otherNonce)
  {
    var peer = AddPeerWithKeys(out var partner);
    var signer = genuineKeys ? partner : CreateAssertions("beta", out _);
    PartnerMovesTo("https://evil.example", signer, otherNonce: otherNonce);

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateValidatingService().ChangeAddress(peer, "https://evil.example"));

    Assert.Equal(FederationErrors.UnknownKeys, exception.Code);
    peers.Verify(p => p.Update(It.IsAny<Peer>()), Times.Never);
    jobs.Verify(j => j.Reschedule(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
  }

  [Theory]
  [InlineData(HttpStatusCode.NotFound)]
  [InlineData(HttpStatusCode.OK)]
  public async Task ChangeAddress_RejectsPartnerWithoutValidProof(HttpStatusCode status)
  {
    var peer = AddPeerWithKeys(out var partner);
    PartnerMovesTo("https://beta.new.example", partner);
    client
      .Setup(c => c.Send(It.IsAny<string>(), TestEnvironment.Instance, HttpMethod.Post, "proof", It.IsAny<HttpContent?>(), null))
      .ReturnsAsync(() => new HttpResponseMessage(status) { Content = new StringContent("no json") });

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateValidatingService().ChangeAddress(peer, "https://beta.new.example"));

    Assert.Equal(FederationErrors.UnknownKeys, exception.Code);
  }

  [Fact]
  public async Task ChangeAddress_RejectsOtherInstance()
  {
    var peer = AddPeerWithKeys(out var partner);
    PartnerMovesTo("https://gamma.example", partner, instance: "gamma");

    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateValidatingService().ChangeAddress(peer, "https://gamma.example"));

    Assert.Equal(FederationErrors.InvalidDiscovery, exception.Code);
    client.Verify(c => c.Send(It.IsAny<string>(), It.IsAny<string>(), HttpMethod.Post, "proof", It.IsAny<HttpContent?>(), null), Times.Never);
  }

  [Fact]
  public void CreateProof_SignsNonceForPartnerAndRejectsInvalidValues()
  {
    var peer = AddPeer(PeerState.Linked);
    assertions.Setup(a => a.Create("beta", It.Is<IDictionary<string, object>>(c => (string)c["nonce"] == "abc"))).Returns("proof");

    Assert.Equal("proof", CreateService().CreateProof(peer, "abc"));
    Assert.Throws<FederationException>(() => CreateService().CreateProof(peer, ""));
    Assert.Throws<FederationException>(() => CreateService().CreateProof(peer, new string('a', 129)));
  }

  [Theory]
  [InlineData("http://beta.new.example")]
  [InlineData("https://user@beta.new.example")]
  public async Task ChangeAddress_RejectsInvalidUrl(string baseUrl)
  {
    var exception = await Assert.ThrowsAsync<FederationException>(() => CreateService().ChangeAddress(AddPeer(PeerState.Linked), baseUrl));

    Assert.Equal(FederationErrors.InvalidUrl, exception.Code);
    client.Verify(c => c.GetDiscovery(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task ChangeAddress_KeepsUnchangedAddress()
  {
    var peer = AddPeer(PeerState.Linked);

    await CreateService().ChangeAddress(peer, "https://beta.example");

    client.Verify(c => c.GetDiscovery(It.IsAny<string>()), Times.Never);
    peers.Verify(p => p.Update(It.IsAny<Peer>()), Times.Never);
  }

  [Fact]
  public async Task SendAddress_WaitsUntilPartnerReceivedApproval()
  {
    var peer = AddPeer(PeerState.ResponsePending);

    var result = await CreateService().SendAddress(peer);

    Assert.False(result.Done);
    client.VerifyNoOtherCalls();
  }

  [Fact]
  public async Task CompleteApproval_ExecutesWaitingOperations()
  {
    var peer = AddPeer(PeerState.ResponsePending);
    PartnerResponds(HttpStatusCode.OK);

    Assert.True(await CreateService().CompleteApproval(peer));

    Assert.Equal(PeerState.Linked, peer.State);
    jobs.Verify(j => j.Reschedule(peer.PeerId, It.IsAny<DateTime>()), Times.Once);
  }

  [Fact]
  public async Task SendAddress_SendsPublicUrl()
  {
    var peer = AddPeer(PeerState.Linked);
    string? body = null;
    client
      .Setup(c => c.Send(peer.Endpoint, peer.Instance, HttpMethod.Post, "address-changed", It.IsAny<HttpContent?>(), null))
      .Callback((string _, string _, HttpMethod _, string _, HttpContent? content, IDictionary<string, object>? _) => body = content!.ReadAsStringAsync().Result)
      .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

    var result = await CreateService().SendAddress(peer);

    Assert.Equal(FederationJobResult.Success, result);
    Assert.Equal("{\"baseUrl\":\"https://alpha.example\"}", body);
  }
}
