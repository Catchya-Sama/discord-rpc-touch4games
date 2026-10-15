namespace TouchRpc.Infrastructure;

public enum DiscordConnectionState
{
    Disabled,
    Connecting,
    Connected,
    Disconnected,
    Error,
}

public sealed record DiscordPresencePayload(
    string Details,
    string? State,
    string? LargeImageKey,
    string LargeImageText);

public sealed record DiscordStatusChangedEventArgs(
    DiscordConnectionState State,
    string Diagnostic);

public interface IDiscordRpcTransport : IDisposable
{
    event EventHandler? Connected;
    event EventHandler? Disconnected;
    event EventHandler<string>? Error;

    bool Start();
    void SetPresence(DiscordPresencePayload payload);
    void ClearPresence();
}

public sealed class DiscordPresenceService : IDisposable
{
    private readonly IDiscordRpcTransport? _transport;
    private DiscordPresencePayload? _latestPayload;
    private DiscordPresencePayload? _lastSentPayload;
    private bool _disposed;
    private bool _connected;

    public DiscordPresenceService(string? applicationId, Func<string, IDiscordRpcTransport>? transportFactory = null)
    {
        var normalizedApplicationId = AppSettingsStore.NormalizeDiscordApplicationId(applicationId);
        if (normalizedApplicationId is null)
        {
            State = string.IsNullOrWhiteSpace(applicationId)
                ? DiscordConnectionState.Disabled
                : DiscordConnectionState.Error;
            Diagnostic = string.IsNullOrWhiteSpace(applicationId)
                ? "Discord is disabled until an Application ID is configured."
                : "Discord Application ID must contain digits only.";
            return;
        }

        _transport = (transportFactory ?? (id => new DiscordRpcTransport(id)))(normalizedApplicationId);
        _transport.Connected += OnConnected;
        _transport.Disconnected += OnDisconnected;
        _transport.Error += OnError;
        State = DiscordConnectionState.Connecting;
        Diagnostic = "Connecting to Discord desktop...";

        try
        {
            if (!_transport.Start())
            {
                State = DiscordConnectionState.Disconnected;
                Diagnostic = "Discord desktop is unavailable; reconnect will be attempted automatically.";
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            State = DiscordConnectionState.Error;
            Diagnostic = $"Discord RPC could not start: {exception.Message}";
        }
    }

    public event EventHandler<DiscordStatusChangedEventArgs>? StatusChanged;

    public DiscordConnectionState State { get; private set; }
    public string Diagnostic { get; private set; }

    public void Publish(DiscordPresencePayload payload)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(payload);
        _latestPayload = payload;
        SendLatestIfNeeded();
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _latestPayload = null;
        _lastSentPayload = null;
        if (_connected) TryClearTransport();
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_connected) TryClearTransport();
        if (_transport is not null)
        {
            _transport.Connected -= OnConnected;
            _transport.Disconnected -= OnDisconnected;
            _transport.Error -= OnError;
            _transport.Dispose();
        }
        _disposed = true;
    }

    private void OnConnected(object? sender, EventArgs eventArgs)
    {
        _connected = true;
        _lastSentPayload = null;
        SetStatus(DiscordConnectionState.Connected, "Connected to Discord desktop.");
        SendLatestIfNeeded();
    }

    private void OnDisconnected(object? sender, EventArgs eventArgs)
    {
        _connected = false;
        _lastSentPayload = null;
        SetStatus(DiscordConnectionState.Disconnected, "Discord disconnected; waiting to reconnect.");
    }

    private void OnError(object? sender, string diagnostic) =>
        SetStatus(DiscordConnectionState.Error, diagnostic);

    private void SendLatestIfNeeded()
    {
        if (!_connected || _latestPayload is null || _latestPayload == _lastSentPayload) return;
        try
        {
            _transport?.SetPresence(_latestPayload);
            _lastSentPayload = _latestPayload;
        }
        catch (Exception exception)
        {
            SetStatus(DiscordConnectionState.Error, $"Discord RPC publish failed: {exception.Message}");
        }
    }

    private void TryClearTransport()
    {
        try
        {
            _transport?.ClearPresence();
        }
        catch (Exception exception)
        {
            SetStatus(DiscordConnectionState.Error, $"Discord RPC clear failed: {exception.Message}");
        }
    }

    private void SetStatus(DiscordConnectionState state, string diagnostic)
    {
        State = state;
        Diagnostic = diagnostic;
        StatusChanged?.Invoke(this, new DiscordStatusChangedEventArgs(state, diagnostic));
    }
}