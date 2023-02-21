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
using System.Text;

namespace AnitomySharp
{
    /// <summary>
    /// A class that will tokenize an anime filename.
    /// 
    /// 用于动画文件名标记化的分词器
    /// </summary>
    public class Tokenizer
    {
        /// <summary>
        /// 用于标记化的文件名
        /// </summary>
        private readonly string _filename;
        /// <summary>
        /// 用于添加预处理后标记(token)的元素列表
        /// </summary>
        private readonly List<Element> _elements;
        /// <summary>
        /// 用于解析的配置
        /// </summary>
        private readonly Options _options;
        /// <summary>
        /// 用于存储标记(token)的列表
        /// </summary>
        private readonly List<Token> _tokens;
        /// <summary>
        /// 括号列表
        /// </summary>
        private static readonly List<Tuple<string, string>> Brackets = new List<Tuple<string, string>>
        {
          new Tuple<string, string>("(", ")"), // U+0028-U+0029
          new Tuple<string, string>("[", "]"), // U+005B-U+005D Square bracket
          new Tuple<string, string>("{", "}"), // U+007B-U+007D Curly bracket
          new Tuple<string, string>("\u300C", "\u300D"),  // Corner bracket 「」
          new Tuple<string, string>("\u300E", "\u300F"),  // White corner bracket	『	』
          new Tuple<string, string>("\u3010", "\u3011"), // Black lenticular bracket 【】
          new Tuple<string, string>("\u3014", "\u3015"), // Black lenticular bracket 〔	〕
          new Tuple<string, string>("\u3016", "\u3017"), // Black lenticular bracket 〖	〗
          new Tuple<string, string>("\uFF08", "\uFF09"), // Fullwidth parenthesis （	）
          new Tuple<string, string>("\uFF3B", "\uFF3D"), // Fullwidth parenthesis ［	］
          new Tuple<string, string>("\uFF5B", "\uFF5D") // Fullwidth parenthesis ｛	｝
        };

        /// <summary>
        /// Tokenize a filename into <see cref="Element"/>s
        /// 
        /// 将传入的文件名标记化，拆分为单个元素
        /// 
        /// </summary>
        /// <param name="filename">the filename</param>
        /// <param name="elements">the list of elements where pre-identified tokens will be added</param>
        /// <param name="options">the parser options</param>
        /// <param name="tokens">the list of tokens where tokens will be added</param>
        public Tokenizer(string filename, List<Element> elements, Options options, List<Token> tokens)
        {
            _filename = filename;
            _elements = elements;
            _options = options;
            _tokens = tokens;
        }

        /// <summary>
        /// Returns true if tokenization was successful; false otherwise.
        /// 
        /// 按照括号列表执行分词，根据<see cref="_tokens"/>大小判断是否标记化成功。成功返回true；否则为false。
        /// </summary>
        /// <returns></returns>
        public bool Tokenize()
        {
            TokenizeByBrackets();
            return _tokens.Count > 0;
        }

        /// <summary>
        /// Adds a token to the internal list of tokens
        /// 
        /// 添加标记(token)至<see cref="_tokens">_tokens列表</see>
        /// </summary>
        /// <param name="category">the token category</param>
        /// <param name="enclosed">whether or not the token is enclosed in braces</param>
        /// <param name="range">the token range</param>
        private void AddToken(Token.TokenCategory category, bool enclosed, TokenRange range)
        {
            _tokens.Add(new Token(category, enclosed, StringHelper.SubstringWithCheck(_filename, range.Offset, range.Size)));
        }

        /// <summary>
        /// 根据<see cref="Options.AllowedDelimiters">分隔符配置</see>，提取当前字符串范围内出现过的分隔符
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        private string GetDelimiters(TokenRange range)
        {
            var delimiters = new StringBuilder();

            bool IsDelimiter(char c)
            {
                /** alphanumeric不属于分隔符 */
                if (StringHelper.IsAlphanumericChar(c)) return false;
                return _options.AllowedDelimiters.Contains(c.ToString()) && !delimiters.ToString().Contains(c.ToString());
            }

            foreach (var i in Enumerable.Range(range.Offset, Math.Min(_filename.Length, range.Offset + range.Size) - range.Offset)
              .Where(value => IsDelimiter(_filename[value])))
            {
                delimiters.Append(_filename[i]);
            }

            return delimiters.ToString();
        }

        /// <summary>
        /// Tokenize by bracket.
        /// 
        /// 使用括号列表规则进行分词
        /// </summary>
        /// <remarks>括号总是成对出现。将括号作为停用符，将文件名划为多块</remarks>
        private void TokenizeByBrackets()
        {
            /** 匹配到的(右)括号类型 */
            string matchingBracket = null;

            /** 返回范围内第一个(左)括号位置 */
            int FindFirstBracket(int start, int end)
            {
                for (var i = start; i < end; i++)
                {
                    foreach (var bracket in Brackets)
                    {
                        /** 和括号列表中每对的第一个括号进行比较 */
                        if (!_filename[i].Equals(char.Parse(bracket.Item1))) continue;
                        matchingBracket = bracket.Item2;
                        return i;
                    }
                }

                return -1;
            }

            /** 括号是否闭合 */
            var isBracketOpen = false;
            for (var i = 0; i < _filename.Length;)
            {
                /**用于后续分词的终止位置，其逻辑为： 
                1. 如果括号未闭合(isBracketOpen = false)，使用结果1：获得第一个(左)括号位置
                2. 如果括号闭合(isBracketOpen = true)，使用结果2：查找上一次匹配到的(右)括号(matchingBracket)的位置
                 */
                var foundIdx = !isBracketOpen ? FindFirstBracket(i, _filename.Length) : _filename.IndexOf(matchingBracket, i, StringComparison.Ordinal);

                /** 
                1. 非括号起始至第一个左括号
                2. 左括号右边至右括号左边
                3. 最后一个右括号至末尾 */
                var range = new TokenRange(i, foundIdx == -1 ? _filename.Length : foundIdx - i);
                if (range.Size > 0)
                {
                    // Check if our range contains any known anime identifiers
                    TokenizeByPreidentified(isBracketOpen, range);
                }

                if (foundIdx != -1)
                {
                    // mark as bracket 标记为括号，并添加到_tokens列表
                    AddToken(Token.TokenCategory.Bracket, true, new TokenRange(range.Offset + range.Size, 1));
                    /** 括号是否闭合 取反 */
                    isBracketOpen = !isBracketOpen;
                    i = foundIdx + 1;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Tokenize by looking for known anime identifiers
        /// 
        /// 根据已知的动画关键词列表来分词
        /// </summary>
        /// <param name="enclosed">whether or not the current <c>range</c> is enclosed in braces. 当前范围是否位于闭合的括号中。</param>
        /// <param name="range">the token range 标记的范围</param>
        private void TokenizeByPreidentified(bool enclosed, TokenRange range)
        {
            var preidentifiedTokens = new List<TokenRange>();

            // Find known anime identifiers
            KeywordManager.PeekAndAdd(_filename, range, _elements, preidentifiedTokens);

            var offset = range.Offset;
            var subRange = new TokenRange(range.Offset, 0);
            while (offset < range.Offset + range.Size)
            {
                foreach (var preidentifiedToken in preidentifiedTokens)
                {
                    if (offset != preidentifiedToken.Offset) continue;
                    if (subRange.Size > 0)
                    {
                        TokenizeByDelimiters(enclosed, subRange);
                    }

                    AddToken(Token.TokenCategory.Identifier, enclosed, preidentifiedToken);
                    /** subRange偏移量移至此token后 */
                    subRange.Offset = preidentifiedToken.Offset + preidentifiedToken.Size;
                    offset = subRange.Offset - 1; // It's going to be incremented below
                }

                /** 
                1. 如果这段字符串最后一个字符属于预定义标识(keyword)，则Size为0
                2. 否则，Size大于0 */
                subRange.Size = ++offset - subRange.Offset;
            }

            // Either there was no preidentified token range, or we're now about to process the tail of our current range
            /** 
            1. 没有预定义标识的范围
            2. 处理当前范围剩余部分
             */

            if (subRange.Size > 0)
            {
                TokenizeByDelimiters(enclosed, subRange);
            }
        }

        /// <summary>
        /// Tokenize by delimiters allowed in <see cref="Options"/>.AllowedDelimiters.
        /// 
        /// 使用提取元素时的分隔符配置进行分词
        /// </summary>
        /// <param name="enclosed">whether or not the current <code>range</code> is enclosed in braces</param>
        /// <param name="range">the token range</param>
        private void TokenizeByDelimiters(bool enclosed, TokenRange range)
        {
            var delimiters = GetDelimiters(range);

            /** 如果这段字符串无分隔符，则整个作为Unknown类型的标记(token) */
            if (string.IsNullOrEmpty(delimiters))
            {
                AddToken(Token.TokenCategory.Unknown, enclosed, range);
                return;
            }

            for (int i = range.Offset, end = range.Offset + range.Size; i < end;)
            {
                var found = Enumerable.Range(i, Math.Min(end, _filename.Length) - i)
                  .Where(c => delimiters.Contains(_filename[c].ToString()))
                  .DefaultIfEmpty(end)
                  .FirstOrDefault();

                var subRange = new TokenRange(i, found - i);
                if (subRange.Size > 0)
                {
                    /** 分隔符分割后的字符串作为Unknown类型的标记(token) */
                    AddToken(Token.TokenCategory.Unknown, enclosed, subRange);
                }

                if (found != end)
                {
                    /** 分隔符作为Delimiter类型的标记(token) */
                    AddToken(Token.TokenCategory.Delimiter, enclosed, new TokenRange(subRange.Offset + subRange.Size, 1));
                    i = found + 1;
                }
                else
                {
                    break;
                }
            }

            ValidateDelimiterTokens();
        }

        /// <summary>
        /// Validates tokens (make sure certain words delimited by certain tokens aren't split)
        /// 
        /// 验证标记，确保由配置的分隔符提取标记(token)时<see cref="TokenizeByDelimiters"/>不会将有意义的单词拆分
        /// </summary>
        private void ValidateDelimiterTokens()
        {
            bool IsDelimiterToken(int it)
            {
                return Token.InListRange(it, _tokens) && _tokens[it].Category == Token.TokenCategory.Delimiter;
            }

            bool IsUnknownToken(int it)
            {
                return Token.InListRange(it, _tokens) && _tokens[it].Category == Token.TokenCategory.Unknown;
            }

            bool IsSingleCharacterToken(int it)
            {
                return IsUnknownToken(it) && _tokens[it].Content.Length == 1 && _tokens[it].Content[0] != '-';
            }

            void AppendTokenTo(Token src, Token dest)
            {
                dest.Content += src.Content;
                src.Category = Token.TokenCategory.Invalid;
            }

            for (var i = 0; i < _tokens.Count; i++)
            {
                var token = _tokens[i];
                if (token.Category != Token.TokenCategory.Delimiter) continue;
                var delimiter = token.Content[0];

                var prevToken = Token.FindPrevToken(_tokens, i, Token.TokenFlag.FlagValid);
                var nextToken = Token.FindNextToken(_tokens, i, Token.TokenFlag.FlagValid);

                // Check for single-character tokens to prevent splitting group names,
                // keywords, episode numbers, etc.
                if (delimiter != ' ' && delimiter != '_')
                {

                    // Single character token
                    if (IsSingleCharacterToken(prevToken))
                    {
                        AppendTokenTo(token, _tokens[prevToken]);

                        while (IsUnknownToken(nextToken))
                        {
                            AppendTokenTo(_tokens[nextToken], _tokens[prevToken]);

                            nextToken = Token.FindNextToken(_tokens, i, Token.TokenFlag.FlagValid);
                            if (!IsDelimiterToken(nextToken) || _tokens[nextToken].Content[0] != delimiter) continue;
                            AppendTokenTo(_tokens[nextToken], _tokens[prevToken]);
                            nextToken = Token.FindNextToken(_tokens, nextToken, Token.TokenFlag.FlagValid);
                        }

                        continue;
                    }

                    if (IsSingleCharacterToken(nextToken))
                    {
                        AppendTokenTo(token, _tokens[prevToken]);
                        AppendTokenTo(_tokens[nextToken], _tokens[prevToken]);
                        continue;
                    }
                }

                // Check for adjacent delimiters
                if (IsUnknownToken(prevToken) && IsDelimiterToken(nextToken))
                {
                    var nextDelimiter = _tokens[nextToken].Content[0];
                    if (delimiter != nextDelimiter && delimiter != ',')
                    {
                        if (nextDelimiter == ' ' || nextDelimiter == '_')
                        {
                            AppendTokenTo(token, _tokens[prevToken]);
                        }
                    }
                }
                else if (IsDelimiterToken(prevToken) && IsDelimiterToken(nextToken))
                {
                    var prevDelimiter = _tokens[prevToken].Content[0];
                    var nextDelimiter = _tokens[nextToken].Content[0];
                    if (prevDelimiter == nextDelimiter && prevDelimiter != delimiter)
                    {
                        token.Category = Token.TokenCategory.Unknown; // e.g. "& in "_&_"
                    }
                }

                // Check for other special cases
                if (delimiter != '&' && delimiter != '+') continue;
                if (!IsUnknownToken(prevToken) || !IsUnknownToken(nextToken)) continue;
                if (!StringHelper.IsNumericString(_tokens[prevToken].Content)
                    || !StringHelper.IsNumericString(_tokens[nextToken].Content)) continue;
                AppendTokenTo(token, _tokens[prevToken]);
                AppendTokenTo(_tokens[nextToken], _tokens[prevToken]); // e.g. 01+02
            }

            // Remove invalid tokens
            _tokens.RemoveAll(token => token.Category == Token.TokenCategory.Invalid);
        }
    }
}
