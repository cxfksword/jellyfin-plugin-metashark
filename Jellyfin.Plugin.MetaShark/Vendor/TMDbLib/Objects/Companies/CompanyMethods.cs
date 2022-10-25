using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Companies
{
    [Flags]
    public enum CompanyMethods
    {
        [EnumValue("Undefined")]
        Undefined = 0,
        [EnumValue("movies")]
        Movies = 1
    }
}