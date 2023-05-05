namespace Serilog.Settings.Configuration.Tests;

static class DirectoryInfoExtensions
{
    public static DirectoryInfo SubDirectory(this DirectoryInfo directory, params string[] paths)
        => new(Path.GetFullPath(Path.Combine(paths.Prepend(directory.FullName).ToArray())));

    public static FileInfo File(this DirectoryInfo directory, params string[] paths)
        => new(Path.GetFullPath(Path.Combine(paths.Prepend(directory.FullName).ToArray())));
}
