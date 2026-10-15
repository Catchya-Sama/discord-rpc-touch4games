using TouchRpc.Infrastructure;

namespace TouchRpc.Tests;

[TestClass]
public sealed class DiscordPresenceTests
{
    [TestMethod]
    public void MissingApplicationId_LeavesDiscordDisabledWithoutCreatingTransport()
    {
        var factoryCalled = false;
        using var service = new DiscordPresenceService(null, _ =>
        {
            factoryCalled = true;
            return new FakeDiscordTransport();
        });

        service.Publish(Payload("Waiting"));

        Assert.AreEqual(DiscordConnectionState.Disabled, service.State);
        Assert.IsFalse(factoryCalled);
    }

    [TestMethod]
    public void IdenticalPayload_IsPublishedOnlyOnce()
    {
        var transport = new FakeDiscordTransport();
        using var service = new DiscordPresenceService("123456789012345678", _ => transport);
        transport.RaiseConnected();
        var payload = Payload("Waiting");

        service.Publish(payload);
        service.Publish(payload);

        Assert.HasCount(1, transport.Published);
        Assert.AreEqual(payload, transport.Published[0]);
    }

    [TestMethod]
    public void Reconnect_PublishesLatestStateOnly()
    {
        var transport = new FakeDiscordTransport();
        using var service = new DiscordPresenceService("123456789012345678", _ => transport);
        service.Publish(Payload("Waiting"));
        service.Publish(Payload("Spectating"));

        transport.RaiseConnected();

        Assert.HasCount(1, transport.Published);
        Assert.AreEqual("Spectating", transport.Published[0].State);
    }

    [TestMethod]
    public void DisconnectThenReconnect_ResendsLatestStateWithoutBacklog()
    {
        var transport = new FakeDiscordTransport();
        using var service = new DiscordPresenceService("123456789012345678", _ => transport);
        transport.RaiseConnected();
        service.Publish(Payload("Waiting"));
        transport.RaiseDisconnected();
        service.Publish(Payload("Smashing Key"));

        transport.RaiseConnected();

        Assert.HasCount(2, transport.Published);
        Assert.AreEqual("Waiting", transport.Published[0].State);
        Assert.AreEqual("Smashing Key", transport.Published[1].State);
    }

    [TestMethod]
    public void Clear_RemovesPendingStateAndClearsConnectedTransport()
    {
        var transport = new FakeDiscordTransport();
        using var service = new DiscordPresenceService("123456789012345678", _ => transport);
        transport.RaiseConnected();
        service.Publish(Payload("Waiting"));

        service.Clear();
        transport.RaiseDisconnected();
        transport.RaiseConnected();

        Assert.AreEqual(1, transport.ClearCount);
        Assert.HasCount(1, transport.Published);
    }

    [TestMethod]
    public void Payload_PreservesPreparedNicknameTooltip()
    {
        var transport = new FakeDiscordTransport();
        using var service = new DiscordPresenceService("123456789012345678", _ => transport);
        transport.RaiseConnected();

        service.Publish(new DiscordPresencePayload("2 Players Joined", "Spectating", "touch", "Synthetic Nickname"));

        Assert.AreEqual("touch", transport.Published.Single().LargeImageKey);
        Assert.AreEqual("Synthetic Nickname", transport.Published.Single().LargeImageText);
    }

    [TestMethod]
    public void ConnectionEvents_UpdateSeparateDiscordStatus()
    {
        var transport = new FakeDiscordTransport();
        using var service = new DiscordPresenceService("123456789012345678", _ => transport);
        var observed = new List<DiscordConnectionState>();
        service.StatusChanged += (_, args) => observed.Add(args.State);

        transport.RaiseConnected();
        transport.RaiseDisconnected();
        transport.RaiseError("Synthetic RPC failure");

        CollectionAssert.AreEqual(
            new[] { DiscordConnectionState.Connected, DiscordConnectionState.Disconnected, DiscordConnectionState.Error },
            observed);
        Assert.AreEqual("Synthetic RPC failure", service.Diagnostic);
    }

    [TestMethod]
    public void InitialConnectionFailure_KeepsServiceAvailableForAutomaticReconnect()
    {
        var transport = new FakeDiscordTransport { StartResult = false };
        using var service = new DiscordPresenceService("123456789012345678", _ => transport);

        service.Publish(Payload("Waiting"));
        transport.RaiseConnected();

        Assert.AreEqual(DiscordConnectionState.Connected, service.State);
        Assert.HasCount(1, transport.Published);
        Assert.AreEqual("Waiting", transport.Published[0].State);
    }

    [TestMethod]
    public void TransportPublishFailure_DoesNotEscapeIntoMonitoringCaller()
    {
        var transport = new FakeDiscordTransport { ThrowOnPublish = true };
        using var service = new DiscordPresenceService("123456789012345678", _ => transport);
        transport.RaiseConnected();

        service.Publish(Payload("Waiting"));

        Assert.AreEqual(DiscordConnectionState.Error, service.State);
        StringAssert.Contains(service.Diagnostic, "publish failed");
    }

    [TestMethod]
    public void Dispose_ClearsConnectedPresence()
    {
        var transport = new FakeDiscordTransport();
        var service = new DiscordPresenceService("123456789012345678", _ => transport);
        transport.RaiseConnected();
        service.Publish(Payload("Waiting"));

        service.Dispose();

        Assert.AreEqual(1, transport.ClearCount);
    }

    private static DiscordPresencePayload Payload(string state) =>
        new("2 Players Joined", state, "touch", "Synthetic Nickname");

    private sealed class FakeDiscordTransport : IDiscordRpcTransport
    {
        public event EventHandler? Connected;
        public event EventHandler? Disconnected;
        public event EventHandler<string>? Error;

        public List<DiscordPresencePayload> Published { get; } = [];
        public int ClearCount { get; private set; }
        public bool StartResult { get; init; } = true;
        public bool ThrowOnPublish { get; init; }

        public bool Start() => StartResult;
        public void SetPresence(DiscordPresencePayload payload)
        {
            if (ThrowOnPublish) throw new InvalidOperationException("Synthetic transport failure");
            Published.Add(payload);
        }
        public void ClearPresence() => ClearCount++;
        public void Dispose() { }
        public void RaiseConnected() => Connected?.Invoke(this, EventArgs.Empty);
        public void RaiseDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);
        public void RaiseError(string message) => Error?.Invoke(this, message);
    }
}