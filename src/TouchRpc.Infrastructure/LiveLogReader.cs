namespace TouchRpc.Infrastructure;

public sealed class LiveLogReader
{
    private const int BufferSize = 16 * 1024;
    private const int PrefixProbeSize = 4 * 1024;
    private readonly string _path;
    private IncrementalLineDecoder _decoder = new();
    private FileIdentity? _identity;
    private byte[] _prefix = [];
    private DateTime _lastWriteUtc;
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
            var lastWriteUtc = File.GetLastWriteTimeUtc(_path);
            var prefix = await ReadPrefixAsync(stream, cancellationToken).ConfigureAwait(false);
            var prefixChanged = _prefix.Length > 0 && !HasSamePrefix(_prefix, prefix);
            var sameLengthRewrite = _identity is not null && stream.Length == _offset &&
                lastWriteUtc != _lastWriteUtc;
            var newSession = _identity is null || _identity.Value != identity || stream.Length < _offset ||
                prefixChanged || sameLengthRewrite;
            if (newSession)
            {
                _identity = identity;
                _offset = 0;
                _decoder.Reset();
            }

            _prefix = prefix;
            _lastWriteUtc = lastWriteUtc;

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

    private static async Task<byte[]> ReadPrefixAsync(FileStream stream, CancellationToken cancellationToken)
    {
        var prefixLength = (int)Math.Min(stream.Length, PrefixProbeSize);
        if (prefixLength == 0) return [];

        var prefix = new byte[prefixLength];
        stream.Position = 0;
        var totalRead = 0;
        while (totalRead < prefix.Length)
        {
            var bytesRead = await stream.ReadAsync(prefix.AsMemory(totalRead), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0) break;
            totalRead += bytesRead;
        }

        return totalRead == prefix.Length ? prefix : prefix[..totalRead];
    }

    private static bool HasSamePrefix(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> current)
    {
        var comparisonLength = Math.Min(previous.Length, current.Length);
        return previous[..comparisonLength].SequenceEqual(current[..comparisonLength]);
    }
}