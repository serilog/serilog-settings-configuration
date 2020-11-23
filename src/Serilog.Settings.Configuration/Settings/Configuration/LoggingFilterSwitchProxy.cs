using System;

namespace Serilog.Settings.Configuration
{
    class LoggingFilterSwitchProxy
    {
        readonly Action<string> _setProxy;
        readonly Func<string> _getProxy;

        LoggingFilterSwitchProxy(object realSwitch)
        {
            RealSwitch = realSwitch ?? throw new ArgumentNullException(nameof(realSwitch));

            var expressionProperty = realSwitch.GetType().GetProperty("Expression");

            _setProxy = (Action<string>)Delegate.CreateDelegate(
                typeof(Action<string>),
                realSwitch,
                expressionProperty.GetSetMethod());

            _getProxy = (Func<string>)Delegate.CreateDelegate(
                typeof(Func<string>),
                realSwitch,
                expressionProperty.GetGetMethod());
        }

        public object RealSwitch { get; }

        public string Expression
        {
            get => _getProxy();
            set => _setProxy(value);
        }

        public static LoggingFilterSwitchProxy Create(string expression = null)
        {
            var filterSwitchType =
                Type.GetType("Serilog.Expressions.LoggingFilterSwitch, Serilog.Expressions") ??
                Type.GetType("Serilog.Filters.Expressions.LoggingFilterSwitch, Serilog.Filters.Expressions");

            if (filterSwitchType is null)
            {
                return null;
            }

            return new LoggingFilterSwitchProxy(Activator.CreateInstance(filterSwitchType, expression));
        }
    }
}
