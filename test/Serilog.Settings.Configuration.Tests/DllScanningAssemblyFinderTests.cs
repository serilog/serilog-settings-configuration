#if NETFRAMEWORK
using System;
using System.IO;
#endif

using Serilog.Settings.Configuration.Assemblies;

namespace Serilog.Settings.Configuration.Tests;

public class DllScanningAssemblyFinderTests
{
    const string BinDir1 = "bin1";
    const string BinDir2 = "bin2";
    const string BinDir3 = "bin3";

    [Fact]
    public void ShouldProbeCurrentDirectory()
    {
        var assemblyNames = new DllScanningAssemblyFinder().FindAssembliesContainingName("TestDummies");
        Assert.Single(assemblyNames);
    }

#if NETFRAMEWORK
    [Fact]
    public void ShouldProbePrivateBinPath()
    {
        var d1 = GetOrCreateDirectory(BinDir1);
        var d2 = GetOrCreateDirectory(BinDir2);
        var d3 = GetOrCreateDirectory(BinDir3);

        DirectoryInfo GetOrCreateDirectory(string name)
            => Directory.Exists(name) ? new DirectoryInfo(name) : Directory.CreateDirectory(name);

        File.Copy("TestDummies.dll", $"{BinDir1}/customSink1.dll", true);
        File.Copy("TestDummies.dll", $"{BinDir2}/customSink2.dll", true);
        File.Copy("TestDummies.dll", $"{BinDir3}/thirdpartydependency.dll", true);

        var ad = AppDomain.CreateDomain("serilog", null,
            new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                PrivateBinPath = $"{d1.Name};{d2.FullName};{d3.Name}"
            });

        try
        {
            ad.DoCallBack(DoTestInner);
        }
        finally
        {
            AppDomain.Unload(ad);
            Directory.Delete(BinDir1, true);
            Directory.Delete(BinDir2, true);
            Directory.Delete(BinDir3, true);
        }

        static void DoTestInner()
        {
            var assemblyNames = new DllScanningAssemblyFinder().FindAssembliesContainingName("customSink");
            Assert.Equal(2, assemblyNames.Count);
        }
    }
#endif
}
