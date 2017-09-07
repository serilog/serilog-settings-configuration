using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.Primitives;

using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Settings.Configuration
{
    class StringArgumentValue : IConfigurationArgumentValue
    {
        readonly Func<string> _valueProducer;
        readonly Func<IChangeToken> _changeTokenProducer;

        public StringArgumentValue(Func<string> valueProducer, Func<IChangeToken> changeTokenProducer = null)
        {
            _valueProducer = valueProducer ?? throw new ArgumentNullException(nameof(valueProducer));
            _changeTokenProducer = changeTokenProducer;
        }

        static readonly Dictionary<Type, Func<string, object>> ExtendedTypeConversions = new Dictionary<Type, Func<string, object>>
            {
                { typeof(Uri), s => new Uri(s) },
                { typeof(TimeSpan), s => TimeSpan.Parse(s) }
            };

        public object ConvertTo(Type toType)
        {
            var argumentValue = Environment.ExpandEnvironmentVariables(_valueProducer());

            var toTypeInfo = toType.GetTypeInfo();
            if (toTypeInfo.IsGenericType && toType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrEmpty(argumentValue))
                    return null;

                // unwrap Nullable<> type since we're not handling null situations
                toType = toTypeInfo.GenericTypeArguments[0];
                toTypeInfo = toType.GetTypeInfo();
            }

            if (toTypeInfo.IsEnum)
                return Enum.Parse(toType, argumentValue);

            var convertor = ExtendedTypeConversions
                .Where(t => t.Key.GetTypeInfo().IsAssignableFrom(toTypeInfo))
                .Select(t => t.Value)
                .FirstOrDefault();

            if (convertor != null)
                return convertor(argumentValue);

            if (toTypeInfo.IsInterface && !string.IsNullOrWhiteSpace(argumentValue))
            {
                var type = Type.GetType(argumentValue.Trim());
                if (type != null)
                {
                    var ctor = type.GetTypeInfo().DeclaredConstructors.FirstOrDefault(ci =>
                    {
                        var parameters = ci.GetParameters();
                        return parameters.Length == 0 || parameters.All(pi => pi.HasDefaultValue);
                    });

                    if (ctor == null)
                        throw new InvalidOperationException($"A default constructor was not found on {type.FullName}.");

                    var call = ctor.GetParameters().Select(pi => pi.DefaultValue).ToArray();
                    return ctor.Invoke(call);
                }
            }

            if (toType == typeof(LoggingLevelSwitch))
            {
                if (!Enum.TryParse(argumentValue, out LogEventLevel minimumLevel))
                    throw new InvalidOperationException($"The value `{argumentValue}` is not a valid Serilog level.");

                var levelSwitch = new LoggingLevelSwitch(minimumLevel);

                if (_changeTokenProducer != null)
                {
                    ChangeToken.OnChange(
                        _changeTokenProducer,
                        () =>
                        {
                            var newArgumentValue = _valueProducer();

                            if (Enum.TryParse(newArgumentValue, out minimumLevel))
                                levelSwitch.MinimumLevel = minimumLevel;
                            else
                                SelfLog.WriteLine($"The value `{newArgumentValue}` is not a valid Serilog level.");
                        });
                }

                return levelSwitch;
            }

            return Convert.ChangeType(argumentValue, toType);
        }
    }
}
