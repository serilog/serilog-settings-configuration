using System.Diagnostics;
using System.Text;
using CliWrap;
using CliWrap.Exceptions;
using Serilog.Settings.Configuration.Tests.Support;
using Xunit.Abstractions;

namespace Serilog.Settings.Configuration.Tests;

[Trait("Category", "Integration")]
public sealed class PublishSingleFileTests : IClassFixture<TestApp>
{
    readonly ITestOutputHelper _outputHelper;
    readonly TestApp _testApp;

    public PublishSingleFileTests(ITestOutputHelper outputHelper, TestApp testApp)
    {
        _outputHelper = outputHelper;
        _testApp = testApp;
    }

    [Theory]
    [ClassData(typeof(PublishModeTheoryData))]
    public async Task RunTestApp_NoUsingAndNoAssembly(PublishMode publishMode)
    {
        var (isSingleFile, stdOut, stdErr) = await RunTestAppAsync(publishMode);
        Assert.Equal(stdOut, isSingleFile ? "Expected exception" : "(Main thread) [Information] Expected success");
        Assert.Empty(stdErr);
    }

    [Theory]
    [ClassData(typeof(PublishModeTheoryData))]
    public async Task RunTestApp_UsingConsole(PublishMode publishMode)
    {
        var (isSingleFile, stdOut, stdErr) = await RunTestAppAsync(publishMode, "--using-console");
        Assert.Equal(stdOut, isSingleFile ? "() [Information] Expected success" : "(Main thread) [Information] Expected success");
        if (isSingleFile)
            Assert.Contains("Unable to find a method called WithThreadName", stdErr);
        else
            Assert.Empty(stdErr);
    }

    [Theory]
    [ClassData(typeof(PublishModeTheoryData))]
    public async Task RunTestApp_UsingThread(PublishMode publishMode)
    {
        var (isSingleFile, stdOut, stdErr) = await RunTestAppAsync(publishMode, "--using-thread");
        Assert.Equal(stdOut, isSingleFile ? "" : "(Main thread) [Information] Expected success");
        if (isSingleFile)
            Assert.Contains("Unable to find a method called Console", stdErr);
        else
            Assert.Empty(stdErr);
    }

    [Theory]
    [ClassData(typeof(PublishModeTheoryData))]
    public async Task RunTestApp_AssemblyThread(PublishMode publishMode)
    {
        var (_, stdOut, stdErr) = await RunTestAppAsync(publishMode, "--assembly-thread");
        Assert.Empty(stdOut);
        Assert.Contains("Unable to find a method called Console", stdErr);
    }

    [Theory]
    [ClassData(typeof(PublishModeTheoryData))]
    public async Task RunTestApp_AssemblyConsole(PublishMode publishMode)
    {
        var (_, stdOut, stdErr) = await RunTestAppAsync(publishMode, "--assembly-console");
        Assert.Equal("() [Information] Expected success", stdOut);
        Assert.Contains("Unable to find a method called WithThreadName", stdErr);
    }

    [Theory]
    [ClassData(typeof(PublishModeAndStrategyTheoryData))]
    public async Task RunTestApp_ConsoleAndThread(PublishMode publishMode, string strategy)
    {
        var (_, stdOut, stdErr) = await RunTestAppAsync(publishMode, $"--{strategy}-console", $"--{strategy}-thread");
        Assert.Equal("(Main thread) [Information] Expected success", stdOut);
        Assert.Empty(stdErr);
    }

    [Theory]
    [ClassData(typeof(PublishModeTheoryData))]
    public async Task RunTestApp_ConfigureMinimumLevelOnly(PublishMode publishMode)
    {
        var (_, stdOut, stdErr) = await RunTestAppAsync(publishMode, "--minimum-level-only");
        Assert.Equal("(Main thread) [Information] Expected success", stdOut);
        Assert.Empty(stdErr);
    }

    async Task<(bool IsSingleFile, string StdOut, string StdErr)> RunTestAppAsync(PublishMode publishMode, params string[] args)
    {
        // Determine whether the app is a _true_ single file, i.e. not a .NET Core 3.x version which
        // [extracts bundled files to disk][1] and thus can find dlls.
        // [1]: https://github.com/dotnet/designs/blob/main/accepted/2020/single-file/extract.md
        var (isSingleFile, _) = await RunTestAppInternalAsync(publishMode, "is-single-file");
        var (stdOut, stdErr) = await RunTestAppInternalAsync(publishMode, args);
        return (bool.Parse(isSingleFile), stdOut, stdErr);
    }

    async Task<(string StdOut, string StdErr)> RunTestAppInternalAsync(PublishMode publishMode, params string[] args)
    {
        var stdOutBuilder = new StringBuilder();
        var stdErrBuilder = new StringBuilder();

        var command = Cli.Wrap(_testApp.GetExecutablePath(publishMode))
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuilder))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuilder));

        _outputHelper.WriteLine(command.ToString());

        var stopwatch = Stopwatch.StartNew();
        var result = await command.ExecuteAsync();
        var executionTime = stopwatch.ElapsedMilliseconds;

        var stdOut = stdOutBuilder.ToString().Trim();
        var stdErr = stdErrBuilder.ToString().Trim();

        _outputHelper.WriteLine($"Executed in {executionTime} ms");
        _outputHelper.WriteLine(stdOut.Length > 0 ? $"stdout: {stdOut}" : "nothing on stdout");
        _outputHelper.WriteLine(stdErr.Length > 0 ? $"stderr: {stdErr}" : "nothing on stderr");
        _outputHelper.WriteLine("");

        if (result.ExitCode != 0)
        {
            throw new CommandExecutionException(command, result.ExitCode, $"An unexpected exception has occurred while running {command}{Environment.NewLine}{stdErr}".Trim());
        }

        return (stdOut, stdErr);
    }

    class PublishModeTheoryData : TheoryData<PublishMode>
    {
        public PublishModeTheoryData()
        {
            foreach (var publishMode in PublishModeExtensions.GetPublishModes())
            {
                Add(publishMode);
            }
        }
    }

    class PublishModeAndStrategyTheoryData : TheoryData<PublishMode, string>
    {
        public PublishModeAndStrategyTheoryData()
        {
            foreach (var publishMode in PublishModeExtensions.GetPublishModes())
            {
                foreach (var strategy in new[] { "using", "assembly" })
                {
                    Add(publishMode, strategy);
                }
            }
        }
    }
}
