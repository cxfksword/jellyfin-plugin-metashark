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
    /// AnitomySharp search configuration options
    /// 
    /// 提取元素时的默认配置项
    /// </summary>
    public class Options
    {
        /// <summary>
        /// 提取元素时使用的分隔符
        /// </summary>
        public string AllowedDelimiters { get; }
        /// <summary>
        /// 是否尝试提取集数。`true`表示提取
        /// </summary>
        public bool ParseEpisodeNumber { get; }
        /// <summary>
        /// 是否尝试提取本集标题。`true`表示提取
        /// </summary>
        public bool ParseEpisodeTitle { get; }
        /// <summary>
        /// 是否提取文件扩展名。`true`表示提取
        /// </summary>
        public bool ParseFileExtension { get; }
        /// <summary>
        /// 是否提取发布组。`true`表示提取
        /// </summary>
        public bool ParseReleaseGroup { get; }
        /// <summary>
        /// 提取元素时的配置项
        /// </summary>
        /// <param name="delimiters">默认值：" _.+,|"</param>
        /// <param name="episode">默认值：true</param>
        /// <param name="title">默认值：true</param>
        /// <param name="extension">默认值：true</param>
        /// <param name="group">默认值：true</param>
        public Options(string delimiters = " _.+,|　", bool episode = true, bool title = true, bool extension = true, bool group = true)
        {
            AllowedDelimiters = delimiters;
            ParseEpisodeNumber = episode;
            ParseEpisodeTitle = title;
            ParseFileExtension = extension;
            ParseReleaseGroup = group;
        }
    }
}
