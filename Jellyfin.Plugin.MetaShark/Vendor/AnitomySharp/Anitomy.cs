/*
 * Copyright (c) 2014-2017, Eren Okka
 * Copyright (c) 2016-2017, Paul Miller
 * Copyright (c) 2017-2018, Tyler Bratton
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;

using System.Collections.Generic;
using System.Linq;

namespace AnitomySharp
{

    /// <summary>
    /// A library capable of parsing Anime filenames. 
    /// 
    /// 用于解析动漫文件名的库。
    /// 
    /// This code is a  C++ to C# port of <see href="https://github.com/erengy/anitomy">Anitomy</see>,
    /// using the already existing Java port <see href="https://github.com/Vorror/anitomyJ">AnitomyJ</see> as a reference.
    /// </summary>
    public class AnitomySharp
    {
        /// <summary>
        /// 
        /// </summary>
        private AnitomySharp() { }

        /// <summary>
        /// Parses an anime <paramref name="filename"/> into its consituent elements. 
        /// 
        /// 将动画文件名拆分为其组成元素。
        /// </summary>
        /// <param name="filename">the anime file name 动画文件名</param>
        /// <returns>the list of parsed elements 分解后的元素列表</returns>
        public static IEnumerable<Element> Parse(string filename)
        {
            return Parse(filename, new Options());
        }

        /// <summary>
        /// Parses an anime <paramref name="filename"/> into its constituent elements. 
        /// 
        /// 将动画文件名拆分为其组成元素。
        /// </summary>
        /// <param name="filename">the anime file name 动画文件名</param>
        /// <param name="options">the options to parse with, use <see cref="Parse(string)"/> to use default options</param>
        /// <returns>the list of parsed elements 分解后的元素列表</returns>
        /// <remarks>**逻辑：**
        /// 1. 提取文件扩展名；
        /// 2. 
        /// 3. #TODO
        /// </remarks>
        public static IEnumerable<Element> Parse(string filename, Options options)
        {
            var elements = new List<Element>(32);
            var tokens = new List<Token>();

            /** remove/parse extension */
            var fname = filename;
            if (options.ParseFileExtension)
            {
                var extension = "";
                if (RemoveExtensionFromFilename(ref fname, ref extension))
                {
                    /** 将文件扩展名元素加入元素列表 */
                    elements.Add(new Element(Element.ElementCategory.ElementFileExtension, extension));
                }
            }

            /** set filename */
            if (string.IsNullOrEmpty(filename))
            {
                return elements;
            }
            /** 将去除扩展名后的文件名加入元素列表 */
            elements.Add(new Element(Element.ElementCategory.ElementFileName, fname));

            /** tokenize
            1. 根据括号、一眼真的关键词、分隔符进行分词（带先后顺序）
            2. 只将一眼真的关键字加入元素列表
             */
            var isTokenized = new Tokenizer(fname, elements, options, tokens).Tokenize();
            if (!isTokenized)
            {
                return elements;
            }
            new Parser(elements, options, tokens).Parse();

            // elements.ForEach(x => Console.WriteLine("\"" + x.Category + "\"" + ": " + "\"" + x.Value + "\""));

            return elements;
        }


        /// <summary>
        /// Removes the extension from the <paramref name="filename"/> 
        /// 
        /// 确认扩展名有效，即在指定的<see cref="Element.ElementCategory.ElementFileExtension">文件扩展名元素类别</see>中，然后去除文件扩展名
        /// </summary>
        /// <param name="filename">the ref that will be updated with the new filename</param>
        /// <param name="extension">the ref that will be updated with the file extension</param>
        /// <returns>if the extension was successfully separated from the filename</returns>
        private static bool RemoveExtensionFromFilename(ref string filename, ref string extension)
        {
            int position;
            if (string.IsNullOrEmpty(filename) || (position = filename.LastIndexOf('.')) == -1)
            {
                return false;
            }

            /** remove file extension */
            extension = filename.Substring(position + 1);
            if (extension.Length > 4 || !extension.All(char.IsLetterOrDigit))
            {
                return false;
            }

            /** check if valid anime extension */
            var keyword = KeywordManager.Normalize(extension);
            if (!KeywordManager.Contains(Element.ElementCategory.ElementFileExtension, keyword))
            {
                return false;
            }

            filename = filename.Substring(0, position);
            return true;
        }
    }
}