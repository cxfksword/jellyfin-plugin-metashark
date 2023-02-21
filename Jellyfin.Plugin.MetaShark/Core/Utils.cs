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
            switch (str)
            {
                case "一": return 1;
                case "二": return 2;
                case "三": return 3;
                case "四": return 4;
                case "五": return 5;
                case "六": return 6;
                case "七": return 7;
                case "八": return 8;
                case "九": return 9;
                case "零": return 0;
                default: return null;
            }
        }
    }
}
