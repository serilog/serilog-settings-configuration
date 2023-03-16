using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Xml.XPath;
using CliWrap;
using FluentAssertions;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Serilog.Settings.Configuration.Tests.PublishModeExtensions;

namespace Serilog.Settings.Configuration.Tests;

public class TestApp : IAsyncLifetime
{
    readonly IMessageSink _messageSink;
    readonly DirectoryInfo _workingDirectory;
    readonly List<DirectoryInfo> _directoriesToCleanup;
    readonly Dictionary<PublishMode, FileInfo> _executables;
    readonly IDistributedLock _lock;
    IDistributedSynchronizationHandle? _lockHandle;

    public TestApp(IMessageSink messageSink)
    {
        _messageSink = messageSink;
        _workingDirectory = GetDirectory("test", "TestApp");
        _directoriesToCleanup = new List<DirectoryInfo>();
        _executables = new Dictionary<PublishMode, FileInfo>();
        _lock = new FileDistributedLock(new FileInfo(Path.Combine(_workingDirectory.FullName, "dotnet-restore.lock")));
    }

    public async Task InitializeAsync()
    {
        _lockHandle = await _lock.AcquireAsync();

        foreach (var publishMode in GetPublishModes())
        {
            var outputDirectory = new DirectoryInfo(Path.Combine(_workingDirectory.FullName, TargetFramework, publishMode.ToString()));
            _directoriesToCleanup.Add(outputDirectory.Parent!);

            var restoreArgs = new[] { "restore", $"-p:TargetFrameworks={string.Join("%3B", GetProjectTargetFrameworks().Append(TargetFramework).Distinct())}" };
            await RunDotnetAsync(_workingDirectory, restoreArgs);

            File.WriteAllText(Path.Combine(_workingDirectory.FullName, "FodyWeavers.xml"), publishMode == PublishMode.SingleFile && IsDesktop ? "<Weavers><Costura/></Weavers>" : "<Weavers/>");

            var args = new[] { "publish", "--no-restore", "--configuration", "Release", "--output", outputDirectory.FullName, $"-p:TargetFramework={TargetFramework}" };
            var publishSingleFile = $"-p:PublishSingleFile={publishMode is PublishMode.SingleFile or PublishMode.SelfContained}";
            var selfContained = $"-p:SelfContained={publishMode is PublishMode.SelfContained}";
            await RunDotnetAsync(_workingDirectory, IsDesktop ? args : args.Append(publishSingleFile).Append(selfContained).ToArray());

            var executableFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "TestApp.exe" : "TestApp";
            var executableFile = new FileInfo(Path.Combine(outputDirectory.FullName, executableFileName));
            executableFile.Exists.Should().BeTrue();
            var dlls = executableFile.Directory!.EnumerateFiles("*.dll");
            if (publishMode == PublishMode.Standard)
            {
                dlls.Should().NotBeEmpty(because: $"the test app was _not_ published as single-file ({publishMode})");
            }
            else
            {
                dlls.Should().BeEmpty(because: $"the test app was published as single-file ({publishMode})");
                executableFile.Directory.EnumerateFiles().Should().ContainSingle().Which.FullName.Should().Be(executableFile.FullName);
            }
            _executables[publishMode] = executableFile;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            foreach (var directoryToCleanup in _directoriesToCleanup.Where(e => e.Exists))
            {
                directoryToCleanup.Delete(recursive: true);
            }
        }
        finally
        {
            await _lockHandle!.DisposeAsync();
        }
    }

    public string GetExecutablePath(PublishMode publishMode) => _executables[publishMode].FullName;

    async Task RunDotnetAsync(DirectoryInfo workingDirectory, params string[] arguments)
    {
        _messageSink.OnMessage(new DiagnosticMessage($"cd {workingDirectory}"));
        _messageSink.OnMessage(new DiagnosticMessage($"dotnet {string.Join(" ", arguments)}"));
        var messageSinkTarget = PipeTarget.ToDelegate(line => _messageSink.OnMessage(new DiagnosticMessage(line)));
        await Cli.Wrap("dotnet")
            .WithWorkingDirectory(workingDirectory.FullName)
            .WithArguments(arguments)
            .WithStandardOutputPipe(messageSinkTarget)
            .WithStandardErrorPipe(messageSinkTarget)
            .ExecuteAsync();
    }

    static IEnumerable<string> GetProjectTargetFrameworks()
    {
        var projectFile = GetFile("src", "Serilog.Settings.Configuration", "Serilog.Settings.Configuration.csproj");
        var project = XDocument.Load(projectFile.FullName);
        var targetFrameworks = project.XPathSelectElement("/Project/PropertyGroup/TargetFrameworks") ?? throw new Exception($"TargetFrameworks element not found in {projectFile}");
        return targetFrameworks.Value.Split(';');
    }

    static DirectoryInfo GetDirectory(params string[] paths)
    {
        var directory = new DirectoryInfo(GetFullPath(paths));
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"The {directory.Name} directory must exist at {directory.FullName}");
        }
        return directory;
    }

    static FileInfo GetFile(params string[] paths)
    {
        var file = new FileInfo(GetFullPath(paths));
        if (!file.Exists)
        {
            throw new FileNotFoundException($"The {file.Name} file must exist at {file.FullName}");
        }
        return file;
    }

    static string GetFullPath(params string[] paths) => Path.GetFullPath(Path.Combine(new[] { GetThisDirectory(), "..", ".." }.Concat(paths).ToArray()));

    static string GetThisDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
