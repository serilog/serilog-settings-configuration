using System.Diagnostics;
using System.Text;
using CliWrap;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit.Abstractions;

namespace Serilog.Settings.Configuration.Tests;

public sealed class PublishSingleFileTests : IDisposable, IClassFixture<TestApp>
{
    readonly ITestOutputHelper _outputHelper;
    readonly TestApp _testApp;
    readonly AssertionScope _scope;

    public PublishSingleFileTests(ITestOutputHelper outputHelper, TestApp testApp)
    {
        _outputHelper = outputHelper;
        _testApp = testApp;
        _scope = new AssertionScope();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    [Theory]
    [CombinatorialData]
    public async Task RunTestApp_NoUsingAndNoAssembly(bool singleFile)
    {
        var (isSingleFile, stdOut, stdErr) = await RunTestAppAsync(singleFile);
        stdOut.Should().Be(isSingleFile ? "Expected exception" : "(Main thread) [Information] Expected success");
        stdErr.Should().BeEmpty();
    }

    [Theory]
    [CombinatorialData]
    public async Task RunTestApp_UsingConsole(bool singleFile)
    {
        var (isSingleFile, stdOut, stdErr) = await RunTestAppAsync(singleFile, "--using-console");
        stdOut.Should().Be(isSingleFile ? "() [Information] Expected success" : "(Main thread) [Information] Expected success");
        if (isSingleFile)
            stdErr.Should().Contain("Unable to find a method called WithThreadName");
        else
            stdErr.Should().BeEmpty();
    }

    [Theory]
    [CombinatorialData]
    public async Task RunTestApp_UsingThread(bool singleFile)
    {
        var (isSingleFile, stdOut, stdErr) = await RunTestAppAsync(singleFile, "--using-thread");
        stdOut.Should().Be(isSingleFile ? "" : "(Main thread) [Information] Expected success");
        if (isSingleFile)
            stdErr.Should().Contain("Unable to find a method called Console");
        else
            stdErr.Should().BeEmpty();
    }

    [Theory]
    [CombinatorialData]
    public async Task RunTestApp_AssemblyThread(bool singleFile)
    {
        var (_, stdOut, stdErr) = await RunTestAppAsync(singleFile, "--assembly-thread");
        stdOut.Should().BeEmpty();
        stdErr.Should().Contain("Unable to find a method called Console");
    }

    [Theory]
    [CombinatorialData]
    public async Task RunTestApp_AssemblyConsole(bool singleFile)
    {
        var (_, stdOut, stdErr) = await RunTestAppAsync(singleFile, "--assembly-console");
        stdOut.Should().Be("() [Information] Expected success");
        stdErr.Should().Contain("Unable to find a method called WithThreadName");
    }

    [Theory]
    [CombinatorialData]
    public async Task RunTestApp_ConsoleAndThread(bool singleFile, [CombinatorialValues("using", "assembly")] string strategy)
    {
        var (_, stdOut, stdErr) = await RunTestAppAsync(singleFile, $"--{strategy}-console", $"--{strategy}-thread");
        stdOut.Should().Be("(Main thread) [Information] Expected success");
        stdErr.Should().BeEmpty();
    }

    async Task<(bool IsSingleFile, string StdOut, string StdErr)> RunTestAppAsync(bool singleFile, params string[] args)
    {
        // Determine whether the app is a _true_ single file, i.e. not a .NET Core 3.x version which
        // [extracts bundled files to disk][1] and thus can find dlls.
        // [1]: https://github.com/dotnet/designs/blob/main/accepted/2020/single-file/extract.md
        var (isSingleFile, _) = await RunTestAppInternalAsync(singleFile, "is-single-file");
        var (stdOut, stdErr) = await RunTestAppInternalAsync(singleFile, args);
        return (bool.Parse(isSingleFile), stdOut, stdErr);
    }

    async Task<(string StdOut, string StdErr)> RunTestAppInternalAsync(bool singleExe, params string[] args)
    {
        var stdOutBuilder = new StringBuilder();
        var stdErrBuilder = new StringBuilder();

        var command = Cli.Wrap(singleExe ? _testApp.SingleFileExe.FullName : _testApp.StandardExe.FullName)
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
            throw new Exception($"An unexpected exception has occurred while running {command}. {stdErr}".Trim());
        }

        return (stdOut, stdErr);
    }
}
