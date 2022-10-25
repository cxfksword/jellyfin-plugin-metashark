using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.People
{
    [Flags]
    public enum PersonMethods
    {
        [EnumValue("Undefined")]
        Undefined = 0,
        [EnumValue("movie_credits")]
        MovieCredits = 1,
        [EnumValue("tv_credits")]
        TvCredits = 2,
        [EnumValue("external_ids")]
        ExternalIds = 4,
        [EnumValue("images")]
        Images = 8,
        [EnumValue("tagged_images")]
        TaggedImages = 16,
        [EnumValue("changes")]
        Changes = 32
    }
}