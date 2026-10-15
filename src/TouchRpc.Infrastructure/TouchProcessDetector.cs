using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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
                    executablePath = process.MainModule?.FileName ?? TryGetExecutablePath(process.Id);
                }
                catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    executablePath = TryGetExecutablePath(process.Id);
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

    public static bool IsRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string? TryGetExecutablePath(int processId)
    {
        if (!OperatingSystem.IsWindows()) return null;
        var handle = OpenProcess(ProcessQueryLimitedInformation, inheritHandle: false, processId);
        if (handle == IntPtr.Zero) return null;
        try
        {
            var capacity = 32768;
            var path = new StringBuilder(capacity);
            return QueryFullProcessImageName(handle, 0, path, ref capacity) ? path.ToString() : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private const uint ProcessQueryLimitedInformation = 0x1000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        IntPtr process,
        uint flags,
        StringBuilder executablePath,
        ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}