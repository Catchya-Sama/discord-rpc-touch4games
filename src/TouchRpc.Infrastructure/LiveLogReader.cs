namespace TouchRpc.Infrastructure;

public sealed class LiveLogReader
{
    private const int BufferSize = 16 * 1024;
    private readonly string _path;
    private IncrementalLineDecoder _decoder = new();
    private FileIdentity? _identity;
    private long _offset;

    public LiveLogReader(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A log path is required.", nameof(path));
        _path = System.IO.Path.GetFullPath(path);
    }

    public string Path => _path;

    public async Task<LogReadBatch> ReadAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = new FileStream(
                _path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var identity = FileIdentity.From(stream, _path);
            var newSession = _identity is null || _identity.Value != identity || stream.Length < _offset;
            if (newSession)
            {
                _identity = identity;
                _offset = 0;
                _decoder.Reset();
            }

            stream.Position = _offset;
            var lines = new List<string>();
            var buffer = new byte[BufferSize];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                _offset += bytesRead;
                lines.AddRange(_decoder.Append(buffer.AsSpan(0, bytesRead)));
            }

            return new LogReadBatch(
                lines.Count > 0 ? LogSourceHealth.Receiving : LogSourceHealth.ValidWaiting,
                newSession,
                lines,
                newSession ? "A new log session was opened." : "The log is valid and waiting for complete lines.");
        }
        catch (FileNotFoundException)
        {
            return Missing();
        }
        catch (DirectoryNotFoundException)
        {
            return Missing();
        }
        catch (UnauthorizedAccessException)
        {
            return new LogReadBatch(LogSourceHealth.Unreadable, false, [], "The selected log is not accessible.");
        }
        catch (IOException)
        {
            return new LogReadBatch(LogSourceHealth.Unreadable, false, [], "The selected log could not be read.");
        }
    }

    private static LogReadBatch Missing() =>
        new(LogSourceHealth.Missing, false, [], "The selected log does not exist yet.");
}