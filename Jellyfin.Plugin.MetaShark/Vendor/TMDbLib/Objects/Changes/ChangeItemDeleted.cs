using Newtonsoft.Json;

namespace TMDbLib.Objects.Changes
{
    public class ChangeItemDeleted : ChangeItemBase
    {
        public ChangeItemDeleted()
        {
            Action = ChangeAction.Deleted;
        }

        [JsonProperty("original_value")]
        public object OriginalValue { get; set; }
    }
}