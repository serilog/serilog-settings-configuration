using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

#if !NET451
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using Microsoft.NET.HostModel.AppHost;
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
                if (currentProcessPath != null && HostWriter.IsBundle(currentProcessPath, out var bundleHeaderOffset))
                {
                    using var mappedFile = MemoryMappedFile.CreateFromFile(currentProcessPath, FileMode.Open);
                    using var currentProcessStream = mappedFile.CreateViewStream(0, new FileInfo(currentProcessPath).Length, MemoryMappedFileAccess.Read);
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
                        using var depsJsonStream = mappedFile.CreateViewStream(depsJsonOffset, depsJsonSize, MemoryMappedFileAccess.Read);
                        using var depsJsonReader = new DependencyContextJsonReader();
                        return depsJsonReader.Read(depsJsonStream);
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
