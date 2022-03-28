using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

#if !NET451
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
#endif

namespace Serilog.Settings.Configuration.Assemblies
{
    abstract class AssemblyFinder
    {
        public abstract IReadOnlyList<AssemblyName> FindAssembliesContainingName(string nameToFind);

        protected static bool IsCaseInsensitiveMatch(string text, string textToFind)
        {
            return text != null && text.ToLowerInvariant().Contains(textToFind.ToLowerInvariant());
        }

        public static AssemblyFinder Auto()
        {
            var dependencyContext = GetDependencyContext();
            return dependencyContext != null ? new DependencyContextAssemblyFinder(dependencyContext) : new DllScanningAssemblyFinder();
        }

        public static AssemblyFinder ForSource(ConfigurationAssemblySource configurationAssemblySource)
        {
            return configurationAssemblySource switch
            {
                ConfigurationAssemblySource.UseLoadedAssemblies => Auto(),
                ConfigurationAssemblySource.AlwaysScanDllFiles => new DllScanningAssemblyFinder(),
                _ => throw new ArgumentOutOfRangeException(nameof(configurationAssemblySource), configurationAssemblySource, null),
            };
        }

        public static AssemblyFinder ForDependencyContext(DependencyContext dependencyContext)
        {
            return new DependencyContextAssemblyFinder(dependencyContext);
        }

        static DependencyContext GetDependencyContext()
        {
            var isBundled = string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location);
            if (!isBundled)
            {
                return DependencyContext.Default;
            }

#if !NET451
            try
            {
                var currentProcessPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (currentProcessPath != null)
                {
                    var currentProcessExeLength = new FileInfo(currentProcessPath).Length;
                    using var currentProcessFile = MemoryMappedFile.CreateFromFile(currentProcessPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                    using var currentProcessView = currentProcessFile.CreateViewAccessor(0, currentProcessExeLength, MemoryMappedFileAccess.Read);
                    // See https://github.com/dotnet/runtime/blob/v6.0.3/src/installer/managed/Microsoft.NET.HostModel/AppHost/HostWriter.cs#L216
                    byte[] bundleSignature = {
                        // 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
                        0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38, 0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
                        0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18, 0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
                    };
                    // Can't use BinaryUtils.SearchInFile(currentProcessPath, bundleSignature) because it calls MemoryMappedFile.CreateFromFile(currentProcessPath)
                    // without specifying `MemoryMappedFileAccess.Read` which eventually cause an IO exception to be thrown on Windows:
                    // > System.IO.IOException: The process cannot access the file
                    // Note: HostWriter.IsBundle(currentProcessPath, out var bundleHeaderOffset) calls BinaryUtils.SearchInFile(currentProcessPath, bundleSignature)
                    // So the internal SearchInFile that takes a MemoryMappedViewAccessor is used instead
                    // Using this internal method is a proof of concept, it needs to be properly rewritten and thus the `Microsoft.NET.HostModel` dependency can be removed.
                    // internal static unsafe int SearchInFile(MemoryMappedViewAccessor accessor, byte[] searchPattern)
                    const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                    var parameterTypes = new[] { typeof(MemoryMappedViewAccessor), typeof(byte[]) };
                    var searchInFile = typeof(Microsoft.NET.HostModel.AppHost.BinaryUtils).GetMethod("SearchInFile", bindingFlags, null, parameterTypes, null);
                    var bundleSignatureIndex = (int?)searchInFile?.Invoke(null, new object[] { currentProcessView, bundleSignature }) ?? -1;
                    if (bundleSignatureIndex > 0 && bundleSignatureIndex < currentProcessExeLength)
                    {
                        var bundleHeaderOffset = currentProcessView.ReadInt64(bundleSignatureIndex - 8);
                        using var currentProcessStream = currentProcessFile.CreateViewStream(0, currentProcessExeLength, MemoryMappedFileAccess.Read);
                        using var reader = new BinaryReader(currentProcessStream, Encoding.UTF8);
                        // See https://github.com/dotnet/runtime/blob/v6.0.3/src/installer/managed/Microsoft.NET.HostModel/Bundle/Manifest.cs#L32-L39
                        // and https://github.com/dotnet/runtime/blob/v6.0.3/src/installer/managed/Microsoft.NET.HostModel/Bundle/Manifest.cs#L144-L155
                        reader.BaseStream.Position = bundleHeaderOffset;
                        var majorVersion = reader.ReadUInt32();
                        _ = reader.ReadUInt32(); // minorVersion
                        _ = reader.ReadInt32(); // numEmbeddedFiles
                        _ = reader.ReadString(); // bundleId
                        if (majorVersion >= 2)
                        {
                            var depsJsonOffset = reader.ReadInt64();
                            var depsJsonSize = reader.ReadInt64();
                            using var depsJsonStream = currentProcessFile.CreateViewStream(depsJsonOffset, depsJsonSize, MemoryMappedFileAccess.Read);
                            using var depsJsonReader = new DependencyContextJsonReader();
                            return depsJsonReader.Read(depsJsonStream);
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
#endif

            return null;
        }
    }
}
