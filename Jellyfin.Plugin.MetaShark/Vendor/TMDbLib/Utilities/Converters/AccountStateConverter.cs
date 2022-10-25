using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMDbLib.Objects.General;
using TMDbLib.Objects.TvShows;

namespace TMDbLib.Utilities.Converters
{
    internal class AccountStateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AccountState) ||
                    objectType == typeof(TvAccountState) ||
                    objectType == typeof(TvEpisodeAccountState) ||
                    objectType == typeof(TvEpisodeAccountStateWithNumber);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            // Sometimes the AccountState.Rated is an object with a value in it
            // In these instances, convert it from:
            //  "rated": { "value": 5 }
            //  "rated": False
            // To:
            //  "rating": 5
            //  "rating": null

            JToken obj = jObject["rated"];
            if (obj.Type == JTokenType.Boolean)
            {
                // It's "False", so the rating is not set
                jObject.Remove("rated");
                jObject.Add("rating", null);
            }
            else if (obj.Type == JTokenType.Object)
            {
                // Read out the value
                double rating = obj["value"].ToObject<double>();
                jObject.Remove("rated");
                jObject.Add("rating", rating);
            }

            object result = Activator.CreateInstance(objectType);

            // Populate the result
            using JsonReader jsonReader = jObject.CreateReader();
            serializer.Populate(jsonReader, result);

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject jToken = JObject.FromObject(value);

            JValue obj = (JValue)jToken["rating"];
            jToken.Remove("rating");

            if (obj.Value == null)
            {
                jToken["rated"] = null;
            }
            else
            {
                jToken["rated"] = JToken.FromObject(new { value = obj });
            }

            jToken.WriteTo(writer);
        }
    }
}