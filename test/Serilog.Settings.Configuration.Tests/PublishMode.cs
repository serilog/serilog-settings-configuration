namespace Serilog.Settings.Configuration.Tests;

/// <summary>
/// The possible application publish modes for the TestApp.
/// See also the <a href="https://learn.microsoft.com/en-us/dotnet/core/deploying/">.NET application publishing overview</a> documentation.
/// </summary>
public enum PublishMode
{
    /// <summary>
    /// Standard app publish, all dlls and related files are copied along the main executable.
    /// </summary>
    Standard,

    /// <summary>
    /// Publish a single file as a framework-dependent binary.
    /// </summary>
    /// <remarks>On .NET Framework, <a href="https://github.com/Fody/Costura">Costura</a> is used to publish as a single file.</remarks>
    SingleFile,

    /// <summary>
    /// Publish a single file as a self contained binary, i.e. including the .NET libraries and target runtime.
    /// </summary>
    /// <remarks>This mode is ignored on .NET Framework as it doesn't make sense.</remarks>
    SelfContained,
}
