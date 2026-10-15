using System.Diagnostics;

namespace TouchRpc.Infrastructure;

public sealed record TouchProcessCandidate(
    int ProcessId,
    string DisplayName,
    string? ExecutablePath,
    string? LogPath,
    bool IsAccessible);

public sealed class TouchProcessDetector
{
    public IReadOnlyList<TouchProcessCandidate> FindCandidates()
    {
        var candidates = new List<TouchProcessCandidate>();
        foreach (var process in Process.GetProcessesByName("TouchClient").OrderBy(item => item.Id))
        {
            using (process)
            {
                string? executablePath = null;
                try
                {
                    executablePath = process.MainModule?.FileName;
                }
                catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    // Access denied and cross-bitness failures are expected fallback cases.
                }

                var logPath = executablePath is null ? null : BuildLogPath(executablePath);
                candidates.Add(new TouchProcessCandidate(
                    process.Id,
                    $"TouchClient.exe (PID {process.Id})",
                    executablePath,
                    logPath,
                    executablePath is not null));
            }
        }

        return candidates;
    }

    public static string BuildLogPath(string executablePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(executablePath))
            ?? throw new ArgumentException("The executable path has no parent directory.", nameof(executablePath));
        return Path.Combine(directory, "TouchClient_Data", "output_log.txt");
    }
}