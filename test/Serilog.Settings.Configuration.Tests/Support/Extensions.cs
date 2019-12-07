using Serilog.Events;

namespace Serilog.Settings.Configuration.Tests.Support
{
    public static class Extensions
    {
        public static object LiteralValue(this LogEventPropertyValue @this)
        {
            return ((ScalarValue)@this).Value;
        }

        // netcore3.0 error:
        // Could not parse the JSON file. System.Text.Json.JsonReaderException : ''' is an invalid start of a property name. Expected a '"'
        public static string ToValidJson(this string str)
        {
#if NETCOREAPP3_0
            str = str.Replace('\'', '"');
#endif
            return str;
        }
    }
}
