using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace TouchRpc.Infrastructure;

internal readonly record struct FileIdentity(ulong Volume, ulong Index, long CreationTicks)
{
    public static FileIdentity From(FileStream stream, string path)
    {
        if (OperatingSystem.IsWindows() && GetFileInformationByHandle(stream.SafeFileHandle, out var info))
        {
            var index = ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow;
            return new FileIdentity(info.VolumeSerialNumber, index, 0);
        }

        return new FileIdentity(0, 0, File.GetCreationTimeUtc(path).Ticks);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle fileHandle,
        out ByHandleFileInformation fileInformation);

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }
}