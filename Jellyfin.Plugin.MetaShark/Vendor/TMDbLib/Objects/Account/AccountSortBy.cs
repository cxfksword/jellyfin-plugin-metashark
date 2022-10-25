using TMDbLib.Utilities;

namespace TMDbLib.Objects.Account
{
    public enum AccountSortBy
    {
        Undefined = 0,
        [EnumValue("created_at")]
        CreatedAt = 1,
    }
}
