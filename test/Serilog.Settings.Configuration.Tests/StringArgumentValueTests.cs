using Serilog.Formatting;
using Serilog.Formatting.Json;
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
    }
}
