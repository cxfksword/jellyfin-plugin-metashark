using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Collections
{
    [Flags]
    public enum CollectionMethods
    {
        [EnumValue("Undefined")]
        Undefined = 0,
        [EnumValue("images")]
        Images = 1
    }
}