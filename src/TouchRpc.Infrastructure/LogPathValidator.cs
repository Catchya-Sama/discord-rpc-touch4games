namespace TouchRpc.Infrastructure;

public sealed record LogPathValidation(bool IsValid, LogSourceHealth Health, string Message);

public static class LogPathValidator
{
    public static LogPathValidation Validate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new LogPathValidation(false, LogSourceHealth.Missing, "Select output_log.txt first.");

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new LogPathValidation(false, LogSourceHealth.Unreadable, "The selected path is not valid.");
        }

        if (!File.Exists(fullPath))
            return new LogPathValidation(false, LogSourceHealth.Missing, "The selected log does not exist.");

        try
        {
            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            Span<byte> probe = stackalloc byte[256];
            var bytesRead = stream.Read(probe);
            if (probe[..bytesRead].Contains((byte)0))
                return new LogPathValidation(false, LogSourceHealth.Unreadable, "The selected file does not look like a text log.");
            return new LogPathValidation(true, LogSourceHealth.ValidWaiting, "The selected log is readable.");
        }
        catch (UnauthorizedAccessException)
        {
            return new LogPathValidation(false, LogSourceHealth.Unreadable, "The selected log is not accessible.");
        }
        catch (IOException)
        {
            return new LogPathValidation(false, LogSourceHealth.Unreadable, "The selected log could not be read.");
        }
    }
}