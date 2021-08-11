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
            try
            {
                // Need to check `Assembly.GetEntryAssembly()` first because 
                // `DependencyContext.Default` throws an exception when `Assembly.GetEntryAssembly()` returns null
                if (Assembly.GetEntryAssembly() != null && DependencyContext.Default != null)
                {
                    return new DependencyContextAssemblyFinder(DependencyContext.Default);
                }
            }
            catch (NotSupportedException) when (typeof(object).Assembly.Location is "") // bundled mode detection
            {
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
