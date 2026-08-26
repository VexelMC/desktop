namespace Vexel.Patching.Patterns;

public static class PatternScanner
{
    public static IReadOnlyList<int> FindAll(ReadOnlySpan<byte> data, BytePattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        if (data.Length < pattern.Length)
        {
            return [];
        }

        var matches = new List<int>();
        var finalOffset = data.Length - pattern.Length;
        for (var offset = 0; offset <= finalOffset; offset++)
        {
            if (data[offset + pattern.AnchorIndex] != pattern.Anchor)
            {
                continue;
            }

            if (MatchesAt(data, pattern, offset))
            {
                matches.Add(offset);
            }
        }

        return matches;
    }

    private static bool MatchesAt(ReadOnlySpan<byte> data, BytePattern pattern, int offset)
    {
        for (var index = 0; index < pattern.Length; index++)
        {
            var expected = pattern.Tokens[index];
            if (expected.HasValue && data[offset + index] != expected.Value)
            {
                return false;
            }
        }

        return true;
    }
}
