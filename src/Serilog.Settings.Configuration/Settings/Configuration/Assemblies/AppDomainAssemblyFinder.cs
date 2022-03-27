using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Serilog.Settings.Configuration.Assemblies
{
    class AppDomainAssemblyFinder : AssemblyFinder
    {
        public override IReadOnlyList<AssemblyName> FindAssembliesContainingName(string nameToFind)
        {
            var query = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                let assemblyName = assembly.GetName()
                where IsCaseInsensitiveMatch(assemblyName.Name, nameToFind)
                select assemblyName;

            return query.ToList();
        }
    }
}
