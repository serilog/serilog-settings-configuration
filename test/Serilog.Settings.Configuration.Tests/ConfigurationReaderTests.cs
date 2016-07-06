using System.Collections.Generic;
using Serilog.Settings.Configuration;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Xunit;
using System.Reflection;
using System.Linq;

namespace Serilog.Settings.Configuration.Tests
{
    public class ConfigurationReaderTests
    {
        [Fact]
        public void StringValuesConvertToDefaultInstancesIfTargetIsInterface()
        {
            var result = ConfigurationReader.ConvertToType("Serilog.Formatting.Json.JsonFormatter, Serilog", typeof(ITextFormatter));
            Assert.IsType<JsonFormatter>(result);
        }

        [Fact]
        public void CallableMethodsAreSelected()
        {
            var options = typeof(DummyLoggerConfigurationExtensions).GetTypeInfo().DeclaredMethods.ToList();
            Assert.Equal(2, options.Count(mi => mi.Name == "DummyRollingFile"));
            var suppliedArguments = new Dictionary<string, string>
            {
                {"pathFormat", "C:\\" }
            };

            var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArguments);
            Assert.Equal(typeof(string), selected.GetParameters()[1].ParameterType);
        }

        [Fact]
        public void MethodsAreSelectedBasedOnCountOfMatchedArguments()
        {
            var options = typeof(DummyLoggerConfigurationExtensions).GetTypeInfo().DeclaredMethods.ToList();
            Assert.Equal(2, options.Count(mi => mi.Name == "DummyRollingFile"));
            var suppliedArguments = new Dictionary<string, string>()
            {
                { "pathFormat", "C:\\" },
                { "formatter", "SomeFormatter, SomeAssembly" }
            };

            var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArguments);
            Assert.Equal(typeof(ITextFormatter), selected.GetParameters()[1].ParameterType);
        }
    }
}
