namespace TouchRpc.Infrastructure;

public enum LogSourceHealth
{
    Missing,
    Unreadable,
    ValidWaiting,
    Receiving,
    GameClosed,
}

public sealed record LogReadBatch(
    LogSourceHealth Health,
    bool NewSession,
    IReadOnlyList<string> Lines,
    string Diagnostic);