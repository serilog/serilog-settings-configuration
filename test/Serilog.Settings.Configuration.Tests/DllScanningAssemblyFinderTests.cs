using System;
using System.IO;

using Xunit;

using Serilog.Settings.Configuration.Assemblies;

namespace Serilog.Settings.Configuration.Tests
{
    public class DllScanningAssemblyFinderTests : IDisposable
    {
        const string BinDir1 = "bin1";
        const string BinDir2 = "bin2";
        const string BinDir3 = "bin3";

        readonly string _privateBinPath;

        public DllScanningAssemblyFinderTests()
        {
            var d1 = GetOrCreateDirectory(BinDir1);
            var d2 = GetOrCreateDirectory(BinDir2);
            var d3 = GetOrCreateDirectory(BinDir3);

            _privateBinPath = $"{d1.Name};{d2.FullName};{d3.Name}";

            DirectoryInfo GetOrCreateDirectory(string name)
                => Directory.Exists(name) ? new DirectoryInfo(name) : Directory.CreateDirectory(name);
        }

        public void Dispose()
        {
            Directory.Delete(BinDir1, true);
            Directory.Delete(BinDir2, true);
            Directory.Delete(BinDir3, true);
        }

        [Fact]
        public void ShouldProbeCurrentDirectory()
        {
            var assemblyNames = new DllScanningAssemblyFinder().FindAssembliesContainingName("testdummies");
            Assert.Single(assemblyNames);
        }

#if PRIVATE_BIN
        [Fact]
        public void ShouldProbePrivateBinPath()
        {
            File.Copy("testdummies.dll", $"{BinDir1}/customSink1.dll", true);
            File.Copy("testdummies.dll", $"{BinDir2}/customSink2.dll", true);
            File.Copy("testdummies.dll", $"{BinDir3}/thirdpartydependency.dll", true);

            var ad = AppDomain.CreateDomain("serilog", null,
                new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    PrivateBinPath = _privateBinPath
                });

            try
            {
                ad.DoCallBack(DoTestInner);
            }
            finally
            {
                AppDomain.Unload(ad);
            }

            static void DoTestInner()
            {
                var assemblyNames = new DllScanningAssemblyFinder().FindAssembliesContainingName("customSink");
                Assert.Equal(2, assemblyNames.Count);
            }
        }
#endif
    }
}
