using System.Reflection;

namespace Serilog.Settings.Configuration.Assemblies;

class CompositeAssemblyFinder : AssemblyFinder
{
    readonly AssemblyFinder[] _assemblyFinders;

    public CompositeAssemblyFinder(params AssemblyFinder[] assemblyFinders)
    {
        _assemblyFinders = assemblyFinders;
    }

    public override IReadOnlyList<(AssemblyName, string?)> FindAssembliesContainingName(string nameToFind)
    {
        var assemblyNames = new List<(AssemblyName, string?)>();
        foreach (var assemblyFinder in _assemblyFinders)
        {
            assemblyNames.AddRange(assemblyFinder.FindAssembliesContainingName(nameToFind));
        }
        return assemblyNames;
    }
}
