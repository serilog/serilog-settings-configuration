using Xunit;
using System.Reflection;
using System.Linq;
using Serilog.Formatting;
using Serilog.Settings.Configuration.Assemblies;
using Serilog.Settings.Configuration.Tests.Support;

namespace Serilog.Settings.Configuration.Tests
{
    public class ConfigurationReaderTests
    {
        readonly ConfigurationReader _configurationReader;

        public ConfigurationReaderTests()
        {
            _configurationReader = new ConfigurationReader(
                JsonStringConfigSource.LoadSection(@"{ 'Serilog': {  } }", "Serilog"),
                AssemblyFinder.ForSource(ConfigurationAssemblySource.UseLoadedAssemblies));
        }

        [Fact]
        public void WriteToSupportSimplifiedSyntax()
        {
            var json = @"
{
    'WriteTo': [ 'LiterateConsole', 'DiagnosticTrace' ]
}";

            var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "WriteTo"));
            Assert.Equal(2, result.Count);
            Assert.True(result.Contains("LiterateConsole"));
            Assert.True(result.Contains("DiagnosticTrace"));

            Assert.Single(result["LiterateConsole"]);
            Assert.Single(result["DiagnosticTrace"]);
        }

        [Fact]
        public void WriteToSupportExpandedSyntaxWithoutArgs()
        {
            var json = @"
{
    'WriteTo': [ {
        'Name': 'LiterateConsole'
    }]
}";

            var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "WriteTo"));
            Assert.Equal(1, result.Count);
            Assert.True(result.Contains("LiterateConsole"));

            Assert.Single(result["LiterateConsole"]);
        }

        [Fact]
        public void WriteToSupportExpandedSyntaxWithArgs()
        {
            var json = @"
{
    'WriteTo': [ {
        'Name': 'LiterateConsole',
        'Args': {
            'outputTemplate': '{Message}'
        },
    }]
}";

            var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "WriteTo"));

            Assert.Equal(1, result.Count);
            Assert.True(result.Contains("LiterateConsole"));

            Assert.Single(result["LiterateConsole"]);

            var args = result["LiterateConsole"].Single().ToArray();

            Assert.Single(args);
            Assert.Equal("outputTemplate", args[0].Key);
            Assert.Equal("{Message}", args[0].Value.ConvertTo(typeof(string), new ResolutionContext()));
        }

        [Fact]
        public void WriteToSupportMultipleSinksOfTheSameKind()
        {
            var json = @"
{
    'WriteTo': [
      {
        'Name': 'LiterateConsole',
        'Args': {
            'outputTemplate': '{Message}'
          },
      },
      'DiagnosticTrace'
    ],
    'WriteTo:File1': {
        'Name': 'File',
        'Args': {
            'outputTemplate': '{Message}'
        },
    },
    'WriteTo:File2': {
        'Name': 'File',
        'Args': {
            'outputTemplate': '{Message}'
        },
    }
}";

            var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "WriteTo"));

            Assert.Equal(3, result.Count);
            Assert.True(result.Contains("LiterateConsole"));
            Assert.True(result.Contains("DiagnosticTrace"));
            Assert.True(result.Contains("File"));

            Assert.Single(result["LiterateConsole"]);
            Assert.Single(result["DiagnosticTrace"]);
            Assert.Equal(2, result["File"].Count());
        }

        [Fact]
        public void Enrich_SupportSimplifiedSyntax()
        {
            var json = @"
{
    'Enrich': [ 'FromLogContext', 'WithMachineName', 'WithThreadId' ]
}";

            var result = _configurationReader.GetMethodCalls(JsonStringConfigSource.LoadSection(json, "Enrich"));
            Assert.Equal(3, result.Count);
            Assert.True(result.Contains("FromLogContext"));
            Assert.True(result.Contains("WithMachineName"));
            Assert.True(result.Contains("WithThreadId"));

            Assert.Single(result["FromLogContext"]);
            Assert.Single(result["WithMachineName"]);
            Assert.Single(result["WithThreadId"]);
        }

        [Fact]
        public void CallableMethodsAreSelected()
        {
            var options = typeof(DummyLoggerConfigurationExtensions).GetTypeInfo().DeclaredMethods.ToList();
            Assert.Equal(2, options.Count(mi => mi.Name == "DummyRollingFile"));
            var suppliedArgumentNames = new[] { "pathFormat" };

            var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArgumentNames);
            Assert.Equal(typeof(string), selected.GetParameters()[1].ParameterType);
        }

        [Fact]
        public void MethodsAreSelectedBasedOnCountOfMatchedArguments()
        {
            var options = typeof(DummyLoggerConfigurationExtensions).GetTypeInfo().DeclaredMethods.ToList();
            Assert.Equal(2, options.Count(mi => mi.Name == "DummyRollingFile"));

            var suppliedArgumentNames = new[] { "pathFormat", "formatter" };

            var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArgumentNames);
            Assert.Equal(typeof(ITextFormatter), selected.GetParameters()[1].ParameterType);
        }

        [Fact]
        public void MethodsAreSelectedBasedOnCountOfMatchedArgumentsAndThenStringType()
        {
            var options = typeof(DummyLoggerConfigurationWithMultipleMethodsExtensions).GetTypeInfo().DeclaredMethods.ToList();
            Assert.Equal(3, options.Count(mi => mi.Name == "DummyRollingFile"));

            var suppliedArgumentNames = new[] { "pathFormat", "formatter" };

            var selected = ConfigurationReader.SelectConfigurationMethod(options, "DummyRollingFile", suppliedArgumentNames);
            Assert.Equal(typeof(string), selected.GetParameters()[2].ParameterType);
        }
    }
}
