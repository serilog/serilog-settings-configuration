using System.Collections.Generic;
using Serilog.Formatting;
using Xunit;
using System.Reflection;
using System.Linq;
using Serilog.Core;
using Serilog.Settings.Configuration.Tests.Support;

namespace Serilog.Settings.Configuration.Tests
{
    public class ConfigurationReaderTests
    {
        readonly ConfigurationReader _configurationReader;

        public ConfigurationReaderTests()
        {
            _configurationReader = new ConfigurationReader(JsonStringConfigSource.LoadSection(@"{ 'Serilog': {  } }", "Serilog"), null);
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

            Assert.Equal(1, result["LiterateConsole"].Count());
            Assert.Equal(1, result["DiagnosticTrace"].Count());
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

            Assert.Equal(1, result["LiterateConsole"].Count());
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

            Assert.Equal(1, result["LiterateConsole"].Count());

            var args = result["LiterateConsole"].Single().Cast<KeyValuePair<string, IConfigurationArgumentValue>>().ToArray();

            Assert.Equal(1, args.Length);
            Assert.Equal("outputTemplate", args[0].Key);
            Assert.Equal("{Message}", args[0].Value.ConvertTo(typeof(string), new Dictionary<string, LoggingLevelSwitch>()));
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

            Assert.Equal(1, result["LiterateConsole"].Count());
            Assert.Equal(1, result["DiagnosticTrace"].Count());
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

            Assert.Equal(1, result["FromLogContext"].Count());
            Assert.Equal(1, result["WithMachineName"].Count());
            Assert.Equal(1, result["WithThreadId"].Count());
        }

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
