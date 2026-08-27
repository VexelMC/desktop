using System.ComponentModel;
using System.Runtime.InteropServices;
using Vexel.Core.Minecraft;
using Vexel.Patching.Patterns;

namespace Vexel.Platform.Windows.Memory;

/// <summary>
/// Performs a read-only pattern scan of a loaded Minecraft main module.
/// This class never requests write, create-thread, or injection permissions.
/// </summary>
public static class LoadedModulePatternScanner
{
    private const uint ProcessQueryInformation = 0x0400;
    private const uint ProcessVmRead = 0x0010;
    private const int ChunkSize = 1024 * 1024;

    public static Task<long[]> FindAllAsync(
        MinecraftProcess process,
        BytePattern pattern,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => FindAll(process, pattern, cancellationToken), cancellationToken);

    private static long[] FindAll(
        MinecraftProcess process,
        BytePattern pattern,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(process);
        ArgumentNullException.ThrowIfNull(pattern);

        if (process.ModuleBaseAddress is null || process.ModuleSize is null || process.ModuleSize < pattern.Length)
        {
            throw new InvalidOperationException("The process module layout is unavailable or too small for this pattern.");
        }

        var handle = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, process.ProcessId);
        if (handle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open the Minecraft process for reading.");
        }

        try
        {
            var matches = new List<long>();
            var overlap = pattern.Length - 1;
            var buffer = new byte[ChunkSize + overlap];
            var baseAddress = process.ModuleBaseAddress.Value;
            var offset = 0;

            while (offset < process.ModuleSize.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var requested = Math.Min(ChunkSize, process.ModuleSize.Value - offset);
                var readAddress = checked(baseAddress + offset);
                if (!ReadProcessMemory(handle, new IntPtr(readAddress), buffer, requested, out var bytesRead) || bytesRead.ToInt64() != requested)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the complete loaded module image.");
                }

                foreach (var match in PatternScanner.FindAll(buffer.AsSpan(0, requested), pattern))
                {
                    matches.Add(checked(readAddress + match));
                }

                if (requested == process.ModuleSize.Value - offset)
                {
                    break;
                }

                offset += requested;
                if (overlap > 0)
                {
                    offset -= overlap;
                }
            }

            return matches.Distinct().Order().ToArray();
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
