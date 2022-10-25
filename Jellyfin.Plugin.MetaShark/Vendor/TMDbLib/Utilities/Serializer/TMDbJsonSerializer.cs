using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using TMDbLib.Utilities.Converters;

namespace TMDbLib.Utilities.Serializer
{
    public class TMDbJsonSerializer : ITMDbSerializer
    {
        private readonly JsonSerializer _serializer;
        private readonly Encoding _encoding = new UTF8Encoding(false);

        public static TMDbJsonSerializer Instance { get; } = new();

        private TMDbJsonSerializer()
        {
            _serializer = JsonSerializer.CreateDefault();
            _serializer.Converters.Add(new ChangeItemConverter());
            _serializer.Converters.Add(new AccountStateConverter());
            _serializer.Converters.Add(new KnownForConverter());
            _serializer.Converters.Add(new SearchBaseConverter());
            _serializer.Converters.Add(new TaggedImageConverter());
            _serializer.Converters.Add(new TolerantEnumConverter());
        }

        public void Serialize(Stream target, object obj, Type type)
        {
            using StreamWriter sw = new StreamWriter(target, _encoding, 4096, true);
            using JsonTextWriter jw = new JsonTextWriter(sw);

            _serializer.Serialize(jw, obj, type);
        }

        public object Deserialize(Stream source, Type type)
        {
            using StreamReader sr = new StreamReader(source, _encoding, false, 4096, true);
            using JsonTextReader jr = new JsonTextReader(sr);

            return _serializer.Deserialize(jr, type);
        }
    }
}