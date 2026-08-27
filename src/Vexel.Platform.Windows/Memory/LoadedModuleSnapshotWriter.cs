using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Vexel.Core.Minecraft;

namespace Vexel.Platform.Windows.Memory;

/// <summary>
/// Exports a loaded module image with read-only process access.
/// Existing destination files are never overwritten.
/// </summary>
public static class LoadedModuleSnapshotWriter
{
    private const uint ProcessQueryInformation = 0x0400;
    private const uint ProcessVmRead = 0x0010;
    private const int BufferSize = 64 * 1024;

    public static Task<ModuleSnapshot> WriteAsync(
        MinecraftProcess process,
        string destinationPath,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Write(process, destinationPath, cancellationToken), cancellationToken);

    private static ModuleSnapshot Write(
        MinecraftProcess process,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(process);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        if (process.ModuleBaseAddress is null || process.ModuleSize is null || process.ModuleSize <= 0)
        {
            throw new InvalidOperationException("The process module layout is unavailable.");
        }

        var fullDestinationPath = Path.GetFullPath(destinationPath);
        if (File.Exists(fullDestinationPath))
        {
            throw new IOException("The snapshot destination already exists and will not be overwritten.");
        }

        var destinationDirectory = Path.GetDirectoryName(fullDestinationPath)
            ?? throw new InvalidOperationException("The snapshot destination directory is invalid.");
        Directory.CreateDirectory(destinationDirectory);
        var temporaryPath = $"{fullDestinationPath}.{Guid.NewGuid():N}.partial";
        var handle = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, process.ProcessId);
        if (handle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open the Minecraft process for reading.");
        }

        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[Math.Min(BufferSize, process.ModuleSize.Value)];
                var address = process.ModuleBaseAddress.Value;
                var remaining = process.ModuleSize.Value;

                while (remaining > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var requested = Math.Min(buffer.Length, remaining);
                    if (!ReadProcessMemory(handle, new IntPtr(address), buffer, requested, out var bytesRead) || bytesRead.ToInt64() != requested)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the complete loaded module image.");
                    }

                    output.Write(buffer, 0, requested);
                    hash.AppendData(buffer, 0, requested);
                    address = checked(address + requested);
                    remaining -= requested;
                }

                output.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, fullDestinationPath);
            return new ModuleSnapshot(fullDestinationPath, process.ModuleSize.Value, Convert.ToHexString(hash.GetHashAndReset()));
        }
        finally
        {
            CloseHandle(handle);
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
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

public sealed record ModuleSnapshot(string Path, int Size, string Sha256);
