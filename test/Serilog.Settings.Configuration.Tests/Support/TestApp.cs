using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CliWrap;
using CliWrap.Exceptions;
using FluentAssertions;
using Polly;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Serilog.Settings.Configuration.Tests.Support.PublishModeExtensions;

namespace Serilog.Settings.Configuration.Tests.Support;

public class TestApp : IAsyncLifetime
{
    readonly IMessageSink _messageSink;
    readonly DirectoryInfo _workingDirectory;
    readonly Dictionary<PublishMode, FileInfo> _executables;

    public TestApp(IMessageSink messageSink)
    {
        _messageSink = messageSink;
        _workingDirectory = GetDirectory("test", $"TestApp-{TargetFramework}");
        _workingDirectory.Create();
        foreach (var file in GetDirectory("test", "TestApp").EnumerateFiles())
        {
            file.CopyTo(_workingDirectory.File(file.Name).FullName, overwrite: true);
        }
        _executables = new Dictionary<PublishMode, FileInfo>();
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        // Retry 3 times because pack / restore / publish may try to access the same files across different target frameworks and fail with System.IO.IOException:
        // The process cannot access the file [Serilog.Settings.Configuration.deps.json or Serilog.Settings.Configuration.dll] because it is being used by another process.
        var retryPolicy = Policy.Handle<CommandExecutionException>().RetryAsync(3);
        await retryPolicy.ExecuteAsync(CreateTestAppAsync);
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        _workingDirectory.Delete(recursive: true);
        return Task.CompletedTask;
    }

    public string GetExecutablePath(PublishMode publishMode) => _executables[publishMode].FullName;

    async Task CreateTestAppAsync()
    {
        await PackAsync();
        await RestoreAsync();

        var publishDirectory = _workingDirectory.SubDirectory("publish");
        var fodyWeaversXml = _workingDirectory.File("FodyWeavers.xml");

        foreach (var publishMode in GetPublishModes())
        {
            var outputDirectory = publishDirectory.SubDirectory(publishMode.ToString());

            File.WriteAllText(fodyWeaversXml.FullName, publishMode == PublishMode.SingleFile && IsDesktop ? "<Weavers><Costura/></Weavers>" : "<Weavers/>");

            var publishArgs = new[] {
                "publish",
                "--no-restore",
                "--configuration", "Release",
                "--output", outputDirectory.FullName,
                $"-p:TargetFramework={TargetFramework}"
            };
            var publishSingleFile = $"-p:PublishSingleFile={publishMode is PublishMode.SingleFile or PublishMode.SelfContained}";
            var selfContained = $"-p:SelfContained={publishMode is PublishMode.SelfContained}";
            await RunDotnetAsync(_workingDirectory, IsDesktop ? publishArgs : publishArgs.Append(publishSingleFile).Append(selfContained).ToArray());

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

    async Task PackAsync()
    {
        var projectFile = GetFile("src", "Serilog.Settings.Configuration", "Serilog.Settings.Configuration.csproj");
        var packArgs = new[] {
            "pack", projectFile.FullName,
            "--configuration", "Release",
            "--output", _workingDirectory.FullName,
            "-p:Version=0.0.0-IntegrationTest.0",
        };
        await RunDotnetAsync(_workingDirectory, packArgs);
    }

    async Task RestoreAsync()
    {
        var packagesDirectory = _workingDirectory.SubDirectory("packages");
        var restoreArgs = new[] {
            "restore",
            "--packages", packagesDirectory.FullName,
            "--source", ".",
            "--source", "https://api.nuget.org/v3/index.json",
            "-p:Configuration=Release",
            $"-p:TargetFramework={TargetFramework}"
        };
        await RunDotnetAsync(_workingDirectory, restoreArgs);
    }

    async Task RunDotnetAsync(DirectoryInfo workingDirectory, params string[] arguments)
    {
        _messageSink.OnMessage(new DiagnosticMessage($"cd {workingDirectory}"));
        _messageSink.OnMessage(new DiagnosticMessage($"dotnet {string.Join(" ", arguments)}"));
        var outBuilder = new StringBuilder();
        var errBuilder = new StringBuilder();
        var command = Cli.Wrap("dotnet")
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(workingDirectory.FullName)
            .WithArguments(arguments)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                outBuilder.AppendLine(line);
                _messageSink.OnMessage(new DiagnosticMessage($"==> out: {line}"));
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
            {
                errBuilder.AppendLine(line);
                _messageSink.OnMessage(new DiagnosticMessage($"==> err: {line}"));
            }));

        var result = await command.ExecuteAsync();
        if (result.ExitCode != 0)
        {
            throw new CommandExecutionException(command, result.ExitCode, $"An unexpected exception has occurred while running {command}{Environment.NewLine}{errBuilder}{outBuilder}".Trim());
        }
    }

    static DirectoryInfo GetDirectory(params string[] paths) => new(GetFullPath(paths));

    static FileInfo GetFile(params string[] paths) => new(GetFullPath(paths));

    static string GetFullPath(params string[] paths) => Path.GetFullPath(Path.Combine(new[] { GetThisDirectory(), "..", "..", ".." }.Concat(paths).ToArray()));

    static string GetThisDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
