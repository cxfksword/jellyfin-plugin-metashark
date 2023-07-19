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
    /// 标记(token)的位置
    /// </summary>
    public struct TokenRange
    {
        /// <summary>
        /// 偏移值
        /// </summary>
        public int Offset;
        /// <summary>
        /// Token长度
        /// </summary>
        public int Size;

        /// <summary>
        /// 构造<see cref="TokenRange"/>
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public TokenRange(int offset, int size)
        {
            Offset = offset;
            Size = size;
        }
    }
}
