using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Vexel.Core.Minecraft;

namespace Vexel.Platform.Windows.Memory;

public static class LoadedModuleFingerprintReader
{
    private const uint ProcessQueryInformation = 0x0400;
    private const uint ProcessVmRead = 0x0010;
    private const int BufferSize = 64 * 1024;

    public static async Task<MinecraftBuildFingerprint> ReadAsync(
        MinecraftProcess process,
        string version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (process.ModuleBaseAddress is null || process.ModuleSize is null || process.ModuleSize <= 0)
        {
            throw new InvalidOperationException("The process module layout is unavailable.");
        }

        var handle = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, process.ProcessId);
        if (handle == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Could not open the Minecraft process for reading.");
        }

        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[Math.Min(BufferSize, process.ModuleSize.Value)];
            var address = process.ModuleBaseAddress.Value;
            var remaining = process.ModuleSize.Value;

            while (remaining > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var requested = Math.Min(buffer.Length, remaining);
                if (!ReadProcessMemory(handle, new IntPtr(address), buffer, requested, out var bytesRead) || bytesRead.ToInt64() != requested)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Could not read the complete loaded module image.");
                }

                hash.AppendData(buffer, 0, requested);
                address = checked(address + requested);
                remaining -= requested;
            }

            await Task.CompletedTask;
            return new MinecraftBuildFingerprint(
                version,
                version,
                process.ModuleSize.Value,
                Convert.ToHexString(hash.GetHashAndReset()),
                null,
                ExecutableArchitecture.X64,
                FingerprintSource.LoadedModule);
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
