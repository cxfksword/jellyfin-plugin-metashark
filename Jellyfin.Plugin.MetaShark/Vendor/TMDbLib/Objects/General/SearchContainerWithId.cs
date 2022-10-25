namespace TMDbLib.Objects.General
{
    public class SearchContainerWithId<T> : SearchContainer<T>
    {
        public int Id { get; set; }
    }
}