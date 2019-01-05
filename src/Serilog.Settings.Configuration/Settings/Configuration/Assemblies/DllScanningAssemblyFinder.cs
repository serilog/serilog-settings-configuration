using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Serilog.Settings.Configuration.Assemblies
{
    sealed class DllScanningAssemblyFinder : AssemblyFinder
    {
        public override IReadOnlyList<AssemblyName> FindAssembliesContainingName(string nameToFind)
        {
            var query = from outputAssemblyPath in System.IO.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
                let assemblyFileName = System.IO.Path.GetFileNameWithoutExtension(outputAssemblyPath)
                where IsCaseInsensitiveMatch(assemblyFileName, nameToFind)
                select AssemblyName.GetAssemblyName(outputAssemblyPath);

            return query.ToList().AsReadOnly();
        }
    }
}
