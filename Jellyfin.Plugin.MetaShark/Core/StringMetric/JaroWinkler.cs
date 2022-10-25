/*
 * The MIT License
 *
 * Copyright 2016 feature[23]
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Linq;
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable LoopCanBeConvertedToQuery

namespace StringMetric
{
    /// The Jaro–Winkler distance metric is designed and best suited for short
    /// strings such as person names, and to detect typos; it is (roughly) a
    /// variation of Damerau-Levenshtein, where the substitution of 2 close
    /// characters is considered less important then the substitution of 2 characters
    /// that a far from each other.
    /// Jaro-Winkler was developed in the area of record linkage (duplicate
    /// detection) (Winkler, 1990). It returns a value in the interval [0.0, 1.0].
    /// The distance is computed as 1 - Jaro-Winkler similarity.
    public class JaroWinkler
    {
        private const double DEFAULT_THRESHOLD = 0.7;
        private const int THREE = 3;
        private const double JW_COEF = 0.1;

        /// <summary>
        /// The current value of the threshold used for adding the Winkler bonus. The default value is 0.7.
        /// </summary>
        private double Threshold { get; }

        /// <summary>
        /// Creates a new instance with default threshold (0.7)
        /// </summary>
        public JaroWinkler()
        {
            Threshold = DEFAULT_THRESHOLD;
        }

        /// <summary>
        /// Creates a new instance with given threshold to determine when Winkler bonus should
        /// be used. Set threshold to a negative value to get the Jaro distance.
        /// </summary>
        /// <param name="threshold"></param>
        public JaroWinkler(double threshold)
        {
            Threshold = threshold;
        }

        /// <summary>
        /// Compute Jaro-Winkler similarity.
        /// </summary>
        /// <param name="s1">The first string to compare.</param>
        /// <param name="s2">The second string to compare.</param>
        /// <returns>The Jaro-Winkler similarity in the range [0, 1]</returns>
        /// <exception cref="ArgumentNullException">If s1 or s2 is null.</exception>
        public double Similarity(string s1, string s2)
        {
            if (s1 == null)
            {
                throw new ArgumentNullException(nameof(s1));
            }

            if (s2 == null)
            {
                throw new ArgumentNullException(nameof(s2));
            }

            if (s1.Equals(s2))
            {
                return 1f;
            }

            int[] mtp = Matches(s1, s2);
            float m = mtp[0];
            if (m == 0)
            {
                return 0f;
            }
            double j = ((m / s1.Length + m / s2.Length + (m - mtp[1]) / m))
                    / THREE;
            double jw = j;

            if (j > Threshold)
            {
                jw = j + Math.Min(JW_COEF, 1.0 / mtp[THREE]) * mtp[2] * (1 - j);
            }
            return jw;
        }

        /// <summary>
        /// Return 1 - similarity.
        /// </summary>
        /// <param name="s1">The first string to compare.</param>
        /// <param name="s2">The second string to compare.</param>
        /// <returns>1 - similarity</returns>
        /// <exception cref="ArgumentNullException">If s1 or s2 is null.</exception>
        public double Distance(string s1, string s2)
            => 1.0 - Similarity(s1, s2);

        private static int[] Matches(string s1, string s2)
        {
            string max, min;
            if (s1.Length > s2.Length)
            {
                max = s1;
                min = s2;
            }
            else
            {
                max = s2;
                min = s1;
            }
            int range = Math.Max(max.Length / 2 - 1, 0);

            //int[] matchIndexes = new int[min.Length];
            //Arrays.fill(matchIndexes, -1);
            int[] match_indexes = Enumerable.Repeat(-1, min.Length).ToArray();

            bool[] match_flags = new bool[max.Length];
            int matches = 0;
            for (int mi = 0; mi < min.Length; mi++)
            {
                char c1 = min[mi];
                for (int xi = Math.Max(mi - range, 0),
                        xn = Math.Min(mi + range + 1, max.Length); xi < xn; xi++)
                {
                    if (!match_flags[xi] && c1 == max[xi])
                    {
                        match_indexes[mi] = xi;
                        match_flags[xi] = true;
                        matches++;
                        break;
                    }
                }
            }
            char[] ms1 = new char[matches];
            char[] ms2 = new char[matches];
            for (int i = 0, si = 0; i < min.Length; i++)
            {
                if (match_indexes[i] != -1)
                {
                    ms1[si] = min[i];
                    si++;
                }
            }
            for (int i = 0, si = 0; i < max.Length; i++)
            {
                if (match_flags[i])
                {
                    ms2[si] = max[i];
                    si++;
                }
            }
            int transpositions = 0;
            for (int mi = 0; mi < ms1.Length; mi++)
            {
                if (ms1[mi] != ms2[mi])
                {
                    transpositions++;
                }
            }
            int prefix = 0;
            for (int mi = 0; mi < min.Length; mi++)
            {
                if (s1[mi] == s2[mi])
                {
                    prefix++;
                }
                else
                {
                    break;
                }
            }
            return new[] { matches, transpositions / 2, prefix, max.Length };
        }
    }
}