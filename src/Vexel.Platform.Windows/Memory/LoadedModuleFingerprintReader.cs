using System.Diagnostics;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
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
            var sections = ReadExecutableSections(handle, process);
            var buffer = new byte[Math.Min(BufferSize, process.ModuleSize.Value)];
            var hashedSize = 0;
            foreach (var section in sections)
            {
                hash.AppendData(Encoding.ASCII.GetBytes($"{section.Name}\0{section.Rva:X8}\0{section.Size:X8}\0"));
                var address = checked(process.ModuleBaseAddress.Value + section.Rva);
                var remaining = section.Size;

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
                    hashedSize += requested;
                }
            }

            await Task.CompletedTask;
            return new MinecraftBuildFingerprint(
                version,
                version,
                hashedSize,
                Convert.ToHexString(hash.GetHashAndReset()),
                null,
                ExecutableArchitecture.X64,
                FingerprintSource.LoadedExecutableSections);
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private static List<PeSection> ReadExecutableSections(IntPtr handle, MinecraftProcess process)
    {
        const int headerReadSize = 64 * 1024;
        var header = new byte[Math.Min(headerReadSize, process.ModuleSize!.Value)];
        if (!ReadProcessMemory(handle, new IntPtr(process.ModuleBaseAddress!.Value), header, header.Length, out var bytesRead) || bytesRead.ToInt64() != header.Length)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Could not read the PE headers from the loaded module.");
        }

        if (header.Length < 0x40 || header[0] != (byte)'M' || header[1] != (byte)'Z')
        {
            throw new BadImageFormatException("The loaded module does not contain a valid DOS header.");
        }

        var peOffset = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0x3C, sizeof(int)));
        if (peOffset < 0 || peOffset > header.Length - 24 ||
            BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(peOffset, sizeof(uint))) != 0x00004550)
        {
            throw new BadImageFormatException("The loaded module does not contain a valid PE header.");
        }

        var sectionCount = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(peOffset + 6, sizeof(ushort)));
        var optionalHeaderSize = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(peOffset + 20, sizeof(ushort)));
        var sectionOffset = checked(peOffset + 24 + optionalHeaderSize);
        if (sectionCount == 0 || sectionOffset < 0 || sectionOffset > header.Length - (sectionCount * 40))
        {
            throw new BadImageFormatException("The loaded module contains invalid PE section headers.");
        }

        var sections = new List<PeSection>();
        for (var index = 0; index < sectionCount; index++)
        {
            var offset = sectionOffset + (index * 40);
            var virtualSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(offset + 8, sizeof(int)));
            var rva = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(offset + 12, sizeof(int)));
            var rawSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(offset + 16, sizeof(int)));
            var characteristics = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(offset + 36, sizeof(uint)));
            const uint imageScnMemExecute = 0x20000000;
            const uint imageScnMemWrite = 0x80000000;

            if ((characteristics & imageScnMemExecute) == 0 || (characteristics & imageScnMemWrite) != 0)
            {
                continue;
            }

            var size = Math.Max(virtualSize, rawSize);
            if (rva < 0 || size <= 0 || rva > process.ModuleSize.Value - size)
            {
                throw new BadImageFormatException("The loaded module contains an executable section outside its image.");
            }

            var name = Encoding.ASCII.GetString(header, offset, 8).TrimEnd('\0');
            sections.Add(new PeSection(name, rva, size));
        }

        if (sections.Count == 0)
        {
            throw new BadImageFormatException("The loaded module has no executable read-only sections.");
        }

        return sections;
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

    private sealed record PeSection(string Name, int Rva, int Size);
}
