using System.Reflection;
using System.Runtime.Versioning;
using NuGet.Frameworks;

namespace Serilog.Settings.Configuration.Tests.Support;

public static class PublishModeExtensions
{
    static PublishModeExtensions()
    {
        var targetFrameworkAttribute = typeof(TestApp).Assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        if (targetFrameworkAttribute == null)
        {
            throw new Exception($"Assembly {typeof(TestApp).Assembly} does not have a {nameof(TargetFrameworkAttribute)}");
        }

        var framework = NuGetFramework.Parse(targetFrameworkAttribute.FrameworkName);

        TargetFramework = framework.GetShortFolderName();
        IsDesktop = framework.IsDesktop();
    }

    public static bool IsDesktop { get; }

    public static string TargetFramework { get; }

    public static IEnumerable<PublishMode> GetPublishModes()
    {
        return IsDesktop ? new[] { PublishMode.Standard, PublishMode.SingleFile } : Enum.GetValues(typeof(PublishMode)).Cast<PublishMode>();
    }
}
