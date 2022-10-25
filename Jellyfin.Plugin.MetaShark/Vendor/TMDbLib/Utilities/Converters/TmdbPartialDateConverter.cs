using Newtonsoft.Json;
using System;
using System.Globalization;

namespace TMDbLib.Utilities.Converters
{
    public class TmdbPartialDateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string str = reader.Value as string;
            if (string.IsNullOrEmpty(str))
                return null;

            DateTime result;
            if (!DateTime.TryParse(str, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out result))
                return null;

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DateTime? date = value as DateTime?;
            writer.WriteValue(date?.ToString(CultureInfo.InvariantCulture));
        }
    }
}