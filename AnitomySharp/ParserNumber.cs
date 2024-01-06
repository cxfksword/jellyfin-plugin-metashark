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
using System.Text.RegularExpressions;

namespace AnitomySharp
{

    /// <summary>
    /// A utility class to assist in number parsing.
    /// </summary>
    public class ParserNumber
    {
        /// <summary>
        /// 动画最小计算年份
        /// </summary>
        public const int AnimeYearMin = 1900;
        /// <summary>
        /// 动画最大计算年份
        /// </summary>
        public const int AnimeYearMax = 2100;
        /// <summary>
        /// 
        /// </summary>
        private const int EpisodeNumberMax = 9999;
        /// <summary>
        /// 最大卷数
        /// </summary>
        private const int VolumeNumberMax = 99;
        /// <summary>
        /// 正则开头
        /// </summary>
        public const string RegexMatchOnlyStart = @"\A(?:";
        /// <summary>
        /// 正则结尾
        /// </summary>
        public const string RegexMatchOnlyEnd = @")\z";
        /// <summary>
        /// 
        /// </summary>
        private readonly Parser _parser;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parser"></param>
        public ParserNumber(Parser parser)
        {
            _parser = parser;
        }

        /// <summary>
        /// Returns whether or not the <c>number</c> is a volume number
        /// 
        /// 返验证卷数字符串是否有效
        /// </summary>
        private static bool IsValidVolumeNumber(string number)
        {
            return StringHelper.StringToInt(number) <= VolumeNumberMax;
        }

        /// <summary>
        /// Returns whether or not the <c>number</c> is a valid episode number.
        /// 
        /// 验证集数字符串是否有效
        /// </summary>
        private static bool IsValidEpisodeNumber(string number)
        {
            // Eliminate non numeric portion of number, then parse as double.
            var temp = "";
            for (var i = 0; i < number.Length && char.IsDigit(number[i]); i++)
            {
                temp += number[i];
            }

            return !string.IsNullOrEmpty(temp) && double.Parse(temp) <= EpisodeNumberMax;
        }

        /// <summary>
        /// Sets the alternative episode number.
        /// </summary>
        private bool SetAlternativeEpisodeNumber(string number, Token token)
        {
            _parser.Elements.Add(new Element(Element.ElementCategory.ElementEpisodeNumberAlt, number));
            token.Category = Token.TokenCategory.Identifier;
            return true;
        }

        /// <summary>
        /// Sets the volume number.
        /// 
        /// 添加卷数元素
        /// </summary>
        /// <param name="number">the number</param>
        /// <param name="token">the token which contains the volume number</param>
        /// <param name="validate">true if we should check if it's a valid number, false to disable verification</param>
        /// <returns>true if the volume number was set</returns>
        public bool SetVolumeNumber(string number, Token token, bool validate)
        {
            if (validate && !IsValidVolumeNumber(number)) return false;

            _parser.Elements.Add(new Element(Element.ElementCategory.ElementVolumeNumber, number));
            token.Category = Token.TokenCategory.Identifier;
            return true;
        }

        /// <summary>
        /// Sets the anime episode number.
        /// 
        /// 添加集数元素
        /// </summary>
        /// <param name="number">the episode number</param>
        /// <param name="token">the token which contains the volume number</param>
        /// <param name="validate">true if we should check if it's a valid episode number; false to disable validation</param>
        /// <returns>true if the episode number was set</returns>
        public bool SetEpisodeNumber(string number, Token token, bool validate)
        {
            if (validate && !IsValidEpisodeNumber(number)) return false;
            token.Category = Token.TokenCategory.Identifier;
            var category = Element.ElementCategory.ElementEpisodeNumber;

            /** Handle equivalent numbers */
            if (_parser.IsEpisodeKeywordsFound)
            {
                foreach (var element in _parser.Elements)
                {
                    if (element.Category != Element.ElementCategory.ElementEpisodeNumber) continue;

                    /** The larger number gets to be the alternative one */
                    var comparison = StringHelper.StringToInt(number) - StringHelper.StringToInt(element.Value);
                    if (comparison > 0)
                    {
                        category = Element.ElementCategory.ElementEpisodeNumberAlt;
                    }
                    else if (comparison < 0)
                    {
                        element.Category = Element.ElementCategory.ElementEpisodeNumberAlt;
                    }
                    else
                    {
                        return false; /** No need to add the same number twice */
                    }

                    break;
                }
            }

            _parser.Elements.Add(new Element(category, number));
            return true;
        }

        /// <summary>
        /// Checks if a number follows the specified <c>token</c>
        /// 
        /// 确认此标记中是否包含给定元素类型的关键词，如果包含且其能满足匹配模式，则添加此元素
        /// </summary>
        /// <param name="category">the category to set if a number follows the <c>token</c></param>
        /// <param name="token">the token</param>
        /// <returns>true if a number follows the token; false otherwise</returns>
        private bool NumberComesAfterPrefix(Element.ElementCategory category, Token token)
        {
            var numberBegin = ParserHelper.IndexOfFirstDigit(token.Content);
            var prefix = StringHelper.SubstringWithCheck(token.Content, 0, numberBegin).ToUpperInvariant();
            if (!KeywordManager.Contains(category, prefix)) return false;
            var number = StringHelper.SubstringWithCheck(token.Content, numberBegin, token.Content.Length - numberBegin);

            switch (category)
            {
                case Element.ElementCategory.ElementEpisodePrefix:
                    if (!MatchEpisodePatterns(number, token))
                    {
                        SetEpisodeNumber(number, token, false);
                    }
                    return true;
                case Element.ElementCategory.ElementVolumePrefix:
                    if (!MatchVolumePatterns(number, token))
                    {
                        SetVolumeNumber(number, token, false);
                    }
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks whether the number precedes the word "of"
        /// </summary>
        /// <param name="token">the token</param>
        /// <param name="currentTokenIdx">the index of the token</param>
        /// <returns>true if the token precedes the word "of"</returns>
        private bool NumberComesBeforeAnotherNumber(Token token, int currentTokenIdx)
        {
            var separatorToken = Token.FindNextToken(_parser.Tokens, currentTokenIdx, Token.TokenFlag.FlagNotDelimiter);

            if (!Token.InListRange(separatorToken, _parser.Tokens)) return false;
            var separators = new List<Tuple<string, bool>>
      {
        Tuple.Create("&", true),
        Tuple.Create("of", false)
      };

            foreach (var separator in separators)
            {
                if (_parser.Tokens[separatorToken].Content != separator.Item1) continue;
                var otherToken = Token.FindNextToken(_parser.Tokens, separatorToken, Token.TokenFlag.FlagNotDelimiter);
                if (!Token.InListRange(otherToken, _parser.Tokens)
                    || !StringHelper.IsNumericString(_parser.Tokens[otherToken].Content)) continue;
                SetEpisodeNumber(token.Content, token, false);
                if (separator.Item2)
                {
                    SetEpisodeNumber(_parser.Tokens[otherToken].Content, _parser.Tokens[otherToken], false);
                }

                _parser.Tokens[separatorToken].Category = Token.TokenCategory.Identifier;
                _parser.Tokens[otherToken].Category = Token.TokenCategory.Identifier;
                return true;
            }

            return false;
        }

        // EPISODE MATCHERS

        /// <summary>
        /// Attempts to find an episode/season inside a <c>word</c>
        /// 
        /// 在传入的字符串中共尝试匹配季/集
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the word was matched to an episode/season number</returns>
        public bool MatchEpisodePatterns(string word, Token token)
        {
            if (StringHelper.IsNumericString(word)) return false;

            word = word.Trim(" -".ToCharArray());

            // 根据前后是否为数字进行分流处理
            var numericFront = char.IsDigit(word[0]);
            var numericBack = char.IsDigit(word[word.Length - 1]);

            if (numericFront && numericBack)
            {
                // e.g. "01v2"
                if (MatchSingleEpisodePattern(word, token))
                {
                    return true;
                }
                // e.g. "01-02", "03-05v2"

                if (MatchMultiEpisodePattern(word, token))
                {
                    return true;
                }
                // e.g. "07.5"

                if (MatchFractionalEpisodePattern(word, token))
                {
                    return true;
                }
            }

            if (numericBack)
            {
                // e.g. "2x01", "S01E03", "S01-02xE001-150"
                if (MatchSeasonAndEpisodePattern(word, token))
                {
                    return true;
                }
                // e.g. "#01", "#02-03v2"

                if (MatchNumberSignPattern(word, token))
                {
                    return true;
                }
            }

            // e.g. "ED1", "OP4a", "OVA2"
            if (!numericFront && MatchTypeAndEpisodePattern(word, token))
            {
                return true;
            }

            // e.g. "4a", "111C"
            if (numericFront && !numericBack && MatchPartialEpisodePattern(word, token))
            {
                return true;
            }

            // e.g. "01-24Fin"
            if (word.IndexOf("fin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (MatchMultiEpisodePattern(word, token))
                {
                    return true;
                }
            }

            // U+8A71 is used as counter for stories, episodes of TV series, etc.
            return MatchJapaneseCounterPattern(word, token);
        }

        /// <summary>
        /// Match a single episode pattern. e.g. "01v2".
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        private bool MatchSingleEpisodePattern(string word, Token token)
        {
            const string regexPattern = RegexMatchOnlyStart + @"(\d{1,4})[vV](\d)" + RegexMatchOnlyEnd;
            var match = Regex.Match(word, regexPattern);

            if (!match.Success) return false;

            SetEpisodeNumber(match.Groups[1].Value, token, false);
            _parser.Elements.Add(new Element(Element.ElementCategory.ElementReleaseVersion, match.Groups[2].Value));
            return true;
        }

        /// <summary>
        /// Match a multi episode pattern. e.g. "01-02", "03-05v2", "01-24Fin".
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        private bool MatchMultiEpisodePattern(string word, Token token)
        {
            const string regexPattern = RegexMatchOnlyStart + @"(\d{1,4})(?:[vV](\d))?[-~&+](\d{1,4})(?:[vV](\d))?(FIN|Fin|fin)?" + RegexMatchOnlyEnd;
            var match = Regex.Match(word, regexPattern);
            if (!match.Success) return false;

            var lowerBound = match.Groups[1].Value;
            var upperBound = match.Groups[3].Value;

            /** Avoid matching expressions such as "009-1" or "5-2" */
            if (StringHelper.StringToInt(lowerBound) >= StringHelper.StringToInt(upperBound)) return false;
            if (!SetEpisodeNumber(lowerBound, token, true)) return false;
            SetEpisodeNumber(upperBound, token, true);
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                _parser.Elements.Add(new Element(Element.ElementCategory.ElementReleaseVersion, match.Groups[2].Value));
            }
            if (!string.IsNullOrEmpty(match.Groups[4].Value))
            {
                _parser.Elements.Add(new Element(Element.ElementCategory.ElementReleaseVersion, match.Groups[4].Value));
            }
            if (!string.IsNullOrEmpty(match.Groups[5].Value))
            {
                _parser.Elements.Add(new Element(Element.ElementCategory.ElementReleaseInformation, match.Groups[5].Value));
            }
            return true;

        }

        /// <summary>
        /// Match season and episode patterns. e.g. "2x01", "S01E03", "S01-02xE001-150", "S01E06v2".
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        private bool MatchSeasonAndEpisodePattern(string word, Token token)
        {
            const string regexPattern = RegexMatchOnlyStart + @"S?(\d{1,2})(?:-S?(\d{1,2}))?(?:x|[ ._-x]?EP?)(\d{1,4})(?:-E?P?(\d{1,4}))?(?:[vV](\d{1,2}))?" + RegexMatchOnlyEnd;
            var match = Regex.Match(word, regexPattern);
            if (!match.Success) return false;

            _parser.Elements.Add(new Element(Element.ElementCategory.ElementAnimeSeason, match.Groups[1].Value));
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                _parser.Elements.Add(new Element(Element.ElementCategory.ElementAnimeSeason, match.Groups[2].Value));
            }
            SetEpisodeNumber(match.Groups[3].Value, token, false);
            if (!string.IsNullOrEmpty(match.Groups[4].Value))
            {
                SetEpisodeNumber(match.Groups[4].Value, token, false);
            }
            return true;
        }

        /// <summary>
        /// Match type and episode. e.g. "ED1", "OP4a", "OVA2".
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        private bool MatchTypeAndEpisodePattern(string word, Token token)
        {
            var numberBegin = ParserHelper.IndexOfFirstDigit(word);
            var prefix = StringHelper.SubstringWithCheck(word, 0, numberBegin);

            var category = Element.ElementCategory.ElementAnimeType;
            var options = new KeywordOptions();

            if (!KeywordManager.FindAndSet(KeywordManager.Normalize(prefix), ref category, ref options)) return false;
            _parser.Elements.Add(new Element(Element.ElementCategory.ElementAnimeType, prefix));
            var number = word.Substring(numberBegin);
            if (!MatchEpisodePatterns(number, token) && !SetEpisodeNumber(number, token, true)) return false;
            var foundIdx = _parser.Tokens.IndexOf(token);
            if (foundIdx == -1) return true;
            token.Content = number;
            _parser.Tokens.Insert(foundIdx,
              new Token(options.Identifiable ? Token.TokenCategory.Identifier : Token.TokenCategory.Unknown, token.Enclosed, prefix));

            return true;

        }

        /// <summary>
        /// Match fractional episodes. e.g. "07.5"
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        private bool MatchFractionalEpisodePattern(string word, Token token)
        {
            if (string.IsNullOrEmpty(word))
            {
                word = "";
            }

            const string regexPattern = RegexMatchOnlyStart + @"\d+\.5" + RegexMatchOnlyEnd;
            var match = Regex.Match(word, regexPattern);
            return match.Success && SetEpisodeNumber(word, token, true);
        }

        /// <summary>
        /// Match partial episodes. e.g. "4a", "111C".
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        private bool MatchPartialEpisodePattern(string word, Token token)
        {
            if (string.IsNullOrEmpty(word)) return false;
            var foundIdx = Enumerable.Range(0, word.Length)
              .DefaultIfEmpty(word.Length)
              .FirstOrDefault(value => !char.IsDigit(word[value]));
            var suffixLength = word.Length - foundIdx;

            bool IsValidSuffix(int c) => c >= 'A' && c <= 'C' || c >= 'a' && c <= 'c';

            return suffixLength == 1 && IsValidSuffix(word[foundIdx]) && SetEpisodeNumber(word, token, true);
        }

        /// <summary>
        /// Match episodes with number signs. e.g. "#01", "#02-03v2"
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        private bool MatchNumberSignPattern(string word, Token token)
        {
            if (string.IsNullOrEmpty(word) || word[0] != '#') word = "";
            const string regexPattern = RegexMatchOnlyStart + @"#(\d{1,4})(?:[-~&+](\d{1,4}))?(?:[vV](\d))?" + RegexMatchOnlyEnd;
            var match = Regex.Match(word, regexPattern);
            if (!match.Success) return false;

            if (!SetEpisodeNumber(match.Groups[1].Value, token, true)) return false;
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                SetEpisodeNumber(match.Groups[2].Value, token, false);
            }
            if (!string.IsNullOrEmpty(match.Groups[3].Value))
            {
                _parser.Elements.Add(new Element(Element.ElementCategory.ElementReleaseVersion, match.Groups[3].Value));
            }

            return true;

        }

        /// <summary>
        /// Match Japanese patterns. e.g. U+8A71 is used as counter for stories, episodes of TV series, etc.
        /// 
        /// 匹配日文中常见顺序词
        /// 
        /// 符合这种匹配模式的，一般在集数后都紧跟本集标题 #TODO
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        public bool MatchJapaneseCounterPattern(string word, Token token)
        {
            if (string.IsNullOrEmpty(word)) return false;

            // 1st|2nd|3rd| #TODO
            string regexPattern = @"(Ⅰ|Ⅱ|Ⅲ)";
            var match = Regex.Match(word, RegexMatchOnlyStart + regexPattern + RegexMatchOnlyEnd, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var episodeNumber = ParserHelper.GetNumberFromOrdinal(match.Groups[1].Value);
                SetEpisodeNumber(episodeNumber, token, false);
                return true;
            }

            regexPattern = @"([上中下前後])([巻卷編编])";
            match = Regex.Match(word, RegexMatchOnlyStart + regexPattern + RegexMatchOnlyEnd, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var episodeNumber = ParserHelper.GetNumberFromOrdinal(match.Groups[1].Value);
                SetEpisodeNumber(episodeNumber, token, false);
                return true;
            }

            regexPattern = @"([第全]?)([0-9一二三四五六七八九十壱弐参]+)([期章話话巻卷幕夜期発縛])";
            match = Regex.Match(word, RegexMatchOnlyStart + regexPattern + RegexMatchOnlyEnd, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var episodeNumber = match.Groups[2].Value;
                if (!StringHelper.IsNumericString(episodeNumber))
                {
                    episodeNumber = ParserHelper.GetNumberFromOrdinal(episodeNumber);
                }
                SetEpisodeNumber(episodeNumber, token, false);
                return true;
            }

            regexPattern = @"(vol|EPISODE|ACT|scene|ep|volume|screen|voice|case|menu|rail|round|game|page|collection|cage|office|doll|Princess)([ \.\-_])([0-9]+)";
            match = Regex.Match(word, RegexMatchOnlyStart + regexPattern + RegexMatchOnlyEnd, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var episodeNumber = match.Groups[3].Value;
                SetEpisodeNumber(episodeNumber, token, false);
                return true;
            }
            else
            {
                return false;
            }

        }

        // VOLUME MATCHES

        /// <summary>
        /// Attempts to find an episode/season inside a <c>word</c>
        /// 
        /// 在传入的字符串中共尝试匹配季/集
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the word was matched to an episode/season number</returns>
        public bool MatchVolumePatterns(string word, Token token)
        {
            // All patterns contain at least one non-numeric character
            if (StringHelper.IsNumericString(word)) return false;

            word = word.Trim(" -".ToCharArray());

            var numericFront = char.IsDigit(word[0]);
            var numericBack = char.IsDigit(word[word.Length - 1]);

            if (numericFront && numericBack)
            {
                // e.g. "01v2"                                    e.g. "01-02", "03-05v2"
                return MatchSingleVolumePattern(word, token) || MatchMultiVolumePattern(word, token);
            }

            return false;
        }

        /// <summary>
        /// Match single volume. e.g. "01v2"
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        private bool MatchSingleVolumePattern(string word, Token token)
        {
            if (string.IsNullOrEmpty(word)) word = "";
            const string regexPattern = RegexMatchOnlyStart + @"(\d{1,2})[vV](\d)" + RegexMatchOnlyEnd;
            var match = Regex.Match(word, regexPattern);
            if (!match.Success) return false;

            SetVolumeNumber(match.Groups[1].Value, token, false);
            _parser.Elements.Add(new Element(Element.ElementCategory.ElementReleaseVersion, match.Groups[2].Value));
            return true;
        }

        /// <summary>
        /// Match multi-volume. e.g. "01-02", "03-05v2".
        /// </summary>
        /// <param name="word">the word</param>
        /// <param name="token">the token</param>
        /// <returns>true if the token matched</returns>
        private bool MatchMultiVolumePattern(string word, Token token)
        {
            if (string.IsNullOrEmpty(word)) word = "";
            const string regexPattern = RegexMatchOnlyStart + @"(\d{1,2})[-~&+](\d{1,2})(?:[vV](\d))?" + RegexMatchOnlyEnd;
            var match = Regex.Match(word, regexPattern);
            if (!match.Success) return false;

            var lowerBound = match.Groups[1].Value;
            var upperBound = match.Groups[2].Value;
            if (StringHelper.StringToInt(lowerBound) >= StringHelper.StringToInt(upperBound)) return false;
            if (!SetVolumeNumber(lowerBound, token, true)) return false;
            SetVolumeNumber(upperBound, token, false);
            if (string.IsNullOrEmpty(match.Groups[3].Value))
            {
                _parser.Elements.Add(new Element(Element.ElementCategory.ElementReleaseVersion, match.Groups[3].Value));
            }
            return true;

        }

        // SEARCH

        /// <summary>
        /// Searches for isolated numbers in a list of <c>tokens</c>.
        /// 
        /// 搜索孤立数字
        /// </summary>
        /// <param name="tokens">the list of tokens</param>
        /// <returns>true if an isolated number was found</returns>
        public bool SearchForIsolatedNumbers(IEnumerable<int> tokens)
        {
            return tokens
              .Where(it => _parser.Tokens[it].Enclosed && _parser.ParseHelper.IsTokenIsolated(it))
              .Any(it => SetEpisodeNumber(_parser.Tokens[it].Content, _parser.Tokens[it], true));
        }

        /// <summary>
        /// Searches for separated numbers in a list of <c>tokens</c>.
        /// 
        /// 搜索带分隔符的数字
        /// </summary>
        /// <param name="tokens">the list of tokens</param>
        /// <returns>true fi a separated number was found</returns>
        public bool SearchForSeparatedNumbers(List<int> tokens)
        {
            foreach (var it in tokens)
            {
                var previousToken = Token.FindPrevToken(_parser.Tokens, it, Token.TokenFlag.FlagNotDelimiter);

                // See if the number has a preceding "-" separator
                if (!_parser.ParseHelper.IsTokenCategory(previousToken, Token.TokenCategory.Unknown)
                    || !ParserHelper.IsDashCharacter(_parser.Tokens[previousToken].Content[0])) continue;
                if (!SetEpisodeNumber(_parser.Tokens[it].Content, _parser.Tokens[it], true)) continue;
                _parser.Tokens[previousToken].Category = Token.TokenCategory.Identifier;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Searches for episode patterns in a list of <c>tokens</c>.
        /// 
        /// 在标记列表中匹配集数模式
        /// </summary>
        /// <param name="tokens">the list of tokens</param>
        /// <returns>true if an episode number was found</returns>
        public bool SearchForEpisodePatterns(List<int> tokens)
        {
            foreach (var it in tokens)
            {
                var numericFront = _parser.Tokens[it].Content.Length > 0 && char.IsDigit(_parser.Tokens[it].Content[0]);

                if (!numericFront)
                {
                    // e.g. "EP.1", "Vol.1"
                    if (NumberComesAfterPrefix(Element.ElementCategory.ElementEpisodePrefix, _parser.Tokens[it]))
                    {
                        return true;
                    }
                    if (NumberComesAfterPrefix(Element.ElementCategory.ElementVolumePrefix, _parser.Tokens[it]))
                    {
                        continue;
                    }
                }
                else
                {
                    // e.g. "8 & 10", "01 of 24"
                    if (NumberComesBeforeAnotherNumber(_parser.Tokens[it], it))
                    {
                        return true;
                    }
                }

                // Look for other patterns
                if (MatchEpisodePatterns(_parser.Tokens[it].Content, _parser.Tokens[it]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Searches for equivalent number in a list of <c>tokens</c>. e.g. 08(114)
        /// 
        /// 匹配自带等效集数的数字，常见于分割放送
        /// </summary>
        /// <param name="tokens">the list of tokens</param>
        /// <returns>true if an equivalent number was found</returns>
        public bool SearchForEquivalentNumbers(List<int> tokens)
        {
            foreach (var it in tokens)
            {
                // Find number must be isolated.
                if (_parser.ParseHelper.IsTokenIsolated(it) || !IsValidEpisodeNumber(_parser.Tokens[it].Content))
                {
                    continue;
                }

                // Find the first enclosed, non-delimiter token
                var nextToken = Token.FindNextToken(_parser.Tokens, it, Token.TokenFlag.FlagNotDelimiter);
                if (!_parser.ParseHelper.IsTokenCategory(nextToken, Token.TokenCategory.Bracket)) continue;
                nextToken = Token.FindNextToken(_parser.Tokens, nextToken, Token.TokenFlag.FlagEnclosed,
                  Token.TokenFlag.FlagNotDelimiter);
                if (!_parser.ParseHelper.IsTokenCategory(nextToken, Token.TokenCategory.Unknown)) continue;

                // Check if it's an isolated number
                if (!_parser.ParseHelper.IsTokenIsolated(nextToken)
                  || !StringHelper.IsNumericString(_parser.Tokens[nextToken].Content)
                  || !IsValidEpisodeNumber(_parser.Tokens[nextToken].Content))
                {
                    continue;
                }

                var list = new List<Token>
        {
          _parser.Tokens[it], _parser.Tokens[nextToken]
        };

                list.Sort((o1, o2) => StringHelper.StringToInt(o1.Content) - StringHelper.StringToInt(o2.Content));
                SetEpisodeNumber(list[0].Content, list[0], false);
                SetAlternativeEpisodeNumber(list[1].Content, list[1]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Searches for the last number token in a list of <c>tokens</c>
        /// 
        /// 搜索最后一个数字
        /// </summary>
        /// <param name="tokens">the list of tokens</param>
        /// <returns>true if the last number token was found</returns>
        public bool SearchForLastNumber(List<int> tokens)
        {
            for (var i = tokens.Count - 1; i >= 0; i--)
            {
                var it = tokens[i];

                // Assuming that episode number always comes after the title,
                // the first token cannot be what we're looking for
                if (it == 0) continue;
                if (_parser.Tokens[it].Enclosed) continue;

                // Ignore if it's the first non-enclosed, non-delimiter token
                if (_parser.Tokens.GetRange(0, it)
                  .All(r => r.Enclosed || r.Category == Token.TokenCategory.Delimiter))
                {
                    continue;
                }

                var previousToken = Token.FindPrevToken(_parser.Tokens, it, Token.TokenFlag.FlagNotDelimiter);
                if (_parser.ParseHelper.IsTokenCategory(previousToken, Token.TokenCategory.Unknown))
                {
                    if (_parser.Tokens[previousToken].Content.Equals("Movie", StringComparison.InvariantCultureIgnoreCase)
                        || _parser.Tokens[previousToken].Content.Equals("Part", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }
                }

                // We'll use this number after all
                if (SetEpisodeNumber(_parser.Tokens[it].Content, _parser.Tokens[it], true))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
