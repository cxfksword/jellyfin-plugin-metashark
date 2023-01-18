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
    public static class RegexExtension
    {
        public static string FirstMatchGroup(this Regex reg, string text, string defalutVal = "")
        {
            var match = reg.Match(text);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }

            return defalutVal;
        }
    }
}