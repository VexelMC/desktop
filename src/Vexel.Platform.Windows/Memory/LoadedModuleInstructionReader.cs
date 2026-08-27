using System.ComponentModel;
using System.Runtime.InteropServices;
using Iced.Intel;
using Vexel.Core.Minecraft;

namespace Vexel.Platform.Windows.Memory;

/// <summary>
/// Reads and decodes a small x64 instruction window from a running module.
/// The process handle has read-only permissions.
/// </summary>
public static class LoadedModuleInstructionReader
{
    private const uint ProcessQueryInformation = 0x0400;
    private const uint ProcessVmRead = 0x0010;

    public static Task<IReadOnlyList<DecodedInstruction>> ReadWindowAsync(
        MinecraftProcess process,
        long startAddress,
        int length,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => ReadWindow(process, startAddress, length, cancellationToken), cancellationToken);

    public static IReadOnlyList<DecodedInstruction> Decode(ReadOnlySpan<byte> bytes, long startAddress)
    {
        var decoder = Decoder.Create(64, bytes.ToArray());
        decoder.IP = checked((ulong)startAddress);
        var instructions = new List<DecodedInstruction>();

        var consumed = 0;
        while (consumed < bytes.Length)
        {
            decoder.Decode(out var instruction);
            if (instruction.Code == Code.INVALID)
            {
                break;
            }

            instructions.Add(new DecodedInstruction(
                checked((long)instruction.IP),
                instruction.Length,
                instruction.ToString()));
            consumed += instruction.Length;
        }

        return instructions;
    }

    private static IReadOnlyList<DecodedInstruction> ReadWindow(
        MinecraftProcess process,
        long startAddress,
        int length,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (length is <= 0 or > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Instruction windows must be between 1 and 4096 bytes.");
        }

        if (process.ModuleBaseAddress is null || process.ModuleSize is null ||
            startAddress < process.ModuleBaseAddress || startAddress > process.ModuleBaseAddress + process.ModuleSize - length)
        {
            throw new InvalidOperationException("The requested instruction window is outside the loaded module.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var handle = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, process.ProcessId);
        if (handle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open the Minecraft process for reading.");
        }

        try
        {
            var bytes = new byte[length];
            if (!ReadProcessMemory(handle, new IntPtr(startAddress), bytes, bytes.Length, out var bytesRead) || bytesRead.ToInt64() != bytes.Length)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the complete instruction window.");
            }

            return Decode(bytes, startAddress);
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(
        IntPtr process,
        IntPtr baseAddress,
        [Out] byte[] buffer,
        int size,
        out IntPtr bytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}

public sealed record DecodedInstruction(long Address, int Length, string Text);
