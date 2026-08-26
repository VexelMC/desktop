namespace Vexel.Patching.Definitions;

public sealed class PatchDefinition
{
    public PatchDefinition(
        string id,
        string name,
        string searchPattern,
        int expectedMatchCount,
        int patchOffset,
        byte[] expectedOriginalBytes,
        byte[] replacementBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);
        ArgumentNullException.ThrowIfNull(expectedOriginalBytes);
        ArgumentNullException.ThrowIfNull(replacementBytes);

        if (expectedMatchCount != 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedMatchCount), "A single patch definition must resolve to exactly one location.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(patchOffset);

        if (expectedOriginalBytes.Length == 0 || expectedOriginalBytes.Length != replacementBytes.Length)
        {
            throw new ArgumentException("Original and replacement bytes must be non-empty and the same length.");
        }

        Id = id;
        Name = name;
        SearchPattern = searchPattern;
        ExpectedMatchCount = expectedMatchCount;
        PatchOffset = patchOffset;
        ExpectedOriginalBytes = expectedOriginalBytes.ToArray();
        ReplacementBytes = replacementBytes.ToArray();
    }

    public string Id { get; }

    public string Name { get; }

    public string SearchPattern { get; }

    public int ExpectedMatchCount { get; }

    public int PatchOffset { get; }

    public byte[] ExpectedOriginalBytes { get; }

    public byte[] ReplacementBytes { get; }
}
