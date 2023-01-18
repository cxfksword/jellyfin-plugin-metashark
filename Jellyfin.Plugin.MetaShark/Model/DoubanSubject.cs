using MediaBrowser.Model.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Model
{
    public class DoubanSubject
    {
        // "name": "哈利·波特与魔法石",
        public string Name { get; set; }
        // "originalName": "Harry Potter and the Sorcerer's Stone",
        public string OriginalName { get; set; }
        // "rating": "9.1",
        public float Rating { get; set; }
        // "img": "https://img9.doubanio.com/view/photo/s_ratio_poster/public/p2614949805.webp",
        public string Img { get; set; }
        // "sid": "1295038",
        public string Sid { get; set; }
        // "year": "2001",
        public int Year { get; set; }
        // "director": "克里斯·哥伦布",
        public string Director { get; set; }
        // "writer": "史蒂夫·克洛夫斯 / J·K·罗琳",
        public string Writer { get; set; }
        // "actor": "丹尼尔·雷德克里夫 / 艾玛·沃森 / 鲁伯特·格林特 / 艾伦·瑞克曼 / 玛吉·史密斯 / 更多...",
        public string Actor { get; set; }
        // "genre": "奇幻 / 冒险",
        public string Genre { get; set; }
        // 电影/电视剧
        public string Category { get; set; }
        // "site": "www.harrypotter.co.uk",
        public string Site { get; set; }
        // "country": "美国 / 英国",
        public string Country { get; set; }
        // "language": "英语",
        public string Language { get; set; }
        // "screen": "2002-01-26(中国大陆) / 2020-08-14(中国大陆重映) / 2001-11-04(英国首映) / 2001-11-16(美国)",
        public string Screen { get; set; }
        public DateTime? ScreenTime
        {
            get
            {
                if (Screen == null) return null;

                var items = Screen.Split("/");
                if (items.Length >= 0)
                {
                    var item = items[0].Split("(")[0];
                    DateTime result;
                    DateTime.TryParseExact(item, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out result);
                    return result;
                }
                return null;
            }
        }
        // "duration": "152分钟 / 159分钟(加长版)",
        public string Duration { get; set; }
        // "subname": "哈利波特1：神秘的魔法石(港/台) / 哈1 / Harry Potter and the Philosopher's Stone",
        public string Subname { get; set; }
        // "imdb": "tt0241527"
        public string Imdb { get; set; }
        public string Intro { get; set; }

        public List<DoubanCelebrity> Celebrities { get; set; }

        [JsonIgnore]
        public List<DoubanCelebrity> LimitDirectorCelebrities
        {
            get
            {
                // 限制导演最多返回5个
                var limitCelebrities = new List<DoubanCelebrity>();
                if (Celebrities == null || Celebrities.Count == 0)
                {
                    return limitCelebrities;
                }

                limitCelebrities.AddRange(Celebrities.Where(x => x.RoleType == MediaBrowser.Model.Entities.PersonType.Director && !string.IsNullOrEmpty(x.Name)).Take(5));
                limitCelebrities.AddRange(Celebrities.Where(x => x.RoleType != MediaBrowser.Model.Entities.PersonType.Director && !string.IsNullOrEmpty(x.Name)));

                return limitCelebrities;
            }
        }

        [JsonIgnore]
        public string ImgMiddle
        {
            get
            {
                return this.Img.Replace("s_ratio_poster", "m");
            }
        }

        [JsonIgnore]
        public string[] Genres
        {
            get
            {
                return this.Genre.Split("/").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            }
        }



    }

    public class DoubanCelebrity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Img { get; set; }
        public string Role { get; set; }

        public string Intro { get; set; }
        public string Gender { get; set; }
        public string Constellation { get; set; }
        public string Birthdate { get; set; }
        public string Birthplace { get; set; }
        public string Nickname { get; set; }
        public string Imdb { get; set; }
        public string Site { get; set; }

        private string _roleType;
        public string RoleType
        {
            get
            {
                if (string.IsNullOrEmpty(this._roleType))
                {
                    return this.Role.Contains("导演", StringComparison.Ordinal) ? MediaBrowser.Model.Entities.PersonType.Director : MediaBrowser.Model.Entities.PersonType.Actor;
                }

                return this._roleType.Contains("导演", StringComparison.Ordinal) ? MediaBrowser.Model.Entities.PersonType.Director : MediaBrowser.Model.Entities.PersonType.Actor;
            }
            set
            {
                _roleType = value;
            }
        }
    }

    public class DoubanPhoto
    {
        public string Id { get; set; }
        public string Small { get; set; }
        public string Medium { get; set; }
        public string Large { get; set; }
        /// <summary>
        /// 原始图片url，必须带referer访问
        /// </summary>
        public string Raw { get; set; }
        public string Size { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
