namespace Jellyfin.Plugin.MetaShark.Parser
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class BTNamePareser
    {
        private Regex btGroupReg = new Regex(@"\[[a-zA-Z\-]*\]", RegexOptions.Compiled);
        private Regex yearReg = new Regex(@"[12][890][78901234][0-9]", RegexOptions.Compiled);
        private Regex resoReg = new Regex(@"([0-9]{3,4}[pP])|([0-9]{3,4}[iI])|([hH][dD])|(4[kK])|([sS][dD])", RegexOptions.Compiled);
        private Regex codeReg = new Regex(@"([hH]\.[0-9]{3})|([vV][cC]-?1)|([xX][vV][iI][dD])
|([mM][pP][eE][Gg]-?\d)|([fF][lL][aA][Cc])|([aA][pP][eE])|([dD][tT][sS])|([aA][cC]-?\d)|([wW][aA][vV])
|([mM][pP]\d)|([aA][lL][aA][cC])|([aA]{2}[cC])"
    , RegexOptions.Compiled);
        private Regex chineseReg = new Regex(@"[\u4e00-\u9fa5]{1,}", RegexOptions.Compiled);
        private Regex serisReg = new Regex(@"([sS][0-9]{1,2})|([Ss][eE][rR][iI][sS][0-9]{1,2})", RegexOptions.Compiled);
        private Regex episodeReg = new Regex(@"([eE][0-9]{1,3})|([Ee][pP][iI][sS][oO][dD][eE][0-9]{1,3})", RegexOptions.Compiled);

        public class ResourceInfo
        {
            public string? Name { get; set; }
            public string? ChineseName { get; set; }
            public string? EnglishName { get; set; }
            public string? Year { get; set; }
            public string? Resolution { get; set; }
            public string? Seris { get; set; }
            public string? Episode { get; set; }

            public bool isSeris()
            {
                return Seris != null || Episode != null;
            }
        }

        public ResourceInfo Match(string btFileName, ILogger _logger)
        {
            NameTrimmer trimmer = new NameTrimmer();
            var trimmedName = trimmer.trimName(btFileName, _logger);
            var btgroup = GetMatch(trimmedName, btGroupReg);
            var year = GetMatch(trimmedName, yearReg);
            var reso = GetMatch(trimmedName, resoReg);
            var code = GetMatch(trimmedName, codeReg);
            var chinese = GetMatch(trimmedName, chineseReg);
            var seris = GetMatch(trimmedName, serisReg);
            var episode = GetMatch(trimmedName, episodeReg);

            ResourceInfo info = new ResourceInfo();
            info.ChineseName = chinese?.MatchContent;
            info.Year = year?.MatchContent;
            info.Seris = seris?.MatchContent;
            info.Episode = episode?.MatchContent;
            info.Resolution = reso?.MatchContent;
            info.Name = trimmedName;
            info.EnglishName = ReplaceMatch(trimmedName, "", chineseReg).Trim();
            return info;

        }

        private class MatchResult
        {
            public int Index { get; set; }
            public string MatchContent { get; set; }

        }
        private MatchResult? GetMatch(string text, Regex reg)
        {
            var match = reg.Match(text);
            if (match.Success && match.Groups.Count > 0)
            {
                return new MatchResult
                {
                    Index = match.Groups[0].Index,
                    MatchContent = match.Groups[0].Value.Trim(),
                };
            }

            return null;
        }

        private string ReplaceMatch(string text, string replacement, Regex reg)
        {
            return reg.Replace(text, replacement);
        }
    }
}
