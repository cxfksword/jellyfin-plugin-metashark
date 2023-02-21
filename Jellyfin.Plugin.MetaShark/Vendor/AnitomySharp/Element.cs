/*
 * Copyright (c) 2014-2017, Eren Okka
 * Copyright (c) 2016-2017, Paul Miller
 * Copyright (c) 2017-2018, Tyler Bratton
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace AnitomySharp
{
    /// <summary>
    /// An <see cref="Element"/> represents an identified Anime <see cref="Token"/>. 
    /// A single filename may contain multiple of the same
    /// token(e.g <see cref="ElementCategory.ElementEpisodeNumber"/>).
    /// 
    /// 一个元素即是一个已标识的标记(token)
    /// 
    /// 单个文件名可能包含多个相同的标记，比如：`ElementEpisodeNumber`元素类别的标记
    /// </summary>
    public class Element
    {
        /// <summary>
        /// Element Categories
        /// 
        /// 元素类别
        /// </summary>
        public enum ElementCategory
        {
            /// <summary>
            /// 元素类别：动画季度，不带<see cref="ElementAnimeSeasonPrefix"/>前缀
            /// </summary>
            ElementAnimeSeason,
            /// <summary>
            /// 元素类别：季度前缀，用于标识<see cref="ElementAnimeSeason">季度</see>的元素类别
            /// </summary>
            ElementAnimeSeasonPrefix,
            /// <summary>
            /// 元素类别：动画名
            /// </summary>
            ElementAnimeTitle,
            /// <summary>
            /// 元素类别：动画类型
            /// </summary>
            ElementAnimeType,
            /// <summary>
            /// 元素类别：动画年份，唯一
            /// </summary>
            ElementAnimeYear,
            /// <summary>
            /// 元素类别：音频术语
            /// </summary>
            ElementAudioTerm,
            /// <summary>
            /// 元素类别：设备，用于标识设备类型
            /// </summary>
            ElementDeviceCompatibility,
            /// <summary>
            /// 元素类别：剧集数
            /// </summary>
            ElementEpisodeNumber,
            /// <summary>
            /// 元素类别：等效剧集数，常见于多季度番剧
            /// </summary>
            ElementEpisodeNumberAlt,
            /// <summary>
            /// 元素类别：剧集前缀，比如：“E”
            /// </summary>
            ElementEpisodePrefix,
            /// <summary>
            /// 元素类别：剧集名
            /// </summary>
            ElementEpisodeTitle,
            /// <summary>
            /// 元素类别：文件校验码，唯一
            /// </summary>
            ElementFileChecksum,
            /// <summary>
            /// 元素类别：文件扩展名，唯一
            /// </summary>
            ElementFileExtension,
            /// <summary>
            /// 文件名，唯一
            /// </summary>
            ElementFileName,
            /// <summary>
            /// 元素类别：语言
            /// </summary>
            ElementLanguage,
            /// <summary>
            /// 元素类别：其他，暂时无法分类的元素
            /// </summary>
            ElementOther,
            /// <summary>
            /// 元素类别：发布组，唯一
            /// </summary>
            ElementReleaseGroup,
            /// <summary>
            /// 元素类别：发布信息
            /// </summary>
            ElementReleaseInformation,
            /// <summary>
            /// 元素类别：发布版本
            /// </summary>
            ElementReleaseVersion,
            /// <summary>
            /// 元素类别：来源
            /// </summary>
            ElementSource,
            /// <summary>
            /// 元素类别：字幕
            /// </summary>
            ElementSubtitles,
            /// <summary>
            /// 元素类别：视频分辨率
            /// </summary>
            ElementVideoResolution,
            /// <summary>
            /// 元素类别：视频术语
            /// </summary>
            ElementVideoTerm,
            /// <summary>
            /// 元素类别：卷数
            /// </summary>
            ElementVolumeNumber,
            /// <summary>
            /// 元素类别：卷前缀
            /// </summary>
            ElementVolumePrefix,
            /// <summary>
            /// 元素类别：未知元素类型
            /// </summary>
            ElementUnknown
        }

        /// <summary>
        /// 
        /// </summary>
        public ElementCategory Category { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Value { get; }

        /// <summary>
        ///  Constructs a new Element
        ///  
        /// 构造一个元素
        /// </summary>
        /// <param name="category">the category of the element</param>
        /// <param name="value">the element's value</param>
        public Element(ElementCategory category, string value)
        {
            Category = category;
            Value = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return -1926371015 + Value.GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (Element)obj;
            return Category.Equals(other.Category);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Element{{category={Category}, value='{Value}'}}";
        }
    }
}