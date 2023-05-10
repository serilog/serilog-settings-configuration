using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Serilog.Settings.Configuration.Assemblies;

sealed class DependencyContextAssemblyFinder : AssemblyFinder
{
    readonly DependencyContext? _dependencyContext;

    public DependencyContextAssemblyFinder(DependencyContext? dependencyContext)
    {
        _dependencyContext = dependencyContext;
    }

    public override IReadOnlyList<AssemblyName> FindAssembliesContainingName(string nameToFind)
    {
        if (_dependencyContext == null)
            return Array.Empty<AssemblyName>();

        var query = from library in _dependencyContext.RuntimeLibraries
                    where IsReferencingSerilog(library)
                    from assemblyName in library.GetDefaultAssemblyNames(_dependencyContext)
                    where IsCaseInsensitiveMatch(assemblyName.Name, nameToFind)
                    select assemblyName;

        return query.ToList();

        static bool IsReferencingSerilog(Library library)
        {
            const string Serilog = "serilog";
            return library.Dependencies.Any(dependency =>
                dependency.Name.StartsWith(Serilog, StringComparison.OrdinalIgnoreCase) &&
               (dependency.Name.Length == Serilog.Length || dependency.Name[Serilog.Length] == '.'));
        }
    }
}
