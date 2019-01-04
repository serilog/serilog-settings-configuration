using System;
using System.IO;

using Xunit;

using Serilog.Settings.Configuration.Assemblies;

namespace Serilog.Settings.Configuration.Tests
{
    public class DllScanningAssemblyFinderTests : IDisposable
    {
        readonly string _privateBinPath;

        public DllScanningAssemblyFinderTests()
        {
            var d1 = GetOrCreateDirectory("bin1");
            var d2 = GetOrCreateDirectory("bin2");
            var d3 = GetOrCreateDirectory("bin3");

            _privateBinPath = $"{d1.Name};{d2.FullName};{d3.Name}";

            DirectoryInfo GetOrCreateDirectory(string name)
                => Directory.Exists(name) ? new DirectoryInfo(name) : Directory.CreateDirectory(name);
        }

        public void Dispose()
        {
            Directory.Delete("bin1", true);
            Directory.Delete("bin2", true);
            Directory.Delete("bin3", true);
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
            File.Copy("testdummies.dll", "bin1/customSink1.dll", true);
            File.Copy("testdummies.dll", "bin2/customSink2.dll", true);
            File.Copy("testdummies.dll", "bin3/thirdpartydependency.dll", true);

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

            void DoTestInner()
            {
                var assemblyNames = new DllScanningAssemblyFinder().FindAssembliesContainingName("customSink");
                Assert.Equal(2, assemblyNames.Count);
            }
        }
#endif
    }
}
