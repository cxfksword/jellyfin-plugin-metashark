using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Core
{
    public static class Utils
    {
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        /// <summary>
        /// 转换数字
        /// </summary>
        public static int? ChineseNumberToInt(string str)
        {
            if (string.IsNullOrEmpty(str)) return null;

            var chineseNumberMap = new Dictionary<Char, Char>() {
                {'一', '1'},
                {'二', '2'},
                {'三', '3'},
                {'四', '4'},
                {'五', '5'},
                {'六', '6'},
                {'七', '7'},
                {'八', '8'},
                {'九', '9'},
                {'零', '0'},
            };

            var numberArr = str.ToCharArray().Select(x => chineseNumberMap.ContainsKey(x) ? chineseNumberMap[x] : x).ToArray();
            var newNumberStr = new string(numberArr);
            if (int.TryParse(new string(numberArr), out var number))
            {
                return number;
            }

            return null;
        }
    }
}
