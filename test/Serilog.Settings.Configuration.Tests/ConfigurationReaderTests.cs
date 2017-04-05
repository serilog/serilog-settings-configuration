using System.Collections.Generic;
using Serilog.Formatting;
using Xunit;
using System.Reflection;
using System.Linq;

namespace Serilog.Settings.Configuration.Tests
{
    public class ConfigurationReaderTests
    {
        [Fact]
        public void CallableMethodsAreSelected()
        {
            var options = typeof(DummyLoggerConfigurationExtensions).GetTypeInfo().DeclaredMethods.ToList();
            Assert.Equal(2, options.Count(mi => mi.Name == "DummyRollingFile"));
            var suppliedArguments = new Dictionary<string, IConfigurationArgumentValue>
            {
                {"pathFormat", new StringArgumentValue(() => "C:\\") }
            };

            var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArguments);
            Assert.Equal(typeof(string), selected.GetParameters()[1].ParameterType);
        }

        [Fact]
        public void MethodsAreSelectedBasedOnCountOfMatchedArguments()
        {
            var options = typeof(DummyLoggerConfigurationExtensions).GetTypeInfo().DeclaredMethods.ToList();
            Assert.Equal(2, options.Count(mi => mi.Name == "DummyRollingFile"));
            var suppliedArguments = new Dictionary<string, IConfigurationArgumentValue>()
            {
                { "pathFormat", new StringArgumentValue(() => "C:\\") },
                { "formatter", new StringArgumentValue(() => "SomeFormatter, SomeAssembly") }
            };

            var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArguments);
            Assert.Equal(typeof(ITextFormatter), selected.GetParameters()[1].ParameterType);
        }
    }
}
