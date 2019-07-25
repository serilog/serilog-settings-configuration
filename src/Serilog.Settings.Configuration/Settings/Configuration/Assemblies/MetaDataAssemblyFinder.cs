using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Serilog.Settings.Configuration.Assemblies
{
    sealed class MetaDataAssemblyFinder : AssemblyFinder
    {
        readonly DependencyContext _dependencyContext;
        readonly Assembly _assembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaDataAssemblyFinder"/> class.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <exception cref="ArgumentNullException">assembly</exception>
        public MetaDataAssemblyFinder(Assembly assembly)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _dependencyContext = GetDependencyContext();
        }

        /// <summary>
        /// Finds the name of the assemblies containing.
        /// </summary>
        /// <param name="nameToFind">The name to find.</param>
        /// <returns>IReadOnlyList&lt;AssemblyName&gt;.</returns>
        public override IReadOnlyList<AssemblyName> FindAssembliesContainingName(string nameToFind)
        {
            var query = from library in _dependencyContext.RuntimeLibraries
                from assemblyName in library.GetDefaultAssemblyNames(_dependencyContext)
                where IsCaseInsensitiveMatch(assemblyName.Name, nameToFind)
                select assemblyName;

            return query.ToList().AsReadOnly();
        }

        /// <summary>
        /// Initializes the <see cref="DependencyContext"/> by resolve the <see cref="Assembly"/>'s metadata information.
        /// </summary>
        /// <returns>The initialized <see cref="DependencyContext"/>.</returns>
        private DependencyContext GetDependencyContext()
        {
            var rootBinDirectory = Path.GetDirectoryName(_assembly.Location);

            // Use the found assembly name to form the name of the dependency .json we are looking for
            var depFileSearchPattern = Path.ChangeExtension(_assembly.ManifestModule.Name, ".deps.json");

            // Although we want only one, we have no way to log an error at the moment and do not want to throw an exception here
            var dependencyJsonFile = Directory.GetParent(rootBinDirectory)
                .EnumerateFileSystemInfos(depFileSearchPattern, SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (dependencyJsonFile != null)
            {
                var jr = new DependencyContextJsonReader();
                return jr.Read(File.OpenRead(dependencyJsonFile.FullName));
            }

            return null;
        }
    }
}
