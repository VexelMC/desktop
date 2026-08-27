using Vexel.Platform.Windows.Memory;

namespace Vexel.Core.Tests;

public sealed class LoadedModuleInstructionReaderTests
{
    [Fact]
    public void DecodeReadsX64InstructionStream()
    {
        var decoded = LoadedModuleInstructionReader.Decode(
            [0x48, 0x89, 0x01, 0xC3],
            0x0000_7FF6_0000_0000);

        Assert.Collection(
            decoded,
            first => Assert.Equal("mov [rcx],rax", first.Text),
            second => Assert.Equal("ret", second.Text));
    }
}
