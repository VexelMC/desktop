using System.Globalization;

namespace Vexel.Patching.Patterns;

public sealed class BytePattern
{
    private BytePattern(byte?[] tokens, int anchorIndex)
    {
        Tokens = tokens;
        AnchorIndex = anchorIndex;
    }

    public IReadOnlyList<byte?> Tokens { get; }

    public int Length => Tokens.Count;

    public int AnchorIndex { get; }

    public byte Anchor => Tokens[AnchorIndex]!.Value;

    public static BytePattern Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var rawTokens = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rawTokens.Length == 0)
        {
            throw new FormatException("A byte pattern must contain at least one token.");
        }

        var tokens = new byte?[rawTokens.Length];
        var anchorIndex = -1;
        for (var index = 0; index < rawTokens.Length; index++)
        {
            var token = rawTokens[index];
            if (token is "?" or "??")
            {
                tokens[index] = null;
                continue;
            }

            if (token.Length != 2 || !byte.TryParse(token, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new FormatException($"Invalid byte-pattern token '{token}' at index {index}.");
            }

            tokens[index] = parsed;
            anchorIndex = index;
        }

        if (anchorIndex < 0)
        {
            throw new FormatException("A byte pattern must contain at least one concrete byte.");
        }

        return new BytePattern(tokens, anchorIndex);
    }
}
