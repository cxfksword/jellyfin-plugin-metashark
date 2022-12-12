using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StringMetric;

namespace Jellyfin.Plugin.MetaShark.Core
{
    public static class StringExtension
    {
        public static long ToLong(this string s)
        {
            long val;
            if (long.TryParse(s, out val))
            {
                return val;
            }

            return 0;
        }

        public static int ToInt(this string s)
        {
            int val;
            if (int.TryParse(s, out val))
            {
                return val;
            }

            return 0;
        }

        public static float ToFloat(this string s)
        {
            float val;
            if (float.TryParse(s, out val))
            {
                return val;
            }

            return 0.0f;
        }

        public static bool IsChinese(this string s)
        {
            Regex chineseReg = new Regex(@"[\u4e00-\u9fa5]{1,}", RegexOptions.Compiled);
            return chineseReg.IsMatch(s.Replace(" ", string.Empty).Trim());
        }

        public static double Distance(this string s1, string s2)
        {
            var jw = new JaroWinkler();

            return jw.Similarity(s1, s2);
        }

        public static string GetMatchGroup(this string text, Regex reg)
        {
            var match = reg.Match(text);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }

            return string.Empty;
        }
    }
}
