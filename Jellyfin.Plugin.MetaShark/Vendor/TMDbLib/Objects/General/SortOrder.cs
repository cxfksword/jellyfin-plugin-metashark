using TMDbLib.Utilities;

namespace TMDbLib.Objects.General
{
    public enum SortOrder
    {
        Undefined = 0,
        [EnumValue("asc")]
        Ascending = 1,
        [EnumValue("desc")]
        Descending = 2
    }
}
