namespace TouchRpc.Core;

public static class PresenceFormatter
{
    public static PresenceView Format(TouchState state)
    {
        if (state.Phase == SessionPhase.Lobby)
            return new PresenceView("In Lobby", string.Empty, DiagnosticCode.None);

        var details = state.RosterCompleteness == RosterCompleteness.Complete && state.UniqueMemberCount > 0
            ? state.UniqueMemberCount == 1 ? "1 Player Joined" : $"{state.UniqueMemberCount} Players Joined"
            : state.Phase is SessionPhase.Playing or SessionPhase.Loading or SessionPhase.Results ? "In Session" : "In Room";

        var presenceState = state.LocalRole switch
        {
            LocalRole.Spectator when state.IsAfk => "AFK · Spectating",
            LocalRole.Spectator => "Spectating",
            LocalRole.Joined when state.IsAfk => "AFK · Waiting",
            LocalRole.Joined when state.Phase == SessionPhase.Playing => "Smashing Key",
            LocalRole.Joined when state.Phase == SessionPhase.Loading => "Loading",
            LocalRole.Joined when state.Phase == SessionPhase.Results => "Viewing Results",
            LocalRole.Joined => "Waiting",
            _ => "Unknown",
        };

        return new PresenceView(details, presenceState, state.Diagnostic);
    }
}