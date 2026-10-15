using TouchRpc.Core;

return Run(args);

static int Run(string[] args)
{
    if (!ReplayOptions.TryParse(args, out var options, out var error))
    {
        Console.Error.WriteLine(error);
        Console.Error.WriteLine("Usage: TouchRpc.Replay <file> [--role-id <digits>] [--role joined|spectator]");
        return 2;
    }

    try
    {
        var parser = new TouchLogParser();
        var reducer = new TouchStateReducer(options.LocalRoleId, options.ConfiguredRole, new ReplayClock());
        reducer.BeginHistoricalReplay();
        foreach (var line in File.ReadLines(options.Path))
            reducer.Replay(parser.ParseLine(line));
        reducer.EndHistoricalReplay();

        var state = reducer.State;
        var presence = PresenceFormatter.Format(state);
        Console.WriteLine($"Details: {presence.Details}");
        Console.WriteLine($"State: {presence.State}");
        Console.WriteLine($"Phase: {state.Phase}");
        Console.WriteLine($"Local role: {state.LocalRole} ({state.RoleEvidence})");
        Console.WriteLine($"Roster: {state.RosterCompleteness}");
        Console.WriteLine($"AFK: {(state.IsAfkTracking ? $"Counting from {state.AfkTimerOrigin}" : "Not tracking")}");
        Console.WriteLine($"Diagnostic: {presence.Diagnostic}");
        return 0;
    }
    catch (IOException)
    {
        Console.Error.WriteLine("The replay file could not be read.");
        return 1;
    }
    catch (UnauthorizedAccessException)
    {
        Console.Error.WriteLine("The replay file is not accessible.");
        return 1;
    }
}

internal sealed class ReplayClock : IMonotonicClock
{
    public long GetTimestamp() => 0;

    public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) => TimeSpan.Zero;
}

internal sealed record ReplayOptions(string Path, string? LocalRoleId, LocalRole ConfiguredRole)
{
    public static bool TryParse(string[] args, out ReplayOptions options, out string error)
    {
        options = new ReplayOptions(string.Empty, null, LocalRole.Unknown);
        error = string.Empty;
        if (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            error = "A replay file path is required.";
            return false;
        }

        string? localRoleId = null;
        var configuredRole = LocalRole.Unknown;
        for (var index = 1; index < args.Length; index++)
        {
            if (args[index] == "--role-id" && index + 1 < args.Length)
            {
                localRoleId = args[++index];
                if (!RoleId.IsValid(localRoleId))
                {
                    error = "Role ID must contain digits only.";
                    return false;
                }
            }
            else if (args[index] == "--role" && index + 1 < args.Length)
            {
                configuredRole = args[++index].ToLowerInvariant() switch
                {
                    "joined" => LocalRole.Joined,
                    "spectator" => LocalRole.Spectator,
                    _ => LocalRole.Unknown,
                };
                if (configuredRole == LocalRole.Unknown)
                {
                    error = "Role must be joined or spectator.";
                    return false;
                }
            }
            else
            {
                error = "An unknown or incomplete option was provided.";
                return false;
            }
        }

        options = new ReplayOptions(args[0], localRoleId, configuredRole);
        return true;
    }
}
