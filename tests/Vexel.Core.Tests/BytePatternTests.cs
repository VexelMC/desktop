using Vexel.Patching.Patterns;

namespace Vexel.Core.Tests;

public sealed class BytePatternTests
{
    private static readonly byte[] ShortData = [0x90, 0x90];
    private static readonly int[] ExpectedWildcardMatches = [1, 5];

    [Fact]
    public void ParseAcceptsConcreteBytesAndWildcards()
    {
        var pattern = BytePattern.Parse("48 8B ?? ? 90");

        Assert.Equal(5, pattern.Length);
        Assert.Equal((byte)0x48, pattern.Tokens[0]);
        Assert.Null(pattern.Tokens[2]);
        Assert.Equal(4, pattern.AnchorIndex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?? ?")]
    [InlineData("4")]
    [InlineData("GG")]
    [InlineData("4? 90")]
    public void ParseRejectsUnsafeOrMalformedPatterns(string value)
    {
        Assert.ThrowsAny<Exception>(() => BytePattern.Parse(value));
    }

    [Fact]
    public void FindAllReturnsEveryWildcardMatch()
    {
        var data = new byte[] { 0x90, 0x48, 0x8B, 0x01, 0x90, 0x48, 0x8B, 0x02, 0x90 };
        var pattern = BytePattern.Parse("48 8B ?? 90");

        var matches = PatternScanner.FindAll(data, pattern);

        Assert.Equal(ExpectedWildcardMatches, matches);
    }

    [Fact]
    public void FindAllReturnsNoMatchWhenPatternIsLongerThanData()
    {
        var matches = PatternScanner.FindAll(ShortData, BytePattern.Parse("90 90 90"));

        Assert.Empty(matches);
    }
}
