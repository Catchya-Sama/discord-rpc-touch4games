using TouchRpc.Core;

namespace TouchRpc.Tests;

[TestClass]
public sealed class PresenceFormatterTests
{
    [TestMethod]
    public void CompleteRoster_UsesSingularAndPluralGrammar()
    {
        var one = CreateWaitingRoom("10000000001");
        var three = CreateWaitingRoom("10000000001", "10000000002", "10000000003");
        Assert.AreEqual("1 Player Joined", PresenceFormatter.Format(one.State).Details);
        Assert.AreEqual("3 Players Joined", PresenceFormatter.Format(three.State).Details);
    }

    [TestMethod]
    public void IncompleteRoster_HidesCountInsteadOfDisplayingZero()
    {
        var reducer = new TouchStateReducer();
        reducer.Apply(new RoomEntryRequested());
        var presence = PresenceFormatter.Format(reducer.State);
        Assert.AreEqual("In Room", presence.Details);
        Assert.AreEqual("Unknown", presence.State);
    }

    [TestMethod]
    public void UnknownRoleInGameplay_DoesNotClaimSmashingKey()
    {
        var reducer = new TouchStateReducer();
        reducer.Apply(new GameplayObserved());
        var presence = PresenceFormatter.Format(reducer.State);
        Assert.AreEqual("In Session", presence.Details);
        Assert.AreEqual("Unknown", presence.State);
    }

    [TestMethod]
    public void CompleteRosterWithoutMembers_HidesCountInsteadOfDisplayingZero()
    {
        var reducer = new TouchStateReducer();
        reducer.Apply(new RoomEntryRequested());
        reducer.Apply(new RoomDetailsStarted());
        reducer.Apply(new WorldObserved(SessionPhase.Waiting));

        var presence = PresenceFormatter.Format(reducer.State);
        Assert.AreEqual("In Room", presence.Details);
        Assert.AreNotEqual("0 Players Joined", presence.Details);
    }

    [TestMethod]
    public void GameplayState_DependsOnVerifiedLocalRole()
    {
        var joined = CreateWaitingRoom("10000000001");
        joined.Apply(new GameplayObserved());

        var spectator = new TouchStateReducer(configuredRole: LocalRole.Spectator);
        spectator.Apply(new RoomEntryRequested());
        spectator.Apply(new RoomDetailsStarted());
        spectator.Apply(new MemberObserved("10000000001", MemberEvidence.Snapshot));
        spectator.Apply(new WorldObserved(SessionPhase.Waiting));
        spectator.Apply(new GameplayObserved());

        Assert.AreEqual("Smashing Key", PresenceFormatter.Format(joined.State).State);
        Assert.AreEqual("Spectating", PresenceFormatter.Format(spectator.State).State);
    }

    [TestMethod]
    public void Lobby_HasNoStateText()
    {
        var reducer = new TouchStateReducer();
        reducer.Apply(new WorldObserved(SessionPhase.Lobby));
        var presence = PresenceFormatter.Format(reducer.State);
        Assert.AreEqual("In Lobby", presence.Details);
        Assert.AreEqual(string.Empty, presence.State);
    }

    [TestMethod]
    public void AfkState_UsesRoleSpecificProductText()
    {
        var joinedClock = new TestMonotonicClock();
        var joined = new TouchStateReducer(
            configuredRole: LocalRole.Joined,
            clock: joinedClock,
            afkThreshold: TimeSpan.FromSeconds(1));
        CompleteRoom(joined);
        joinedClock.Advance(TimeSpan.FromSeconds(1));
        joined.Tick();

        var spectatorClock = new TestMonotonicClock();
        var spectator = new TouchStateReducer(
            configuredRole: LocalRole.Spectator,
            clock: spectatorClock,
            afkThreshold: TimeSpan.FromSeconds(1));
        CompleteRoom(spectator);
        spectatorClock.Advance(TimeSpan.FromSeconds(1));
        spectator.Tick();

        Assert.AreEqual("AFK · Waiting", PresenceFormatter.Format(joined.State).State);
        Assert.AreEqual("AFK · Spectating", PresenceFormatter.Format(spectator.State).State);
    }

    private static TouchStateReducer CreateWaitingRoom(params string[] members)
    {
        var reducer = new TouchStateReducer();
        reducer.Apply(new RoomEntryRequested());
        reducer.Apply(new RoomDetailsStarted());
        foreach (var member in members) reducer.Apply(new MemberObserved(member, MemberEvidence.Snapshot));
        reducer.Apply(new WorldObserved(SessionPhase.Waiting));
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