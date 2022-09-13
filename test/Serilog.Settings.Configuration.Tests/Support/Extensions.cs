using Serilog.Events;

namespace Serilog.Settings.Configuration.Tests.Support
{
    public static class Extensions
    {
        public static object LiteralValue(this LogEventPropertyValue @this)
        {
            return ((ScalarValue)@this).Value;
        }

        public static string ToValidJson(this string str)
        {
            str = str.Replace('\'', '"');
            return str;
        }

        public static string Format(this string template, params object[] paramObjects) => string.Format(template, paramObjects);
    }
}
