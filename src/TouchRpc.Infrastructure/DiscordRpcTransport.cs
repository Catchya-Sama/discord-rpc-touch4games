using DiscordRPC;

namespace TouchRpc.Infrastructure;

internal sealed class DiscordRpcTransport : IDiscordRpcTransport
{
    private readonly DiscordRpcClient _client;

    public DiscordRpcTransport(string applicationId)
    {
        _client = new DiscordRpcClient(applicationId)
        {
            SkipIdenticalPresence = true,
        };
        _client.OnReady += (_, _) => Connected?.Invoke(this, EventArgs.Empty);
        _client.OnConnectionFailed += (_, _) => Disconnected?.Invoke(this, EventArgs.Empty);
        _client.OnClose += (_, _) => Disconnected?.Invoke(this, EventArgs.Empty);
        _client.OnError += (_, args) => Error?.Invoke(this, $"Discord RPC error: {args.Message}");
    }

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<string>? Error;

    public bool Start() => _client.Initialize();

    public void SetPresence(DiscordPresencePayload payload) => _client.SetPresence(new RichPresence
    {
        Details = payload.Details,
        State = string.IsNullOrWhiteSpace(payload.State) ? null : payload.State,
        Assets = string.IsNullOrWhiteSpace(payload.LargeImageKey)
            ? null
            : new Assets
            {
                LargeImageKey = payload.LargeImageKey,
                LargeImageText = payload.LargeImageText,
            },
    });

    public void ClearPresence() => _client.ClearPresence();

    public void Dispose() => _client.Dispose();
}