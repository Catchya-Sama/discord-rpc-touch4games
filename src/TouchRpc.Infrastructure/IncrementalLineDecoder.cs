using System.Text;

namespace TouchRpc.Infrastructure;

internal sealed class IncrementalLineDecoder
{
    private readonly Decoder _decoder = new UTF8Encoding(false, false).GetDecoder();
    private readonly StringBuilder _partialLine = new();

    public IReadOnlyList<string> Append(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return [];

        var charCount = _decoder.GetCharCount(bytes, flush: false);
        var chars = new char[charCount];
        _decoder.GetChars(bytes, chars, flush: false);
        _partialLine.Append(chars);

        var lines = new List<string>();
        var start = 0;
        for (var index = 0; index < _partialLine.Length; index++)
        {
            if (_partialLine[index] != '\n') continue;

            var length = index - start;
            if (length > 0 && _partialLine[index - 1] == '\r') length--;
            lines.Add(_partialLine.ToString(start, length));
            start = index + 1;
        }

        if (start > 0) _partialLine.Remove(0, start);
        return lines;
    }

    public void Reset()
    {
        _decoder.Reset();
        _partialLine.Clear();
    }
}