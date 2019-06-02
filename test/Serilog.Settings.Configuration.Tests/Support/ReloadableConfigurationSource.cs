using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Serilog.Settings.Configuration.Tests.Support
{
    class ReloadableConfigurationSource : IConfigurationSource
    {
        readonly ReloadableConfigurationProvider _configProvider;
        readonly IDictionary<string, string> _source;

        public ReloadableConfigurationSource(IDictionary<string, string> source)
        {
            _source = source;
            _configProvider = new ReloadableConfigurationProvider(source);
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => _configProvider;

        public void Reload() => _configProvider.Reload();

        public void Set(string key, string value) => _source[key] = value;

        class ReloadableConfigurationProvider : ConfigurationProvider
        {
            readonly IDictionary<string, string> _source;

            public ReloadableConfigurationProvider(IDictionary<string, string> source)
            {
                _source = source;
            }

            public override void Load() => Data = _source;

            public void Reload() => OnReload();
        }
    }
}
