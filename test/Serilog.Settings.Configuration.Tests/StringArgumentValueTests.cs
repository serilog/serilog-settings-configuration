using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Settings.Configuration.Tests.Support;
using Xunit;

namespace Serilog.Settings.Configuration.Tests
{
    public class StringArgumentValueTests
    {
        [Fact]
        public void StringValuesConvertToDefaultInstancesIfTargetIsInterface()
        {
            var stringArgumentValue = new StringArgumentValue(() => "Serilog.Formatting.Json.JsonFormatter, Serilog");

            var result = stringArgumentValue.ConvertTo(typeof(ITextFormatter));

            Assert.IsType<JsonFormatter>(result);
        }

        [Fact]
        public void StringValuesConvertToDefaultInstancesIfTargetIsAbstractClass()
        {
          var stringArgumentValue = new StringArgumentValue(() => "Serilog.Settings.Configuration.Tests.Support.ConcreteClass, Serilog.Settings.Configuration.Tests");

          var result = stringArgumentValue.ConvertTo(typeof(AbstractClass));

          Assert.IsType<ConcreteClass>(result);
        }
  }
}
