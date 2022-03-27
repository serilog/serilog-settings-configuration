#if !NETCOREAPP2_1
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Serilog.Settings.Configuration.Tests.Support;
using Xunit;

namespace Serilog.Settings.Configuration.Tests
{
    public class SingleFileAppTest
    {
        [Fact]
        void SingleFileApp()
        {
            var testDirectory = new DirectoryInfo(GetCurrentFilePath()).Parent?.Parent ?? throw new DirectoryNotFoundException("Can't find the 'test' directory");
            var workingDirectory = Path.Combine(testDirectory.FullName, "TestSingleFileApp");
            var publishDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                ProcessExtensions.RunDotnet(workingDirectory, "publish", "-c", "Release", "-o", publishDirectory);
                var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "TestSingleFileApp.exe" : "TestSingleFileApp";
                var exePath = Path.Combine(publishDirectory, exeName);
                var result = ProcessExtensions.RunCommand(exePath);

                Assert.Matches("^$", result.Error);
                Assert.Equal("Everything is working as expected", result.Output);
            }
            finally
            {
                try
                {
                    Directory.Delete(publishDirectory, recursive: true);
                }
                catch
                {
                    // Don't hide the actual exception, if any
                }
            }
        }

        static string GetCurrentFilePath([CallerFilePath] string path = "") => path;
    }
}
#endif
