using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;

namespace Serilog.Settings.Configuration.Tests.Support
{
    class JsonStringConfigSource : IConfigurationSource
    {
        readonly string _json;

        public JsonStringConfigSource(string json)
        {
            _json = json;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new JsonStringConfigProvider(_json);
        }

        public static IConfigurationSection LoadSection(string json, string section)
        {
            return new ConfigurationBuilder().Add(new JsonStringConfigSource(json)).Build().GetSection(section);
        }        

        class JsonStringConfigProvider : JsonConfigurationProvider
        {
            readonly string _json;

            public JsonStringConfigProvider(string json) : base(new JsonConfigurationSource { Optional = true })
            {
                _json = json;
            }

            public override void Load()
            {
                Load(StringToStream(_json));
            }

            static Stream StringToStream(string str)
            {
                var memStream = new MemoryStream();
                var textWriter = new StreamWriter(memStream);
                textWriter.Write(str);
                textWriter.Flush();
                memStream.Seek(0, SeekOrigin.Begin);

                return memStream;
            }
        }
    }
}
