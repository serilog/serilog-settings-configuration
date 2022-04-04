using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Serilog.Settings.Configuration.Assemblies
{
    static class SingleFileApplication
    {
        static byte[] bundleSignature =
        {
            // 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
            // The first byte is actually 0x8b but we don't want to accidentally have a second place where the bundle
            // signature can appear in the single file application so the first byte is set in the static constructor
            // See https://github.com/dotnet/runtime/blob/v6.0.3/src/installer/managed/Microsoft.NET.HostModel/AppHost/HostWriter.cs#L216-L222
            0x00, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38, 0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
            0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18, 0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
        };

        static SingleFileApplication()
        {
            bundleSignature[0] = 0x8b;
        }

        public static Stream GetDepsJsonStream()
        {
            var appHostPath = Process.GetCurrentProcess().MainModule?.FileName;
            if (appHostPath == null)
            {
                return null;
            }

            using var appHostFile = MemoryMappedFile.CreateFromFile(appHostPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using Stream appHostStream = appHostFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            var bundleSignatureIndex = SearchBundleSignature(appHostStream);
            if (bundleSignatureIndex == -1)
            {
                return null;
            }

            using var appHostReader = new BinaryReader(appHostStream);
            appHostReader.BaseStream.Position = bundleSignatureIndex - 8;
            var bundleHeaderOffset = appHostReader.ReadInt64();
            if (bundleHeaderOffset <= 0 || bundleHeaderOffset >= appHostStream.Length)
            {
                return null;
            }

            // See https://github.com/dotnet/runtime/blob/v6.0.3/src/installer/managed/Microsoft.NET.HostModel/Bundle/Manifest.cs#L32-L39
            // and https://github.com/dotnet/runtime/blob/v6.0.3/src/installer/managed/Microsoft.NET.HostModel/Bundle/Manifest.cs#L144-L155
            appHostReader.BaseStream.Position = bundleHeaderOffset;
            var majorVersion = appHostReader.ReadUInt32();
            _ = appHostReader.ReadUInt32(); // minorVersion
            var numEmbeddedFiles = appHostReader.ReadInt32();
            _ = appHostReader.ReadString(); // bundleId
            if (majorVersion >= 2)
            {
                var depsJsonOffset = appHostReader.ReadInt64();
                var depsJsonSize = appHostReader.ReadInt64();
                return appHostFile.CreateViewStream(depsJsonOffset, depsJsonSize, MemoryMappedFileAccess.Read);
            }

            // For version < 2 all the file entries must be enumerated until the `DepsJson` type is found
            // See https://github.com/dotnet/runtime/blob/v6.0.3/src/installer/managed/Microsoft.NET.HostModel/Bundle/FileEntry.cs#L43-L54
            for (var i = 0; i < numEmbeddedFiles; i++)
            {
                var offset = appHostReader.ReadInt64();
                var size = appHostReader.ReadInt64();
                var type = appHostReader.ReadByte();
                _ = appHostReader.ReadString(); // relativePath
                if (type == 3)
                {
                    // type 3 is the .deps.json configuration file
                    // See https://github.com/dotnet/runtime/blob/v6.0.3/src/installer/managed/Microsoft.NET.HostModel/Bundle/FileType.cs#L17
                    return appHostFile.CreateViewStream(offset, size, MemoryMappedFileAccess.Read);
                }
            }

            return null;
        }

        static int SearchBundleSignature(Stream stream)
        {
            var m = 0;
            var i = 0;

            var length = stream.Length;
            while (m + i < length)
            {
                stream.Position = m + i;
                if (bundleSignature[i] == stream.ReadByte())
                {
                    if (i == bundleSignature.Length - 1)
                    {
                        return m;
                    }
                    i++;
                }
                else
                {
                    m += i == 0 ? 1 : i;
                    i = 0;
                }
            }

            return -1;
        }
    }
}
