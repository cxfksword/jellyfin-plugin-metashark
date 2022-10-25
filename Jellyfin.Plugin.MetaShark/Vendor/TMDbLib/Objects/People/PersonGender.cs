using TMDbLib.Utilities;

namespace TMDbLib.Objects.People
{
    public enum PersonGender
    {
        [EnumValue(null)]
        Unknown = 0,
        Female = 1,
        Male = 2,
        NonBinary = 3
    }
}
