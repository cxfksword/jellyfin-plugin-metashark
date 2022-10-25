using Newtonsoft.Json;

namespace TMDbLib.Objects.Changes
{
    public class ChangeItemUpdated : ChangeItemBase
    {
        public ChangeItemUpdated()
        {
            Action = ChangeAction.Updated;
        }

        [JsonProperty("original_value")]
        public object OriginalValue { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }
    }
}