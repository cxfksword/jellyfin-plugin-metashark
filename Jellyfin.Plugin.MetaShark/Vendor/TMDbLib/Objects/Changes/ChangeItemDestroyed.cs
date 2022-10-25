using Newtonsoft.Json;

namespace TMDbLib.Objects.Changes
{
    public class ChangeItemDestroyed : ChangeItemBase
    {
        public ChangeItemDestroyed()
        {
            Action = ChangeAction.Destroyed;
        }

        [JsonProperty("value")]
        public object Value { get; set; }
    }
}