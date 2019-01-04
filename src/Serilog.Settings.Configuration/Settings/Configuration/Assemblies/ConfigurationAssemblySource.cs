// Copyright 2013-2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Serilog.Settings.Configuration
{
    /// <summary>
    /// Defines how the package will identify the assemblies which are scanned for sinks and other Type information.
    /// </summary>
    public enum ConfigurationAssemblySource
    {
        /// <summary>
        /// Try to scan the assemblies already in memory. This is the default. If GetEntryAssembly is null, fallback to DLL scanning.
        /// </summary>
        UseLoadedAssemblies,

        /// <summary>
        /// Scan for assemblies in DLLs from the working directory. This is the fallback when GetEntryAssembly is null.
        /// </summary>
        AlwaysScanDllFiles
    }
}
