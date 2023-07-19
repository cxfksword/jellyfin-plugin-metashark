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
    /// An anime filename is tokenized into individual <see cref="Token"/>s. This class represents an individual token.
    /// 
    /// 动画文件名被标记化为单一的标记(token)
    /// </summary>
    public class Token
    {
        /// <summary>
        /// The category of the token.
        /// 
        /// 标记(token)类型
        /// </summary>
        public enum TokenCategory
        {
            /// <summary>
            /// 未知类型，
            /// 
            /// 包括：无括号/分隔符的字符串；分隔符分割后的字符串
            /// </summary>
            Unknown,
            /// <summary>
            /// 括号
            /// </summary>
            Bracket,
            /// <summary>
            /// 分隔符，包括Options.AllowedDelimiters
            /// </summary>
            Delimiter,
            /// <summary>
            /// 标识符，包括关键词（一眼真keyword<see cref="KeywordManager.PeekEntries"/>被添加到token）
            /// </summary>
            Identifier,
            /// <summary>
            /// 无效，错误的标记，不会出现在最后的标记(token)列表中。比如在<see cref="Tokenizer.ValidateDelimiterTokens">验证分隔符切分的标记</see>时，规则匹配到的无效标记(token)
            /// </summary>
            Invalid
        }

        /// <summary>
        /// TokenFlag, used for searching specific token categories. This allows granular searching of TokenCategories.
        /// 
        /// 标记符，用于细粒度搜索特定的标记类型(<see cref="TokenCategory"/>)。
        /// </summary>
        public enum TokenFlag
        {
            /// <summary>
            /// None 无
            /// </summary>
            FlagNone,

            // Categories
            /// <summary>
            /// 括号符
            /// </summary>
            FlagBracket,
            /// <summary>
            /// 
            /// </summary>
            FlagNotBracket,
            /// <summary>
            /// 分隔符
            /// </summary>
            FlagDelimiter,
            /// <summary>
            /// 
            /// </summary>
            FlagNotDelimiter,
            /// <summary>
            /// 标识符
            /// </summary>
            FlagIdentifier,
            /// <summary>
            /// 
            /// </summary>
            FlagNotIdentifier,
            /// <summary>
            /// 未知
            /// </summary>
            FlagUnknown,
            /// <summary>
            /// 
            /// </summary>
            FlagNotUnknown,
            /// <summary>
            /// 有效
            /// </summary>
            FlagValid,
            /// <summary>
            /// 
            /// </summary>
            FlagNotValid,

            // Enclosed (Meaning that it is enclosed in some bracket (e.g. [ ] ))
            /// <summary>
            /// 闭合符
            /// </summary>
            FlagEnclosed,
            /// <summary>
            /// 未闭合符
            /// </summary>
            FlagNotEnclosed
        }

        /// <summary>
        /// Set of token category flags
        /// 
        /// 标识符分类列表
        /// </summary>
        private static readonly List<TokenFlag> FlagMaskCategories = new List<TokenFlag>
        {
        TokenFlag.FlagBracket, TokenFlag.FlagNotBracket,
        TokenFlag.FlagDelimiter, TokenFlag.FlagNotDelimiter,
        TokenFlag.FlagIdentifier, TokenFlag.FlagNotIdentifier,
        TokenFlag.FlagUnknown, TokenFlag.FlagNotUnknown,
        TokenFlag.FlagValid, TokenFlag.FlagNotValid
        };

        /// <summary>
        /// Set of token enclosed flags
        /// 
        /// 闭合的标识符列表
        /// </summary>
        private static readonly List<TokenFlag> FlagMaskEnclosed = new List<TokenFlag>
        {
        TokenFlag.FlagEnclosed, TokenFlag.FlagNotEnclosed
        };

        /// <summary>
        /// 标记的类型
        /// </summary>
        public TokenCategory Category { get; set; }
        /// <summary>
        /// 标记的内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 标记是否被括号包裹
        /// </summary>
        public bool Enclosed { get; }

        /// <summary>
        /// Constructs a new token
        /// 
        /// 构造一个新的标记(token)
        /// </summary>
        /// <param name="category">the token category</param>
        /// <param name="enclosed">whether or not the token is enclosed in braces</param>
        /// <param name="content">the token content</param>
        public Token(TokenCategory category, bool enclosed, string content)
        {
            Category = category;
            Enclosed = enclosed;
            Content = content;
        }

        /// <summary>
        /// Validates a token against the <c>flags</c>. The <c>flags</c> is used as a search parameter.
        /// 
        /// 验证传入的标记(token)是否满足标记符(flag)
        /// </summary>
        /// <param name="token">the token</param>
        /// <param name="flags">the flags the token must conform against</param>
        /// <returns>true if the token conforms to the set of <c>flags</c>; false otherwise</returns>
        private static bool CheckTokenFlags(Token token, ICollection<TokenFlag> flags)
        {
            // Simple alias to check if flag is a part of the set
            bool CheckFlag(TokenFlag flag)
            {
                return flags.Contains(flag);
            }

            // Make sure token is the correct closure
            if (flags.Any(f => FlagMaskEnclosed.Contains(f)))
            {
                var success = CheckFlag(TokenFlag.FlagEnclosed) == token.Enclosed;
                if (!success) return false; // Not enclosed correctly (e.g. enclosed when we're looking for non-enclosed).
            }

            // Make sure token is the correct category
            if (!flags.Any(f => FlagMaskCategories.Contains(f))) return true;
            var secondarySuccess = false;

            void CheckCategory(TokenFlag fe, TokenFlag fn, TokenCategory c)
            {
                if (secondarySuccess) return;
                var result = CheckFlag(fe) ? token.Category == c : CheckFlag(fn) && token.Category != c;
                secondarySuccess = result;
            }

            CheckCategory(TokenFlag.FlagBracket, TokenFlag.FlagNotBracket, TokenCategory.Bracket);
            CheckCategory(TokenFlag.FlagDelimiter, TokenFlag.FlagNotDelimiter, TokenCategory.Delimiter);
            CheckCategory(TokenFlag.FlagIdentifier, TokenFlag.FlagNotIdentifier, TokenCategory.Identifier);
            CheckCategory(TokenFlag.FlagUnknown, TokenFlag.FlagNotUnknown, TokenCategory.Unknown);
            CheckCategory(TokenFlag.FlagNotValid, TokenFlag.FlagValid, TokenCategory.Invalid);
            return secondarySuccess;
        }

        /// <summary>
        /// Given a list of <c>tokens</c>, searches for any token token that matches the list of <c>flags</c>.
        /// </summary>
        /// <param name="tokens">the list of tokens</param>
        /// <param name="begin">the search starting position.</param>
        /// <param name="end">the search ending position.</param>
        /// <param name="flags">the search flags</param>
        /// <returns>the search result</returns>
        public static int FindToken(List<Token> tokens, int begin, int end, params TokenFlag[] flags)
        {
            return FindTokenBase(tokens, begin, end, i => i < tokens.Count, i => i + 1, flags);
        }

        /// <summary>
        /// Given a list of <c>tokens</c>, searches for the next token in <c>tokens</c> that matches the list of <c>flags</c>.
        /// </summary>
        /// <param name="tokens">the list of tokens</param>
        /// <param name="first">the search starting position.</param>
        /// <param name="flags">the search flags</param>
        /// <returns>the search result</returns>
        public static int FindNextToken(List<Token> tokens, int first, params TokenFlag[] flags)
        {
            return FindTokenBase(tokens, first + 1, tokens.Count, i => i < tokens.Count, i => i + 1, flags);
        }

        /// <summary>
        /// Given a list of <c>tokens</c>, searches for the previous token in <c>tokens</c> that matches the list of <c>flags</c>.
        /// 
        /// 在给定的标记列表中搜索匹配输入的标记符前一个标记
        /// </summary>
        /// <param name="tokens">the list of tokens</param>
        /// <param name="begin">the search starting position. Exclusive of position.Pos</param>
        /// <param name="flags">the search flags</param>
        /// <returns>the search result</returns>
        public static int FindPrevToken(List<Token> tokens, int begin, params TokenFlag[] flags)
        {
            return FindTokenBase(tokens, begin - 1, -1, i => i >= 0, i => i - 1, flags);
        }

        /// <summary>
        /// Given a list of tokens finds the first token that passes <see cref="CheckTokenFlags"/>.
        /// 
        /// 在给定的标记列表中找到第一个通过<see cref="CheckTokenFlags"/>的标记(token)
        /// </summary>
        /// <param name="tokens">the list of the tokens to search</param>
        /// <param name="begin">the start index of the search.</param>
        /// <param name="end">the end index of the search.</param>
        /// <param name="shouldContinue">a function that returns whether or not we should continue searching</param>
        /// <param name="next">a function that returns the next search index</param>
        /// <param name="flags">the flags that each token should be validated against</param>
        /// <returns>the found token</returns>
        private static int FindTokenBase(
          List<Token> tokens,
          int begin,
          int end,
          Func<int, bool> shouldContinue,
          Func<int, int> next,
          params TokenFlag[] flags)
        {
            var find = new List<TokenFlag>();
            find.AddRange(flags);

            for (var i = begin; shouldContinue(i); i = next(i))
            {
                var token = tokens[i];
                if (CheckTokenFlags(token, find))
                {
                    return i;
                }
            }

            return end;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool InListRange(int pos, List<Token> list)
        {
            return -1 < pos && pos < list.Count;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (!(o is Token)) return false;
            var token = (Token)o;
            return Enclosed == token.Enclosed && Category == token.Category && Equals(Content, token.Content);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hashCode = -1776802967;
            hashCode = hashCode * -1521134295 + Category.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Content);
            hashCode = hashCode * -1521134295 + Enclosed.GetHashCode();
            return hashCode;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Token{{category={Category}, content='{Content}', enclosed={Enclosed}}}";
        }
    }
}
