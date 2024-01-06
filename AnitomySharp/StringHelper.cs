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
using System.Linq;

namespace AnitomySharp
{

    /// <summary>
    /// A string helper class that is analogous to <c>string.cpp</c> of the original Anitomy, and <c>StringHelper.java</c> of AnitomyJ.
    /// </summary>
    public static class StringHelper
    {

        /// <summary>
        /// Returns whether or not the character is alphanumeric
        /// 
        /// 如果给定字符为字母或数字，则返回`true`
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsAlphanumericChar(char c)
        {
            return c >= '0' && c <= '9' || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z';
        }

        /// <summary>
        /// Returns whether or not the character is a hex character.
        /// 
        /// 如果给定字符为十六进制字符，则返回`true`
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool IsHexadecimalChar(char c)
        {
            return c >= '0' && c <= '9' || c >= 'A' && c <= 'F' || c >= 'a' && c <= 'f';
        }

        /// <summary>
        /// Returns whether or not the character is a latin character
        /// 
        /// 判断给定字符是否为拉丁字符
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool IsLatinChar(char c)
        {
            // We're just checking until the end of the Latin Extended-B block, 
            // rather than all the blocks that belong to the Latin script.
            return c <= '\u024F';
        }

        /// <summary>
        /// Returns whether or not the character is a Chinese character
        /// 
        /// 判断给定字符是否为中文字符
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool IsChineseChar(char c)
        {
            // We're just checking until the end of the Latin Extended-B block, 
            // rather than all the blocks that belong to the Latin script.
            return c <= '\u9FFF' && c >= '\u4E00';
        }

        /// <summary>
        /// Returns whether or not the <c>str</c> is a hex string.
        /// 
        /// 如果给定字符串为十六进制字符串，则返回`true`
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsHexadecimalString(string str)
        {
            return !string.IsNullOrEmpty(str) && str.All(IsHexadecimalChar);
        }

        /// <summary>
        /// Returns whether or not the <c>str</c> is mostly a latin string.
        /// 
        /// 判断给定字符串是否过半字符为拉丁
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsMostlyLatinString(string str)
        {
            var length = !string.IsNullOrEmpty(str) ? 1.0 : str.Length;
            return str.Where(IsLatinChar).Count() / length >= 0.5;
        }

        /// <summary>
        /// Returns whether or not the <c>str</c> is mostly a Chinese string.
        /// 
        /// 判断给定字符串是否过半字符为中文
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsMostlyChineseString(string str)
        {
            var length = !string.IsNullOrEmpty(str) ? 1.0 : str.Length;
            return str.Where(IsChineseChar).Count() / length >= 0.5;
        }
        /// <summary>
        /// Returns whether or not the <c>str</c> is a numeric string.
        /// 
        /// 判断字符串是否全数字
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNumericString(string str)
        {
            return str.All(char.IsDigit);
        }
        /// <summary>
        /// Returns whether or not the <c>str</c> is a alpha string.
        /// 
        /// 判断字符串是否全字母
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsAlphaString(string str)
        {
            return str.All(char.IsLetter);
        }

        /// <summary>
        /// Returns the int value of the <c>str</c>; 0 otherwise.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int StringToInt(string str)
        {
            try
            {
                return int.Parse(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 0;
            }
        }

        /// <summary>
        /// 提取给定范围的子字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string SubstringWithCheck(string str, int start, int count)
        {
            if (start + count > str.Length) count = str.Length - start;
            return str.Substring(start, count);
        }
    }
}
