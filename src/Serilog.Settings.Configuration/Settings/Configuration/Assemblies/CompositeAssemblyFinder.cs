using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Serilog.Settings.Configuration.Assemblies
{
    class CompositeAssemblyFinder : AssemblyFinder
    {
        readonly AssemblyFinder[] _assemblyFinders;

        public CompositeAssemblyFinder(params AssemblyFinder[] assemblyFinders)
        {
            if (assemblyFinders == null) throw new ArgumentNullException(nameof(assemblyFinders));
            if (assemblyFinders.Length == 0) throw new ArgumentException("The assembly finders must not be empty", nameof(assemblyFinders));
            _assemblyFinders = assemblyFinders;
        }

        public override IReadOnlyList<AssemblyName> FindAssembliesContainingName(string nameToFind)
        {
            var assemblyNames = new HashSet<AssemblyName>(SimpleNameComparer.Instance);
            foreach (var assemblyFinder in _assemblyFinders)
            {
                foreach (var assemblyName in assemblyFinder.FindAssembliesContainingName(nameToFind))
                {
                    assemblyNames.Add(assemblyName);
                }
            }
            return assemblyNames.ToList();
        }

        class SimpleNameComparer : IEqualityComparer<AssemblyName>
        {
            public static SimpleNameComparer Instance = new();

            public bool Equals(AssemblyName x, AssemblyName y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name;
            }

            public int GetHashCode(AssemblyName obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
