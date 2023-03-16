using System.Reflection;

namespace Serilog.Settings.Configuration.Assemblies;

class AutoAssemblyFinder : AssemblyFinder
{
    readonly AssemblyFinder[] _assemblyFinders;

    public AutoAssemblyFinder(params AssemblyFinder[] assemblyFinders)
    {
        _assemblyFinders = assemblyFinders;
    }

    public override IReadOnlyList<AssemblyName> FindAssembliesContainingName(string nameToFind)
    {
        var assemblyNames = new List<AssemblyName>();
        foreach (var assemblyFinder in _assemblyFinders)
        {
            assemblyNames.AddRange(assemblyFinder.FindAssembliesContainingName(nameToFind));
        }
        return assemblyNames;
    }
}
