using TouchRpc.Core;

namespace TouchRpc.Tests;

internal sealed class TestMonotonicClock : IMonotonicClock
{
    private long _ticks;

    public long GetTimestamp() => _ticks;

    public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
        TimeSpan.FromTicks(endingTimestamp - startingTimestamp);

    public void Advance(TimeSpan elapsed) => _ticks += elapsed.Ticks;
}