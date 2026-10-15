using TouchRpc.Core;

namespace TouchRpc.Tests;

[TestClass]
public sealed class AfkLifecycleTests
{
    private static readonly TimeSpan TestAfkThreshold = TimeSpan.FromMinutes(30);

    [TestMethod]
    public void AfkBoundary_ChangesExactlyAtThirtyMinutes()
    {
        var clock = new TestMonotonicClock();
        var reducer = CompletedRoom(clock, LocalRole.Joined);

        clock.Advance(TimeSpan.FromMinutes(29) + TimeSpan.FromSeconds(59));
        reducer.Tick();
        Assert.IsFalse(reducer.State.IsAfk);
        Assert.AreEqual("Waiting", PresenceFormatter.Format(reducer.State).State);

        clock.Advance(TimeSpan.FromSeconds(1));
        reducer.Tick();
        Assert.IsTrue(reducer.State.IsAfk);
        Assert.AreEqual("AFK · Waiting", PresenceFormatter.Format(reducer.State).State);
    }

    [TestMethod]
    public void JoinedGameplay_CancelsAfkUntilReturningToWaiting()
    {
        var clock = new TestMonotonicClock();
        var reducer = CompletedRoom(clock, LocalRole.Joined);
        clock.Advance(TestAfkThreshold);
        reducer.Tick();
        Assert.IsTrue(reducer.State.IsAfk);

        reducer.Apply(new GameplayObserved());
        Assert.IsFalse(reducer.State.IsAfkTracking);
        Assert.IsFalse(reducer.State.IsAfk);
        Assert.AreEqual("Smashing Key", PresenceFormatter.Format(reducer.State).State);

        reducer.Apply(new ResultsObserved());
        clock.Advance(TimeSpan.FromHours(1));
        reducer.Tick();
        Assert.IsFalse(reducer.State.IsAfk);

        reducer.Apply(new WorldObserved(SessionPhase.Waiting));
        Assert.IsTrue(reducer.State.IsAfkTracking);
        Assert.AreEqual(TimeSpan.Zero, reducer.State.AfkElapsed);
        clock.Advance(TestAfkThreshold);
        reducer.Tick();
        Assert.AreEqual("AFK · Waiting", PresenceFormatter.Format(reducer.State).State);
    }

    [TestMethod]
    public void PlayRoomWorld_CancelsKnownJoinedAfkBeforeGameplayMarker()
    {
        var clock = new TestMonotonicClock();
        var reducer = CompletedRoom(clock, LocalRole.Joined);
        clock.Advance(TestAfkThreshold);
        reducer.Tick();
        Assert.IsTrue(reducer.State.IsAfk);

        reducer.Apply(new WorldObserved(SessionPhase.Playing));

        Assert.IsFalse(reducer.State.IsAfkTracking);
        Assert.IsFalse(reducer.State.IsAfk);
        Assert.AreEqual("Smashing Key", PresenceFormatter.Format(reducer.State).State);
    }

    [TestMethod]
    public void SpectatorTimer_ContinuesThroughGameplayResultsAndWaiting()
    {
        var clock = new TestMonotonicClock();
        var reducer = CompletedRoom(clock, LocalRole.Spectator);
        clock.Advance(TimeSpan.FromMinutes(20));

        reducer.Apply(new GameplayObserved());
        reducer.Apply(new ResultsObserved());
        reducer.Apply(new WorldObserved(SessionPhase.Waiting));
        clock.Advance(TimeSpan.FromMinutes(10));
        reducer.Tick();

        Assert.IsTrue(reducer.State.IsAfk);
        Assert.AreEqual(TestAfkThreshold, reducer.State.AfkElapsed);
        Assert.AreEqual("AFK · Spectating", PresenceFormatter.Format(reducer.State).State);
    }

    [TestMethod]
    public void RoleSwitchReadyAndRemoteActivity_DoNotResetTimer()
    {
        var clock = new TestMonotonicClock();
        var reducer = new TouchStateReducer("10000000001", LocalRole.Joined, clock, TestAfkThreshold);
        CompleteRoom(reducer);
        clock.Advance(TimeSpan.FromMinutes(20));

        reducer.Apply(new MemberObserved("10000000002", MemberEvidence.AddPlayer));
        reducer.Apply(new RoomOperationObserved(2, "10000000001"));
        reducer.Apply(new RoomOperationObserved(21, "10000000002"));
        reducer.Apply(new RoleSwitchRequested());
        reducer.Apply(new RoomOperationObserved(1, "10000000001"));
        clock.Advance(TimeSpan.FromMinutes(10));
        reducer.Tick();

        Assert.AreEqual(LocalRole.Spectator, reducer.State.LocalRole);
        Assert.IsTrue(reducer.State.IsAfk);
        Assert.AreEqual(TestAfkThreshold, reducer.State.AfkElapsed);
    }

    [TestMethod]
    public void LocalLeaveAndNewRoom_CreateFreshInterval()
    {
        var clock = new TestMonotonicClock();
        var reducer = new TouchStateReducer("10000000001", LocalRole.Joined, clock, TestAfkThreshold);
        CompleteRoom(reducer);
        clock.Advance(TestAfkThreshold);
        reducer.Tick();
        Assert.IsTrue(reducer.State.IsAfk);

        reducer.Apply(new MemberLeft("10000000001"));
        Assert.IsFalse(reducer.State.IsAfkTracking);
        Assert.AreEqual(AfkTimerOrigin.None, reducer.State.AfkTimerOrigin);

        CompleteRoom(reducer);
        Assert.IsFalse(reducer.State.IsAfk);
        Assert.AreEqual(TimeSpan.Zero, reducer.State.AfkElapsed);
    }

    [TestMethod]
    public void PendingEntryAndUnknownGameplay_DoNotInventAfk()
    {
        var clock = new TestMonotonicClock();
        var reducer = new TouchStateReducer(clock: clock, afkThreshold: TestAfkThreshold);
        reducer.Apply(new RoomEntryRequested());
        clock.Advance(TimeSpan.FromHours(2));
        reducer.Tick();

        Assert.IsFalse(reducer.State.IsAfkTracking);
        Assert.IsFalse(reducer.State.IsAfk);

        reducer.Apply(new GameplayObserved());
        clock.Advance(TimeSpan.FromHours(2));
        reducer.Tick();
        Assert.IsFalse(reducer.State.IsAfk);
        Assert.AreEqual(DiagnosticCode.RoomEntryPending, reducer.State.Diagnostic);
    }

    [TestMethod]
    public void ConfirmedRoomWithUnknownRole_TracksTimeButDoesNotClaimAfk()
    {
        var clock = new TestMonotonicClock();
        var reducer = new TouchStateReducer(clock: clock, afkThreshold: TestAfkThreshold);
        reducer.Apply(new RoomEntryRequested());
        reducer.Apply(new WorldObserved(SessionPhase.Waiting));

        Assert.IsTrue(reducer.State.IsAfkTracking);
        Assert.IsFalse(reducer.State.IsAfk);

        clock.Advance(TestAfkThreshold);
        reducer.Tick();

        Assert.IsFalse(reducer.State.IsAfk);
        Assert.AreEqual(DiagnosticCode.AfkRoleUnknown, reducer.State.Diagnostic);
        Assert.AreEqual("Unknown", PresenceFormatter.Format(reducer.State).State);
    }

    [TestMethod]
    public void HistoricalReplay_StartsAtMonitoringWithExplicitDiagnostic()
    {
        var clock = new TestMonotonicClock();
        var reducer = new TouchStateReducer(configuredRole: LocalRole.Spectator, clock: clock, afkThreshold: TestAfkThreshold);
        reducer.BeginHistoricalReplay();
        CompleteRoom(reducer);
        reducer.EndHistoricalReplay();

        Assert.AreEqual(AfkTimerOrigin.MonitoringStarted, reducer.State.AfkTimerOrigin);
        Assert.AreEqual(DiagnosticCode.AfkHistoryUnavailable, reducer.State.Diagnostic);
        Assert.IsFalse(reducer.State.IsAfk);

        clock.Advance(TestAfkThreshold);
        reducer.Tick();
        Assert.IsTrue(reducer.State.IsAfk);
        Assert.AreEqual("AFK · Spectating", PresenceFormatter.Format(reducer.State).State);
    }

    [TestMethod]
    public void NonPositiveThreshold_IsRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new TouchStateReducer(afkThreshold: TimeSpan.Zero));
    }

    private static TouchStateReducer CompletedRoom(TestMonotonicClock clock, LocalRole role)
    {
        var reducer = new TouchStateReducer(configuredRole: role, clock: clock, afkThreshold: TestAfkThreshold);
        CompleteRoom(reducer);
        return reducer;
    }

    private static void CompleteRoom(TouchStateReducer reducer)
    {
        reducer.Apply(new RoomEntryRequested());
        reducer.Apply(new RoomDetailsStarted());
        reducer.Apply(new MemberObserved("10000000001", MemberEvidence.Snapshot));
        reducer.Apply(new WorldObserved(SessionPhase.Waiting));
    }
}