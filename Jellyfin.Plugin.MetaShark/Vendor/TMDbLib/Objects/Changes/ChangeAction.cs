using Newtonsoft.Json;
using TMDbLib.Utilities;
using TMDbLib.Utilities.Converters;

namespace TMDbLib.Objects.Changes
{
    [JsonConverter(typeof(EnumStringValueConverter))]
    public enum ChangeAction
    {
        Unknown,

        [EnumValue("added")]
        Added = 1,

        [EnumValue("created")]
        Created = 2,

        [EnumValue("updated")]
        Updated = 3,

        [EnumValue("deleted")]
        Deleted = 4,

        [EnumValue("destroyed")]
        Destroyed = 5
    }
}