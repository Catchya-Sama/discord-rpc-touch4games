using TouchRpc.Core;

namespace TouchRpc.Tests;

[TestClass]
public sealed class ReducerTests
{
    [TestMethod]
    public void WaitingEntry_InfersJoinedOnlyAtKnownWorld()
    {
        var reducer = new TouchStateReducer();
        reducer.Apply(new RoomEntryRequested());
        reducer.Apply(new RoomDetailsStarted());
        reducer.Apply(new MemberObserved("10000000001", MemberEvidence.Snapshot));
        Assert.AreEqual(LocalRole.Unknown, reducer.State.LocalRole);
        Assert.AreEqual(RosterCompleteness.Partial, reducer.State.RosterCompleteness);

        reducer.Apply(new WorldObserved(SessionPhase.Waiting));
        Assert.AreEqual(LocalRole.Joined, reducer.State.LocalRole);
        Assert.AreEqual(RosterCompleteness.Complete, reducer.State.RosterCompleteness);
    }

    [TestMethod]
    public void MidgameEntry_InfersSpectatorAndNeverSmashingKey()
    {
        var reducer = new TouchStateReducer();
        reducer.Replay([new RoomEntryRequested(), new RoomDetailsStarted(),
            new MemberObserved("10000000001", MemberEvidence.Snapshot),
            new MidgameEntryObserved(), new WorldObserved(SessionPhase.Playing), new GameplayObserved()]);

        Assert.AreEqual(LocalRole.Spectator, reducer.State.LocalRole);
        Assert.AreEqual("Spectating", PresenceFormatter.Format(reducer.State).State);
    }

    [TestMethod]
    public void DuplicateMembers_DoNotChangeUniqueCount()
    {
        var reducer = CompletedJoinedRoom("10000000001", "10000000002", "10000000003");
        reducer.Apply(new MemberObserved("10000000002", MemberEvidence.AddPlayer));
        reducer.Apply(new MemberObserved("10000000002", MemberEvidence.RoomMember));
        Assert.AreEqual(3, reducer.State.UniqueMemberCount);
    }

    [TestMethod]
    public void RemoteLeave_RemovesExactlyOneUniqueMember()
    {
        var reducer = CompletedJoinedRoom("10000000001", "10000000002", "10000000003");
        reducer.Apply(new MemberLeft("10000000002"));
        reducer.Apply(new MemberLeft("10000000002"));
        Assert.AreEqual(2, reducer.State.UniqueMemberCount);
    }

    [TestMethod]
    public void LocalLeave_ClearsRoomState()
    {
        var reducer = new TouchStateReducer("10000000001");
        CompleteJoinedRoom(reducer, "10000000001", "10000000002");
        reducer.Apply(new MemberLeft("10000000001"));
        Assert.AreEqual(SessionPhase.Unknown, reducer.State.Phase);
        Assert.AreEqual(LocalRole.Unknown, reducer.State.LocalRole);
        Assert.AreEqual(0, reducer.State.UniqueMemberCount);
    }

    [TestMethod]
    public void RoleSwitch_TogglesOnceOnlyAfterMatchingLocalConfirmation()
    {
        var reducer = new TouchStateReducer("10000000001");
        CompleteJoinedRoom(reducer, "10000000001", "10000000002");
        reducer.Apply(new RoleSwitchRequested());
        reducer.Apply(new RoomOperationObserved(1, "10000000002"));
        Assert.AreEqual(LocalRole.Joined, reducer.State.LocalRole);
        reducer.Apply(new RoomOperationObserved(1, "10000000001"));
        reducer.Apply(new RoomOperationObserved(1, "10000000001"));
        Assert.AreEqual(LocalRole.Spectator, reducer.State.LocalRole);
    }

    [TestMethod]
    public void UnconfirmedRoleSwitch_DoesNotToggle()
    {
        var reducer = new TouchStateReducer("10000000001");
        CompleteJoinedRoom(reducer, "10000000001");
        reducer.Apply(new RoleSwitchRequested());
        Assert.AreEqual(LocalRole.Joined, reducer.State.LocalRole);
        Assert.AreEqual(DiagnosticCode.RoleSwitchAwaitingConfirmation, reducer.State.Diagnostic);
    }

    [TestMethod]
    public void ReadyAndHostOperations_DoNotChangeRole()
    {
        var reducer = new TouchStateReducer("10000000001");
        CompleteJoinedRoom(reducer, "10000000001");
        reducer.Apply(new RoomOperationObserved(2, "10000000001"));
        reducer.Apply(new RoomOperationObserved(3, "10000000001"));
        reducer.Apply(new RoomOperationObserved(21, "10000000001"));
        Assert.AreEqual(LocalRole.Joined, reducer.State.LocalRole);
    }

    [TestMethod]
    public void Spectator_PersistsThroughResultsAndWaiting()
    {
        var reducer = new TouchStateReducer(configuredRole: LocalRole.Spectator);
        reducer.Apply(new RoomDetailsStarted());
        reducer.Apply(new MemberObserved("10000000001", MemberEvidence.Snapshot));
        reducer.Apply(new ResultsObserved());
        reducer.Apply(new WorldObserved(SessionPhase.Waiting));
        Assert.AreEqual(LocalRole.Spectator, reducer.State.LocalRole);
        Assert.AreEqual("Spectating", PresenceFormatter.Format(reducer.State).State);
    }

    [TestMethod]
    public void ConfiguredRole_TakesPriorityOverEntryInference()
    {
        var reducer = new TouchStateReducer(configuredRole: LocalRole.Spectator);
        reducer.Apply(new RoomEntryRequested());
        reducer.Apply(new RoomDetailsStarted());
        reducer.Apply(new MemberObserved("10000000001", MemberEvidence.Snapshot));
        reducer.Apply(new WorldObserved(SessionPhase.Waiting));

        Assert.AreEqual(LocalRole.Spectator, reducer.State.LocalRole);
        Assert.AreEqual(EvidenceKind.UserConfigured, reducer.State.RoleEvidence);
        Assert.AreEqual("Spectating", PresenceFormatter.Format(reducer.State).State);
    }

    private static TouchStateReducer CompletedJoinedRoom(params string[] members)
    {
        var reducer = new TouchStateReducer();
        CompleteJoinedRoom(reducer, members);
        return reducer;
    }

    private static void CompleteJoinedRoom(TouchStateReducer reducer, params string[] members)
    {
        reducer.Apply(new RoomEntryRequested());
        reducer.Apply(new RoomDetailsStarted());
        foreach (var member in members) reducer.Apply(new MemberObserved(member, MemberEvidence.Snapshot));
        reducer.Apply(new WorldObserved(SessionPhase.Waiting));
    }
}