using System;
using System.Diagnostics;

namespace Farplane;

public static class LegacyMemoryReader
{
    static IntPtr _memoryHandle;
    static Process _memoryProcess;

    static bool _isAttached =>
        _memoryHandle != IntPtr.Zero && _memoryProcess != null && !_memoryProcess.HasExited;

    internal static void Attach(Process process)
    {
        Detach();

        // Check if valid process
        if (process == null || process.HasExited)
        {
            return;
        }

        // Open memory for reading and store handle
        var openHandle = WinAPI.OpenProcess(
            ProcessAccessFlags.VirtualMemoryOperation
                | ProcessAccessFlags.VirtualMemoryRead
                | ProcessAccessFlags.VirtualMemoryWrite,
            false,
            process.Id
        );

        if (openHandle == IntPtr.Zero)
        {
            return;
        }

        _memoryProcess = process;
        _memoryHandle = openHandle;
    }

    internal static bool CheckProcess()
    {
        if (_memoryProcess == null || _memoryProcess.HasExited)
        {
            Detach();
            return false;
        }
        return true;
    }

    internal static void Detach()
    {
        _memoryProcess?.Dispose();
        _memoryProcess = null;
        _memoryHandle = IntPtr.Zero;
    }

    public static int ReadInt32(int address, bool pointer = false)
    {
        var bytes = ReadBytes(address, 4, pointer);
        return BitConverter.ToInt32(bytes, 0);
    }

    public static short ReadInt16(int address, bool pointer = false)
    {
        var bytes = ReadBytes(address, 2, pointer);
        return BitConverter.ToInt16(bytes, 0);
    }

    public static uint ReadUInt32(int address, bool pointer = false)
    {
        var bytes = ReadBytes(address, 4, pointer);
        return BitConverter.ToUInt32(bytes, 0);
    }

    public static ushort ReadUInt16(int address, bool pointer = false)
    {
        var bytes = ReadBytes(address, 2, pointer);
        return BitConverter.ToUInt16(bytes, 0);
    }

    public static bool ReadByteFlag(int address, bool pointer = false)
    {
        var bytes = ReadBytes(address, 1, pointer);
        return bytes[0] == 1;
    }

    public static byte ReadByte(int address, bool pointer = false)
    {
        var bytes = ReadBytes(address, 1, pointer);
        return bytes[0];
    }

    public static byte[] ReadBytes(int address, int length, bool pointer = false)
    {
        if (!_isAttached)
        {
            return null;
        }

        var readBuffer = new byte[length];

        WinAPI.ReadProcessMemory(
            _memoryHandle,
            (pointer ? (IntPtr)0 : _memoryProcess.MainModule.BaseAddress) + address,
            readBuffer,
            length,
            out _
        );
        return readBuffer;
    }

    public static bool WriteBytes(int address, byte[] bytes, bool pointer = false)
    {
        if (!_isAttached)
        {
            return false;
        }

        var writeBuffer = new byte[bytes.Length];
        bytes.CopyTo(writeBuffer, 0);

        var success = WinAPI.WriteProcessMemory(
            _memoryHandle,
            (pointer ? (IntPtr)0 : _memoryProcess.MainModule.BaseAddress) + address,
            writeBuffer,
            writeBuffer.Length,
            out _
        );

        return success;
    }

    public static bool WriteByte(int address, byte bytes, bool pointer = false)
    {
        if (!_isAttached)
        {
            return false;
        }

        var success = WinAPI.WriteProcessMemory(
            _memoryHandle,
            (pointer ? (IntPtr)0 : _memoryProcess.MainModule.BaseAddress) + address,
            [bytes],
            1,
            out _
        );

        return success;
    }
}
