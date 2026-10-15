using System.Diagnostics;

namespace TouchRpc.Core;

public interface IMonotonicClock
{
    long GetTimestamp();
    TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp);
}

public sealed class SystemMonotonicClock : IMonotonicClock
{
    public static SystemMonotonicClock Instance { get; } = new();

    private SystemMonotonicClock()
    {
    }

    public long GetTimestamp() => Stopwatch.GetTimestamp();

    public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
        Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp);
}

public static class AfkPolicy
{
    public static readonly TimeSpan DefaultThreshold = TimeSpan.FromMinutes(30);
}