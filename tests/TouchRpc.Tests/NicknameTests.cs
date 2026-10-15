using TouchRpc.Core;

namespace TouchRpc.Tests;

[TestClass]
public sealed class NicknameTests
{
    [TestMethod]
    public void VerifiedAutomaticNickname_WinsOverManualNickname()
    {
        var reducer = new TouchStateReducer(
            manualNickname: "Manual Name",
            manualNicknameRoleId: "10000000001");
        reducer.Apply(new LocalIdentityObserved("10000000001"));
        reducer.Apply(new LocalNicknameObserved("10000000001", "Verified Name"));

        Assert.AreEqual("Verified Name", reducer.State.LocalNickname);
        Assert.AreEqual(NicknameSource.AutomaticVerified, reducer.State.NicknameSource);
        Assert.AreEqual("Verified Name", reducer.State.NicknameTooltip);
    }

    [TestMethod]
    public void OtherPlayersNickname_CannotReplaceLocalNickname()
    {
        var reducer = new TouchStateReducer(manualNickname: "Local Manual");
        reducer.Apply(new LocalIdentityObserved("10000000001"));
        reducer.Apply(new LocalNicknameObserved("10000000002", "Other Player"));

        Assert.AreEqual("Local Manual", reducer.State.LocalNickname);
        Assert.AreEqual(NicknameSource.Manual, reducer.State.NicknameSource);
    }

    [TestMethod]
    public void ManualNickname_WorksWhenAutomaticMappingIsUnavailable()
    {
        var reducer = new TouchStateReducer(
            localRoleId: "10000000001",
            manualNickname: "Manual Name",
            manualNicknameRoleId: "10000000001");
        reducer.Tick();

        Assert.AreEqual("Manual Name", reducer.State.LocalNickname);
        Assert.AreEqual(NicknameSource.Manual, reducer.State.NicknameSource);
        Assert.AreEqual(NicknameDiagnostic.AutomaticUnavailable, reducer.State.NicknameDiagnostic);
    }

    [TestMethod]
    public void ManualNicknameForDifferentIdentity_FallsBackToTouch()
    {
        var reducer = new TouchStateReducer(
            localRoleId: "10000000002",
            manualNickname: "Old Account",
            manualNicknameRoleId: "10000000001");
        reducer.Tick();

        Assert.IsNull(reducer.State.LocalNickname);
        Assert.AreEqual("TOUCH", reducer.State.NicknameTooltip);
        Assert.AreEqual(NicknameDiagnostic.ManualIdentityMismatch, reducer.State.NicknameDiagnostic);
    }

    [TestMethod]
    public void AccountChange_ClearsAutomaticNicknameAndRoomState()
    {
        var clock = new TestMonotonicClock();
        var reducer = new TouchStateReducer(clock: clock, afkThreshold: TimeSpan.FromMinutes(30));
        reducer.Apply(new LocalIdentityObserved("10000000001"));
        reducer.Apply(new LocalNicknameObserved("10000000001", "First Account"));
        CompleteRoom(reducer);
        clock.Advance(TimeSpan.FromMinutes(5));
        reducer.Tick();

        reducer.Apply(new LocalIdentityObserved("10000000002"));

        Assert.IsNull(reducer.State.LocalNickname);
        Assert.AreEqual("TOUCH", reducer.State.NicknameTooltip);
        Assert.AreEqual(SessionPhase.Unknown, reducer.State.Phase);
        Assert.AreEqual(0, reducer.State.UniqueMemberCount);
        Assert.IsFalse(reducer.State.IsAfkTracking);
    }

    [TestMethod]
    public void LeavingRoom_DoesNotClearLocalIdentityOrNickname()
    {
        var reducer = new TouchStateReducer();
        reducer.Apply(new LocalIdentityObserved("10000000001"));
        reducer.Apply(new LocalNicknameObserved("10000000001", "Persistent Name"));
        CompleteRoom(reducer);

        reducer.Apply(new MemberLeft("10000000001"));

        Assert.AreEqual("10000000001", reducer.State.LocalRoleId);
        Assert.AreEqual("Persistent Name", reducer.State.LocalNickname);
        Assert.AreEqual("Persistent Name", reducer.State.NicknameTooltip);
    }

    [TestMethod]
    public void UnicodeNickname_PreservesCapitalizationAndTextElements()
    {
        var nickname = "Xóóty 界🎵";
        var selected = NicknamePolicy.Select("10000000001", nickname, "10000000001", null, null);

        Assert.AreEqual(nickname, selected.LocalNickname);
        Assert.AreEqual(nickname, selected.Tooltip);
    }

    [TestMethod]
    public void DiscordTooltip_UsesFallbackForOneCharacterAndTruncatesAtTextElementBoundary()
    {
        var shortName = NicknamePolicy.Select("10000000001", "界", "10000000001", null, null);
        var longName = NicknamePolicy.Select(
            "10000000001",
            string.Concat(Enumerable.Repeat("e\u0301", 130)),
            "10000000001",
            null,
            null);

        Assert.AreEqual("TOUCH", shortName.Tooltip);
        Assert.AreEqual(NicknameDiagnostic.TooltipTooShort, shortName.Diagnostic);
        Assert.AreEqual(string.Concat(Enumerable.Repeat("e\u0301", 128)), longName.Tooltip);
        Assert.AreEqual(NicknameDiagnostic.TooltipTruncated, longName.Diagnostic);
    }

    [TestMethod]
    public void NicknameChange_DoesNotChangePresenceRosterOrAfkAnchor()
    {
        var clock = new TestMonotonicClock();
        var reducer = new TouchStateReducer(clock: clock, afkThreshold: TimeSpan.FromMinutes(30));
        reducer.Apply(new LocalIdentityObserved("10000000001"));
        CompleteRoom(reducer);
        clock.Advance(TimeSpan.FromMinutes(10));
        reducer.Tick();
        var before = PresenceFormatter.Format(reducer.State);

        reducer.Apply(new LocalNicknameObserved("10000000001", "New Name"));

        Assert.AreEqual(before, PresenceFormatter.Format(reducer.State));
        Assert.AreEqual(2, reducer.State.UniqueMemberCount);
        Assert.AreEqual(TimeSpan.FromMinutes(10), reducer.State.AfkElapsed);
    }

    private static void CompleteRoom(TouchStateReducer reducer)
    {
        reducer.Apply(new RoomDetailsStarted());
        reducer.Apply(new MemberObserved("10000000001", MemberEvidence.Snapshot));
        reducer.Apply(new MemberObserved("10000000002", MemberEvidence.Snapshot));
        reducer.Apply(new WorldObserved(SessionPhase.Waiting));
    }
}