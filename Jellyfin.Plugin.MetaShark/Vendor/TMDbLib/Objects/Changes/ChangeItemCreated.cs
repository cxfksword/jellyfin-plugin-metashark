namespace TMDbLib.Objects.Changes
{
    public class ChangeItemCreated : ChangeItemBase
    {
        public ChangeItemCreated()
        {
            Action = ChangeAction.Created;
        }
    }
}