using Newtonsoft.Json;

namespace TMDbLib.Objects.Changes
{
    public class ChangeItemAdded : ChangeItemBase
    {
        public ChangeItemAdded()
        {
            Action = ChangeAction.Added;
        }

        [JsonProperty("value")]
        public object Value { get; set; }
    }
}