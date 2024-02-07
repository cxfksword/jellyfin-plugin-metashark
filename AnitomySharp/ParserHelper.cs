/*
 * Copyright (c) 2014-2017, Eren Okka
 * Copyright (c) 2016-2017, Paul Miller
 * Copyright (c) 2017-2018, Tyler Bratton
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnitomySharp
{

    /// <summary>
    /// Utility class to assist in the parsing.
    /// 
    /// 辅助解析的工具类
    /// </summary>
    public class ParserHelper
    {
        /// <summary>
        /// 破折号
        /// </summary>
        private const string Dashes = "-\u2010\u2011\u2012\u2013\u2014\u2015";
        /// <summary>
        /// 带空格的破折号
        /// </summary>
        private const string DashesWithSpace = " -\u2010\u2011\u2012\u2013\u2014\u2015";
        /// <summary>
        /// 英文与数字匹配词典
        /// </summary>
        private static readonly Dictionary<string, string> Ordinals = new Dictionary<string, string>
    {
      {"1st", "1"}, {"First", "1"},
      {"2nd", "2"}, {"Second", "2"},
      {"3rd", "3"}, {"Third", "3"},
      {"4th", "4"}, {"Fourth", "4"},
      {"5th", "5"}, {"Fifth", "5"},
      {"6th", "6"}, {"Sixth", "6"},
      {"7th", "7"}, {"Seventh", "7"},
      {"8th", "8"}, {"Eighth", "8"},
      {"9th", "9"}, {"Ninth", "9"},
      {"一", "1"}, {"壱", "1"},
      {"二", "2"}, {"弐", "2"},
      {"三", "3"}, {"参", "3"},
      {"四", "4"}, {"上", "1"},
      {"五", "5"}, {"下", "2"},
      {"六", "6"}, {"前", "1"},
      {"七", "7"}, {"後", "2"},
      {"八", "8"}, {"中", "2"}, //most only 2 episodes
      {"九", "9"}, {"Ⅰ", "1"},
      {"十", "10"},{"Ⅱ", "2"},
      {"Ⅲ", "3"}
    };

        /// <summary>
        /// 
        /// </summary>
        private readonly Parser _parser;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parser"></param>
        public ParserHelper(Parser parser)
        {
            _parser = parser;
        }

        /// <summary>
        /// Returns whether or not the <c>result</c> matches the <c>category</c>.
        /// 
        /// 判断传入的标记(token)的类型是否与传入的类别一致
        /// </summary>
        /// <param name="result"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public bool IsTokenCategory(int result, Token.TokenCategory category)
        {
            return Token.InListRange(result, _parser.Tokens) && _parser.Tokens[result].Category == category;
        }

        /// <summary>
        /// Returns whether or not the <c>str</c> is a CRC string.
        /// 
        /// 如果给定字符串为CRC，则返回`true`
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsCrc32(string str)
        {
            return str != null && str.Length == 8 && StringHelper.IsHexadecimalString(str);
        }
        /// <summary>
        /// Returns whether or not the <c>character</c> is a dash character
        /// 
        /// 判断给定字符是否为破折号
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsDashCharacter(char c)
        {
            return Dashes.Contains(c.ToString());
        }

        /// <summary>
        /// Returns a number from an original (e.g. 2nd)
        /// 
        /// 转换原始值中的英文数字
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetNumberFromOrdinal(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return Ordinals.TryGetValue(str, out var foundString) ? foundString : "";
        }

        /// <summary>
        /// Returns the index of the first digit in the <c>str</c>; -1 otherwise.
        /// 
        /// 返回<c>str</c>中第一个数字的索引位置
        /// </summary>
        /// <param name="str"></param>
        /// <returns>如果无数字，则返回-1</returns>
        public static int IndexOfFirstDigit(string str)
        {
            if (string.IsNullOrEmpty(str)) return -1;
            for (var i = 0; i < str.Length; i++)
            {
                if (char.IsDigit(str, i))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns whether or not the <c>str</c> is a resolution.
        /// 
        /// 如果给定字符串为分辨率，则返回`true`
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsResolution(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            const int minWidthSize = 3;
            const int minHeightSize = 3;

            if (str.Length >= minWidthSize + 1 + minHeightSize)
            {
                var pos = str.IndexOfAny("xX\u00D7".ToCharArray());
                if (pos == -1 || pos < minWidthSize || pos > str.Length - (minHeightSize + 1)) return false;
                return !str.Where((t, i) => i != pos && !char.IsDigit(t)).Any();
            }

            if (str.Length < minHeightSize + 1) return false;
            {
                if (char.ToLower(str[str.Length - 1]) != 'p') return false;
                for (var i = 0; i < str.Length - 1; i++)
                {
                    if (!char.IsDigit(str[i])) return false;
                }

                return true;
            }

        }
        /// <summary>
        /// Returns whether or not the <c>category</c> is searchable.
        /// 
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public bool IsElementCategorySearchable(Element.ElementCategory category)
        {
            switch (category)
            {
                case Element.ElementCategory.ElementAnimeSeasonPrefix:
                case Element.ElementCategory.ElementAnimeType:
                case Element.ElementCategory.ElementAudioTerm:
                case Element.ElementCategory.ElementDeviceCompatibility:
                case Element.ElementCategory.ElementEpisodePrefix:
                case Element.ElementCategory.ElementFileChecksum:
                case Element.ElementCategory.ElementLanguage:
                case Element.ElementCategory.ElementOther:
                case Element.ElementCategory.ElementReleaseGroup:
                case Element.ElementCategory.ElementReleaseInformation:
                case Element.ElementCategory.ElementReleaseVersion:
                case Element.ElementCategory.ElementSource:
                case Element.ElementCategory.ElementSubtitles:
                case Element.ElementCategory.ElementVideoResolution:
                case Element.ElementCategory.ElementVideoTerm:
                case Element.ElementCategory.ElementVolumePrefix:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns whether the <c>category</c> is singular.
        /// 
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public bool IsElementCategorySingular(Element.ElementCategory category)
        {
            switch (category)
            {
                case Element.ElementCategory.ElementAnimeSeason:
                case Element.ElementCategory.ElementAnimeType:
                case Element.ElementCategory.ElementAudioTerm:
                case Element.ElementCategory.ElementDeviceCompatibility:
                case Element.ElementCategory.ElementEpisodeNumber:
                case Element.ElementCategory.ElementLanguage:
                case Element.ElementCategory.ElementOther:
                case Element.ElementCategory.ElementReleaseInformation:
                case Element.ElementCategory.ElementSource:
                case Element.ElementCategory.ElementVideoTerm:
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns whether or not a token at the current <c>pos</c> is isolated(surrounded by braces).
        /// 
        /// 判断当前位置标记(token)是否孤立，是否被括号包裹
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsTokenIsolated(int pos)
        {
            var prevToken = Token.FindPrevToken(_parser.Tokens, pos, Token.TokenFlag.FlagNotDelimiter);
            if (!IsTokenCategory(prevToken, Token.TokenCategory.Bracket)) return false;
            var nextToken = Token.FindNextToken(_parser.Tokens, pos, Token.TokenFlag.FlagNotDelimiter);
            return IsTokenCategory(nextToken, Token.TokenCategory.Bracket);
        }
        /// <summary>
        /// Returns whether or not a token at the current <c>pos</c> is isolated(surrounded by braces, delimiter).
        /// 
        /// 判断当前位置标记(token)是否孤立，前面是否为分隔符，后面是否为括号包裹
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsTokenIsolatedWithDelimiterAndBracket(int pos)
        {
            var prevToken = Token.FindPrevToken(_parser.Tokens, pos, Token.TokenFlag.FlagNone);
            if (!IsTokenCategory(prevToken, Token.TokenCategory.Delimiter)) return false;
            var nextToken = Token.FindNextToken(_parser.Tokens, pos, Token.TokenFlag.FlagNotDelimiter);
            return IsTokenCategory(nextToken, Token.TokenCategory.Bracket);
        }

        /// <summary>
        /// Returns whether or not a token at the current <c>pos+1</c> is ElementAnimeType.
        /// 
        /// 判断当前标记(token)的下一个标记的类型是否为ElementAnimeType。如果是，则返回`true`
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsNextTokenContainAnimeType(int pos)
        {
            var prevToken = Token.FindPrevToken(_parser.Tokens, pos, Token.TokenFlag.FlagNotDelimiter);
            if (!IsTokenCategory(prevToken, Token.TokenCategory.Bracket)) return false;
            var nextToken = Token.FindNextToken(_parser.Tokens, pos, Token.TokenFlag.FlagNotDelimiter);
            if (nextToken < 0) return false;
            return KeywordManager.Contains(Element.ElementCategory.ElementAnimeType, _parser.Tokens[nextToken].Content);
        }
        /// <summary>
        /// 判断当前标记(token)的上一个标记的类型是否为ElementAnimeType。如果是，则返回`true`
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsPrevTokenContainAnimeType(int pos)
        {
            var prevToken = Token.FindPrevToken(_parser.Tokens, pos, Token.TokenFlag.FlagNotDelimiter);
            var nextToken = Token.FindNextToken(_parser.Tokens, pos, Token.TokenFlag.FlagNotDelimiter);
            if (!IsTokenCategory(nextToken, Token.TokenCategory.Bracket)) return false;
            if (prevToken < 0) return false;
            return KeywordManager.Contains(Element.ElementCategory.ElementAnimeType, _parser.Tokens[prevToken].Content);
        }
        /// <summary>
        /// 判断当前标记(token)的上一个标记的类型是否为ElementAnimeType（在 PeekEntries 中）。如果是，则返回`true`
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsPrevTokenContainAnimeTypeInPeekEntries(int pos)
        {
            var prevToken = Token.FindPrevToken(_parser.Tokens, pos, Token.TokenFlag.FlagNotDelimiter);
            var nextToken = Token.FindNextToken(_parser.Tokens, pos, Token.TokenFlag.FlagNotDelimiter);
            if (!IsTokenCategory(nextToken, Token.TokenCategory.Bracket)) return false;
            if (prevToken < 0) return false;
            return KeywordManager.ContainsInPeekEntries(Element.ElementCategory.ElementAnimeType, _parser.Tokens[prevToken].Content);
        }

        /// <summary>
        /// Finds and sets the anime season keyword.
        /// 
        /// 查找动画季度关键词并添加对应元素
        /// </summary>
        /// <param name="token"></param>
        /// <param name="currentTokenPos"></param>
        /// <returns></returns>
        public bool CheckAndSetAnimeSeasonKeyword(Token token, int currentTokenPos)
        {
            void SetAnimeSeason(Token first, Token second, string content)
            {
                _parser.Elements.Add(new Element(Element.ElementCategory.ElementAnimeSeason, content));
                first.Category = Token.TokenCategory.Identifier;
                second.Category = Token.TokenCategory.Identifier;
            }

            var previousToken = Token.FindPrevToken(_parser.Tokens, currentTokenPos, Token.TokenFlag.FlagNotDelimiter);
            if (Token.InListRange(previousToken, _parser.Tokens))
            {
                var number = GetNumberFromOrdinal(_parser.Tokens[previousToken].Content);
                if (!string.IsNullOrEmpty(number))
                {
                    SetAnimeSeason(_parser.Tokens[previousToken], token, number);
                    return true;
                }
            }

            var nextToken = Token.FindNextToken(_parser.Tokens, currentTokenPos, Token.TokenFlag.FlagNotDelimiter);
            if (!Token.InListRange(nextToken, _parser.Tokens) ||
                !StringHelper.IsNumericString(_parser.Tokens[nextToken].Content)) return false;
            SetAnimeSeason(token, _parser.Tokens[nextToken], _parser.Tokens[nextToken].Content);
            return true;

        }

        /// <summary>
        /// A Method to find the correct volume/episode number when prefixed (i.e. Vol.4).
        /// 
        /// 用于查找带前缀的正确的卷数/集数值
        /// </summary>
        /// <param name="category">the category we're searching for</param>
        /// <param name="currentTokenPos">the current token position</param>
        /// <param name="token">the token</param>
        /// <returns>true if we found the volume/episode number</returns>
        public bool CheckExtentKeyword(Element.ElementCategory category, int currentTokenPos, Token token)
        {
            var nToken = Token.FindNextToken(_parser.Tokens, currentTokenPos, Token.TokenFlag.FlagNotDelimiter);
            if (!IsTokenCategory(nToken, Token.TokenCategory.Unknown)) return false;
            if (IndexOfFirstDigit(_parser.Tokens[nToken].Content) != 0) return false;
            switch (category)
            {
                case Element.ElementCategory.ElementEpisodeNumber:
                    if (!_parser.ParseNumber.MatchEpisodePatterns(_parser.Tokens[nToken].Content, _parser.Tokens[nToken]))
                    {
                        _parser.ParseNumber.SetEpisodeNumber(_parser.Tokens[nToken].Content, _parser.Tokens[nToken], false);
                    }
                    break;
                case Element.ElementCategory.ElementVolumeNumber:
                    if (!_parser.ParseNumber.MatchVolumePatterns(_parser.Tokens[nToken].Content, _parser.Tokens[nToken]))
                    {
                        _parser.ParseNumber.SetVolumeNumber(_parser.Tokens[nToken].Content, _parser.Tokens[nToken], false);
                    }
                    break;
            }

            token.Category = Token.TokenCategory.Identifier;
            return true;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="keepDelimiters"></param>
        /// <param name="tokens"></param>
        public void BuildElement(Element.ElementCategory category, bool keepDelimiters, List<Token> tokens)
        {
            var element = new StringBuilder();

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                switch (token.Category)
                {
                    case Token.TokenCategory.Unknown:
                        element.Append(token.Content);
                        token.Category = Token.TokenCategory.Identifier;
                        break;
                    case Token.TokenCategory.Bracket:
                        element.Append(token.Content);
                        break;
                    case Token.TokenCategory.Delimiter:
                        var delimiter = "";
                        if (!string.IsNullOrEmpty(token.Content))
                        {
                            delimiter = token.Content[0].ToString();
                        }

                        if (keepDelimiters)
                        {
                            element.Append(delimiter);
                        }
                        else if (Token.InListRange(i, tokens))
                        {
                            switch (delimiter)
                            {
                                case ",":
                                case "&":
                                    element.Append(delimiter);
                                    break;
                                default:
                                    element.Append(' ');
                                    break;
                            }
                        }
                        break;
                }
            }

            if (!keepDelimiters)
            {
                element = new StringBuilder(element.ToString().Trim(DashesWithSpace.ToCharArray()));
            }

            if (!string.IsNullOrEmpty(element.ToString()))
            {
                _parser.Elements.Add(new Element(category, element.ToString()));
            }
        }
    }
}
