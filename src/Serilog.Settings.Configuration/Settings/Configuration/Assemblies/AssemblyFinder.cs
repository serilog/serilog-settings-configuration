using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Serilog.Settings.Configuration.Assemblies
{
    abstract class AssemblyFinder
    {
        public abstract IReadOnlyList<AssemblyName> FindAssembliesContainingName(string nameToFind);

        protected static bool IsCaseInsensitiveMatch(string text, string textToFind)
        {
            return text != null && text.ToLowerInvariant().Contains(textToFind.ToLowerInvariant());
        }

        public static AssemblyFinder Auto()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var isBundled = entryAssembly.Location.Length == 0;
                if (isBundled)
                {
                    using var depsJsonStream = SingleFileApplication.GetDepsJsonStream();
                    if (depsJsonStream != null)
                    {
                        using var depsJsonReader = new DependencyContextJsonReader();
                        var dependencyContext = depsJsonReader.Read(depsJsonStream);
                        return new DependencyContextAssemblyFinder(dependencyContext);
                    }
                }

                var entryAssemblyContext = DependencyContext.Load(entryAssembly);
                if (entryAssemblyContext != null)
                {
                    return new DependencyContextAssemblyFinder(entryAssemblyContext);
                }
            }

            return new DllScanningAssemblyFinder();
        }

        public static AssemblyFinder ForSource(ConfigurationAssemblySource configurationAssemblySource)
        {
            return configurationAssemblySource switch
            {
                ConfigurationAssemblySource.UseLoadedAssemblies => Auto(),
                ConfigurationAssemblySource.AlwaysScanDllFiles => new DllScanningAssemblyFinder(),
                _ => throw new ArgumentOutOfRangeException(nameof(configurationAssemblySource), configurationAssemblySource, null),
            };
        }

        public static AssemblyFinder ForDependencyContext(DependencyContext dependencyContext)
        {
            return new DependencyContextAssemblyFinder(dependencyContext);
        }
    }
}
