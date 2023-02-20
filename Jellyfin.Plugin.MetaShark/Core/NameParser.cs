using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetaShark.Model;

namespace Jellyfin.Plugin.MetaShark.Core
{
    public static class NameParser
    {
        private static readonly Regex yearReg = new Regex(@"[12][890][0-9][0-9]", RegexOptions.Compiled);
        private static readonly Regex seasonSuffixReg = new Regex(@"[ .]S\d{1,2}$", RegexOptions.Compiled);

        private static readonly Regex unusedReg = new Regex(@"\[.+?\]|\(.+?\)|【.+?】", RegexOptions.Compiled);

        private static readonly Regex extrasReg = new Regex(@"\[(OP|ED|PV|CM|Menu|NCED|NCOP|Drama|PreView)[0-9_]*?\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static ParseNameResult Parse(string fileName, bool isTvSeries = false)
        {
            var parseResult = new ParseNameResult();
            var anitomyResult = AnitomySharp.AnitomySharp.Parse(fileName);
            foreach (var item in anitomyResult)
            {
                switch (item.Category)
                {
                    case AnitomySharp.Element.ElementCategory.ElementAnimeTitle:
                        // 处理混合中英文的标题，中文一般在最前面，如V字仇杀队.V.for.Vendetta
                        char[] delimiters = { ' ', '.' };
                        var firstSpaceIndex = item.Value.IndexOfAny(delimiters);
                        if (firstSpaceIndex > 0)
                        {
                            var firstString = item.Value.Substring(0, firstSpaceIndex);
                            var lastString = item.Value.Substring(firstSpaceIndex + 1);
                            if (firstString.HasChinese() && !lastString.HasChinese())
                            {
                                parseResult.ChineseName = CleanName(firstString);
                                parseResult.Name = CleanName(lastString);
                            }
                            else
                            {
                                parseResult.Name = CleanName(item.Value);
                            }
                        }
                        else
                        {
                            parseResult.Name = CleanName(item.Value);
                        }
                        break;
                    case AnitomySharp.Element.ElementCategory.ElementEpisodeNumber:
                        var year = ParseYear(item.Value);
                        if (year > 0)
                        {
                            parseResult.Year = year;
                        }
                        else
                        {
                            var indexNumber = item.Value.ToInt();
                            if (indexNumber > 0)
                            {
                                parseResult.IndexNumber = item.Value.ToInt();
                            }
                        }
                        break;
                    case AnitomySharp.Element.ElementCategory.ElementAnimeType:
                        if (item.Value == "SP")
                        {
                            parseResult.IsSpecial = true;
                        }
                        break;
                    case AnitomySharp.Element.ElementCategory.ElementAnimeYear:
                        parseResult.Year = item.Value.ToInt();
                        break;
                    default:
                        break;
                }
            }

            // 假如Anitomy解析不到year，尝试使用jellyfin默认parser，看能不能解析成功
            if (parseResult.Year == null && !IsAnime(fileName))
            {
                var nativeParseResult = ParseMovie(fileName);
                if (nativeParseResult.Year != null)
                {
                    parseResult = nativeParseResult;
                }
            }

            // 解析不到title时，使用默认名
            if (string.IsNullOrEmpty(parseResult.Name))
            {
                parseResult.Name = fileName;
            }

            return parseResult;
        }

        private static string CleanName(string name)
        {
            // 电视剧名称后紧跟季信息时，会附加到名称中，需要去掉
            name = seasonSuffixReg.Replace(name, string.Empty);

            // 删除多余的[]/()附加信息
            name = unusedReg.Replace(name, string.Empty);

            return name.Replace(".", " ").Trim();
        }

        // emby原始电影解析
        public static ParseNameResult ParseMovie(string fileName)
        {
            var parseResult = new ParseNameResult();
            var nameOptions = new Emby.Naming.Common.NamingOptions();
            var result = Emby.Naming.Video.VideoResolver.CleanDateTime(fileName, nameOptions);
            if (Emby.Naming.Video.VideoResolver.TryCleanString(result.Name, nameOptions, out var cleanName))
            {
                parseResult.Name = CleanName(cleanName);
                parseResult.Year = result.Year;
            }
            else
            {
                parseResult.Name = CleanName(result.Name);
                parseResult.Year = result.Year;
            }
            return parseResult;
        }


        private static int ParseYear(string val)
        {
            var match = yearReg.Match(val);
            if (match.Success && match.Groups.Count > 0)
            {
                return match.Groups[0].Value.ToInt();
            }

            return 0;
        }

        public static bool IsSpecial(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
            if (IsAnime(fileName))
            {
                if (fileName.Contains("[SP]"))
                {
                    return true;
                }

                string folder = Path.GetFileName(Path.GetDirectoryName(path)) ?? string.Empty;
                return folder == "SPs" && !extrasReg.IsMatch(fileName);
            }

            return false;
        }

        public static bool IsExtra(string name)
        {
            return IsAnime(name) && extrasReg.IsMatch(name);
        }



        // 判断是否为动漫
        // https://github.com/jxxghp/nas-tools/blob/f549c924558fd49e183333285bc6a804af1a2cb7/app/media/meta/metainfo.py#L51
        public static bool IsAnime(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (Regex.Match(name, @"【[+0-9XVPI-]+】\s*【", RegexOptions.IgnoreCase).Success)
            {
                return true;
            }

            if (Regex.Match(name, @"\s+-\s+[\dv]{1,4}\s+", RegexOptions.IgnoreCase).Success)
            {
                return true;
            }

            if (Regex.Match(name, @"S\d{2}\s*-\s*S\d{2}|S\d{2}|\s+S\d{1,2}|EP?\d{2,4}\s*-\s*EP?\d{2,4}|EP?\d{2,4}|\s+EP?\d{1,4}", RegexOptions.IgnoreCase).Success)
            {
                return true;
            }

            if (Regex.Match(name, @"\[[+0-9XVPI-]+]\s*\[", RegexOptions.IgnoreCase).Success)
            {
                return true;
            }

            if (Regex.Match(name, @"\[.+\].*?\[.+?\]", RegexOptions.IgnoreCase).Success)
            {
                return true;
            }

            return false;
        }
    }
}