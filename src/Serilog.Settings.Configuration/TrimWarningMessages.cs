
using System.Data;

namespace Serilog
{
    internal static class TrimWarningMessages
    {
        public const string NotSupportedWhenTrimming = "Automatic configuration is not supported when trimming.";
        public const string NotSupportedInAot = "Automatic configuration is not supported when AOT compiling.";
        public const string UnboundedReflection = "Uses unbounded reflection to load types";
        public const string CreatesArraysOfArbitraryTypes = "Creates arrays of arbitrary types";
        public const string IncompatibleWithSingleFile = "Incompatible with single-file publishing";
    }
}