using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.MetaShark.Model
{
    public class ParseNameResult : ItemLookupInfo
    {
        public string? ChineseName { get; set; } = null;

        /// <summary>
        /// 可能会解析不对，最好只在动画SP中才使用
        /// </summary>
        public string? EpisodeName { get; set; } = null;

        private string _animeType = string.Empty;
        public string AnimeType
        {
            get
            {
                return _animeType.ToUpper();
            }
            set
            {
                _animeType = value;
            }
        }

        public bool IsSpecial
        {
            get
            {
                return !string.IsNullOrEmpty(AnimeType) && AnimeType.ToUpper() == "SP";
            }
        }

        public bool IsExtra
        {
            get
            {
                return !string.IsNullOrEmpty(AnimeType) && AnimeType.ToUpper() != "SP";
            }
        }

        public string? PaddingZeroIndexNumber
        {
            get
            {
                if (!IndexNumber.HasValue)
                {
                    return null;
                }

                return $"{IndexNumber:00}";
            }
        }

        public string ExtraName
        {
            get
            {
                if (IndexNumber.HasValue)
                {
                    return $"{Name} {AnimeType} {PaddingZeroIndexNumber}";
                }
                else
                {
                    return $"{Name} {AnimeType}";
                }
            }
        }
    }
}
