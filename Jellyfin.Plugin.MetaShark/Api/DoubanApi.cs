using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.MetaShark.Model;
using System.Threading;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Common.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Net;
using Jellyfin.Plugin.MetaShark.Api.Http;
using System.Web;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using Microsoft.Extensions.Caching.Memory;
using Jellyfin.Plugin.MetaShark.Providers;
using AngleSharp;
using System.Net.WebSockets;
using Jellyfin.Data.Entities.Libraries;
using AngleSharp.Dom;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.MetaShark.Core;
using System.Data;
using TMDbLib.Objects.Movies;
using System.Xml.Linq;

namespace Jellyfin.Plugin.MetaShark.Api
{
    public class DoubanApi : IDisposable
    {
        const string HTTP_USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36 Edg/93.0.961.44";
        private readonly ILogger<DoubanApi> _logger;
        private HttpClient httpClient;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private readonly IMemoryCache _memoryCache;
        private static readonly object _lock = new object();
        private DateTime lastRequestTime = DateTime.Now.AddDays(-1);

        Regex regId = new Regex(@"/(\d+?)/", RegexOptions.Compiled);
        Regex regSid = new Regex(@"sid: (\d+?),", RegexOptions.Compiled);
        Regex regCat = new Regex(@"\[(.+?)\]", RegexOptions.Compiled);
        Regex regYear = new Regex(@"(\d{4})", RegexOptions.Compiled);
        Regex regDirector = new Regex(@"导演: (.+?)\n", RegexOptions.Compiled);
        Regex regWriter = new Regex(@"编剧: (.+?)\n", RegexOptions.Compiled);
        Regex regActor = new Regex(@"主演: (.+?)\n", RegexOptions.Compiled);
        Regex regGenre = new Regex(@"类型: (.+?)\n", RegexOptions.Compiled);
        Regex regCountry = new Regex(@"制片国家/地区: (.+?)\n", RegexOptions.Compiled);
        Regex regLanguage = new Regex(@"语言: (.+?)\n", RegexOptions.Compiled);
        Regex regDuration = new Regex(@"片长: (.+?)\n", RegexOptions.Compiled);
        Regex regScreen = new Regex(@"上映日期: (.+?)\n", RegexOptions.Compiled);
        Regex regSubname = new Regex(@"又名: (.+?)\n", RegexOptions.Compiled);
        Regex regImdb = new Regex(@"IMDb: (.+?)$", RegexOptions.Compiled);
        Regex regSite = new Regex(@"官方网站: (.+?)\n", RegexOptions.Compiled);
        Regex regNameMath = new Regex(@"(.+第\w季|[\w\uff1a\uff01\uff0c\u00b7]+)\s*(.*)", RegexOptions.Compiled);
        Regex regRole = new Regex(@"\([饰|配] (.+?)\)", RegexOptions.Compiled);
        Regex regBackgroundImage = new Regex(@"url\((.+?)\)", RegexOptions.Compiled);
        Regex regGender = new Regex(@"性别: \n(.+?)\n", RegexOptions.Compiled);
        Regex regConstellation = new Regex(@"星座: \n(.+?)\n", RegexOptions.Compiled);
        Regex regBirthdate = new Regex(@"出生日期: \n(.+?)\n", RegexOptions.Compiled);
        Regex regLifedate = new Regex(@"生卒日期: \n(.+?) 至", RegexOptions.Compiled);
        Regex regBirthplace = new Regex(@"出生地: \n(.+?)\n", RegexOptions.Compiled);
        Regex regCelebrityRole = new Regex(@"职业: \n(.+?)\n", RegexOptions.Compiled);
        Regex regNickname = new Regex(@"更多外文名: \n(.+?)\n", RegexOptions.Compiled);
        Regex regFamily = new Regex(@"家庭成员: \n(.+?)\n", RegexOptions.Compiled);
        Regex regCelebrityImdb = new Regex(@"imdb编号: \n(.+?)\n", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubanApi"/> class.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public DoubanApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DoubanApi>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            var handler = new HttpClientHandlerEx();
            this.SetDoubanCookie(handler.CookieContainer);
            httpClient = new HttpClient(handler, true);
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            httpClient.DefaultRequestHeaders.Add("user-agent", HTTP_USER_AGENT);
            httpClient.DefaultRequestHeaders.Add("Origin", "https://movie.douban.com");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://movie.douban.com/");
        }



        private void SetDoubanCookie(CookieContainer cookieContainer)
        {
            var configCookie = Plugin.Instance?.Configuration.DoubanCookies.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(configCookie))
            {
                return;
            }

            var uri = new Uri("https://douban.com/");
            var arr = configCookie.Split(';');
            foreach (var str in arr)
            {
                var cookieArr = str.Split('=');
                if (cookieArr.Length != 2)
                {
                    continue;
                }

                var key = cookieArr[0].Trim();
                var value = cookieArr[1].Trim();
                try
                {
                    cookieContainer.Add(new Cookie(key, value, "/", ".douban.com"));
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, ex.Message);
                }
            }
        }

        public async Task<List<DoubanSubject>> SearchAsync(string keyword, CancellationToken cancellationToken)
        {
            var list = new List<DoubanSubject>();
            if (string.IsNullOrEmpty(keyword))
            {
                return list;
            }

            var cacheKey = $"search_{keyword}";
            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            List<DoubanSubject> searchResult;
            if (_memoryCache.TryGetValue<List<DoubanSubject>>(cacheKey, out searchResult))
            {
                return searchResult;
            }


            keyword = HttpUtility.UrlEncode(keyword);
            var url = $"https://www.douban.com/search?cat=1002&q={keyword}";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return list;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var context = BrowsingContext.New();
            var doc = await context.OpenAsync(req => req.Content(body), cancellationToken).ConfigureAwait(false);
            var movieElements = doc.QuerySelectorAll("div.result-list .result");

            foreach (var movieElement in movieElements)
            {

                var rating = movieElement.GetText("div.rating-info>.rating_nums") ?? "0";
                var img = movieElement.GetAttr("a.nbg>img", "src") ?? string.Empty;
                var oncick = movieElement.GetAttr("div.title a", "onclick") ?? string.Empty;
                var sid = oncick.GetMatchGroup(this.regSid);
                var name = movieElement.GetText("div.title a") ?? string.Empty;
                var titleStr = movieElement.GetText("div.title>h3>span") ?? string.Empty;
                var cat = titleStr.GetMatchGroup(this.regCat);
                var subjectStr = movieElement.GetText("div.rating-info>.subject-cast") ?? string.Empty;
                var year = subjectStr.GetMatchGroup(this.regYear);

                if (cat != "电影" && cat != "电视剧")
                {
                    continue;
                }

                var movie = new DoubanSubject();
                movie.Sid = sid;
                movie.Name = name;
                movie.Genre = cat;
                movie.Img = img;
                movie.Rating = rating.ToFloat();
                movie.Year = year.ToInt();
                list.Add(movie);
            }


            _memoryCache.Set<List<DoubanSubject>>(cacheKey, list, expiredOption);
            return list;
        }

        public async Task<DoubanSubject?> GetMovieAsync(string sid, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sid))
            {
                return null;
            }

            var cacheKey = $"movie_{sid}";
            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            DoubanSubject? movie;
            if (_memoryCache.TryGetValue<DoubanSubject?>(cacheKey, out movie))
            {
                return movie;
            }

            var url = $"https://movie.douban.com/subject/{sid}/";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            movie = new DoubanSubject();
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var context = BrowsingContext.New();
            var doc = await context.OpenAsync(req => req.Content(body), cancellationToken).ConfigureAwait(false);
            var contentNode = doc.QuerySelector("#content");
            if (contentNode != null)
            {
                var nameStr = contentNode.GetText("h1>span:first-child");
                var match = this.regNameMath.Match(nameStr);
                var name = string.Empty;
                var orginalName = string.Empty;
                if (match.Success && match.Groups.Count == 3)
                {
                    name = match.Groups[1].Value;
                    orginalName = match.Groups[2].Value;
                }
                var yearStr = contentNode.GetText("h1>span.year") ?? string.Empty;
                var year = yearStr.GetMatchGroup(this.regYear);
                var rating = contentNode.GetText("div.rating_self strong.rating_num") ?? "0";
                var img = contentNode.GetAttr("a.nbgnbg>img", "src") ?? string.Empty;
                var intro = contentNode.GetText("div.indent>span") ?? string.Empty;
                intro = intro.Replace("©豆瓣", string.Empty);

                var info = contentNode.GetText("#info") ?? string.Empty;
                var director = info.GetMatchGroup(this.regDirector);
                var writer = info.GetMatchGroup(this.regWriter);
                var actor = info.GetMatchGroup(this.regActor);
                var genre = info.GetMatchGroup(this.regGenre);
                var country = info.GetMatchGroup(this.regCountry);
                var language = info.GetMatchGroup(this.regLanguage);
                var duration = info.GetMatchGroup(this.regDuration);
                var screen = info.GetMatchGroup(this.regScreen);
                var subname = info.GetMatchGroup(this.regSubname);
                var imdb = info.GetMatchGroup(this.regImdb);
                var site = info.GetMatchGroup(this.regSite);

                movie.Sid = sid;
                movie.Name = name;
                movie.OriginalName = orginalName;
                movie.Year = year.ToInt();
                movie.Rating = rating.ToFloat();
                movie.Img = img;
                movie.Intro = intro;
                movie.Subname = subname;
                movie.Director = director;
                movie.Genre = genre;
                movie.Country = country;
                movie.Language = language;
                movie.Duration = duration;
                movie.Screen = screen;
                movie.Site = site;
                movie.Actor = actor;
                movie.Writer = writer;
                movie.Imdb = imdb;

                movie.Celebrities = new List<DoubanCelebrity>();
                var celebrityNodes = contentNode.QuerySelectorAll("#celebrities li.celebrity");
                foreach (var node in celebrityNodes)
                {
                    var celebrityIdStr = node.GetAttr("div.info a.name", "href") ?? string.Empty;
                    var celebrityId = celebrityIdStr.GetMatchGroup(this.regId);
                    var celebrityImgStr = node.GetAttr("div.avatar", "style") ?? string.Empty;
                    var celebrityImg = celebrityImgStr.GetMatchGroup(this.regBackgroundImage);
                    var celebrityName = node.GetText("div.info a.name") ?? string.Empty;
                    var celebrityRole = node.GetText("div.info span.role") ?? string.Empty;
                    var celebrityRoleType = string.Empty;

                    var celebrity = new DoubanCelebrity();
                    celebrity.Id = celebrityId;
                    celebrity.Name = celebrityName;
                    celebrity.Role = celebrityRole;
                    celebrity.RoleType = celebrityRoleType;
                    celebrity.Img = celebrityImg;
                    movie.Celebrities.Add(celebrity);
                }
                _memoryCache.Set<DoubanSubject?>(cacheKey, movie, expiredOption);
                return movie;
            }


            _memoryCache.Set<DoubanSubject?>(cacheKey, null, expiredOption);
            return null;
        }

        public async Task<List<DoubanCelebrity>> GetCelebritiesBySidAsync(string sid, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sid))
            {
                return new List<DoubanCelebrity>();
            }

            var cacheKey = $"celebrities_{sid}";
            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            List<DoubanCelebrity> celebrities;
            if (this._memoryCache.TryGetValue(cacheKey, out celebrities))
            {
                return celebrities;
            }

            var list = new List<DoubanCelebrity>();
            var url = $"https://movie.douban.com/subject/{sid}/celebrities";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new List<DoubanCelebrity>();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var context = BrowsingContext.New();
            var doc = await context.OpenAsync(req => req.Content(body), cancellationToken).ConfigureAwait(false);
            var celebrityElements = doc.QuerySelectorAll("#content ul.celebrities-list li.celebrity");

            foreach (var node in celebrityElements)
            {

                var celebrityIdStr = node.GetAttr("div.info a.name", "href") ?? string.Empty;
                var celebrityId = celebrityIdStr.GetMatchGroup(this.regId);
                var celebrityImgStr = node.GetAttr("div.avatar", "style") ?? string.Empty;
                var celebrityImg = celebrityImgStr.GetMatchGroup(this.regBackgroundImage);
                var celebrityNameStr = node.GetText("div.info a.name") ?? string.Empty;
                var arr = celebrityNameStr.Split(" ");
                var celebrityName = arr.Length > 1 ? arr[0] : string.Empty;
                var celebrityRoleStr = node.GetText("div.info span.role") ?? string.Empty;
                var celebrityRole = celebrityRoleStr.GetMatchGroup(this.regRole);
                var arrRole = celebrityRoleStr.Split(" ");
                var celebrityRoleType = arrRole.Length > 1 ? arrRole[0] : string.Empty;
                if (string.IsNullOrEmpty(celebrityRole))
                {
                    celebrityRole = celebrityRoleType;
                }

                if (celebrityRoleType != "导演" && celebrityRoleType != "配音" && celebrityRoleType != "演员")
                {
                    continue;
                }

                var celebrity = new DoubanCelebrity();
                celebrity.Id = celebrityId;
                celebrity.Name = celebrityName;
                celebrity.Role = celebrityRole;
                celebrity.RoleType = celebrityRoleType;
                celebrity.Img = celebrityImg;

                list.Add(celebrity);
            }

            _memoryCache.Set<List<DoubanCelebrity>>(cacheKey, list, expiredOption);
            return list;
        }


        public async Task<DoubanCelebrity?> GetCelebrityAsync(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            var cacheKey = $"celebrity_{id}";
            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            DoubanCelebrity? celebrity;
            if (_memoryCache.TryGetValue<DoubanCelebrity?>(cacheKey, out celebrity))
            {
                return celebrity;
            }

            var url = $"https://movie.douban.com/celebrity/{id}/";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            celebrity = new DoubanCelebrity();
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var context = BrowsingContext.New();
            var doc = await context.OpenAsync(req => req.Content(body), cancellationToken).ConfigureAwait(false);
            var contentNode = doc.QuerySelector("#content");
            if (contentNode != null)
            {
                var img = contentNode.GetAttr("#headline .nbg img", "src") ?? string.Empty;
                var name = contentNode.GetText("h1") ?? string.Empty;
                var intro = contentNode.GetText("#intro span.all") ?? string.Empty;
                if (string.IsNullOrEmpty(intro))
                {
                    intro = contentNode.GetText("#intro div.bd") ?? string.Empty;
                }
                var info = contentNode.GetText("div.info") ?? string.Empty;
                var gender = info.GetMatchGroup(this.regGender);
                var constellation = info.GetMatchGroup(this.regConstellation);
                var birthdate = info.GetMatchGroup(this.regBirthdate);
                var lifedate = info.GetMatchGroup(this.regLifedate);
                if (string.IsNullOrEmpty(birthdate))
                {
                    birthdate = lifedate;
                }

                var birthplace = info.GetMatchGroup(this.regBirthplace);
                var role = info.GetMatchGroup(this.regCelebrityRole);
                var nickname = info.GetMatchGroup(this.regNickname);
                var family = info.GetMatchGroup(this.regFamily);
                var imdb = info.GetMatchGroup(this.regCelebrityImdb);

                celebrity.Img = img;
                celebrity.Gender = gender;
                celebrity.Birthdate = birthdate;
                celebrity.Nickname = nickname;
                celebrity.Imdb = imdb;
                celebrity.Birthplace = birthplace;
                celebrity.Name = name;
                celebrity.Intro = intro;
                celebrity.Constellation = constellation;
                celebrity.Role = role;
                _memoryCache.Set<DoubanCelebrity?>(cacheKey, celebrity, expiredOption);
                return celebrity;
            }


            _memoryCache.Set<DoubanCelebrity?>(cacheKey, null, expiredOption);
            return null;
        }

        public async Task<List<DoubanPhoto>> GetWallpaperBySidAsync(string sid, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sid))
            {
                return new List<DoubanPhoto>();
            }

            var cacheKey = $"photo_{sid}";
            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            List<DoubanPhoto> photos;
            if (_memoryCache.TryGetValue<List<DoubanPhoto>>(cacheKey, out photos))
            {
                return photos;
            }

            var list = new List<DoubanPhoto>();
            var url = $"https://movie.douban.com/subject/{sid}/photos?type=W&start=0&sortby=size&size=a&subtype=a";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new List<DoubanPhoto>();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var context = BrowsingContext.New();
            var doc = await context.OpenAsync(req => req.Content(body), cancellationToken).ConfigureAwait(false);
            var elements = doc.QuerySelectorAll(".poster-col3>li");

            foreach (var node in elements)
            {

                var id = node.GetAttribute("data-id") ?? string.Empty;
                var small = $"https://img2.doubanio.com/view/photo/s/public/p{id}.jpg";
                var medium = $"https://img2.doubanio.com/view/photo/m/public/p{id}.jpg";
                var large = $"https://img2.doubanio.com/view/photo/l/public/p{id}.jpg";
                var size = node.GetText("div.prop") ?? string.Empty;
                var width = string.Empty;
                var height = string.Empty;
                if (!string.IsNullOrEmpty(size))
                {
                    var arr = size.Split('x');
                    if (arr.Length == 2)
                    {
                        width = arr[0];
                        height = arr[1];
                    }
                }

                var photo = new DoubanPhoto();
                photo.Id = id;
                photo.Size = size;
                photo.Small = small;
                photo.Medium = medium;
                photo.Large = large;
                photo.Width = width.ToInt();
                photo.Height = height.ToInt();

                list.Add(photo);
            }

            _memoryCache.Set<List<DoubanPhoto>>(cacheKey, list, expiredOption);
            return list;
        }


        protected void LimitRequestFrequently()
        {
            lock (_lock)
            {
                var ts = DateTime.Now - lastRequestTime;
                var diff = (int)(200 - ts.TotalMilliseconds);
                if (diff > 0)
                {
                    Thread.Sleep(diff);
                }
                lastRequestTime = DateTime.Now;
            }
        }

        private string? GetText(IElement el, string css)
        {
            var node = el.QuerySelector(css);
            if (node != null)
            {
                return node.Text();
            }

            return null;
        }

        private string? GetAttr(IElement el, string css, string attr)
        {
            var node = el.QuerySelector(css);
            if (node != null)
            {
                return node.GetAttribute(attr);
            }

            return null;
        }

        private string Match(string text, Regex reg)
        {
            var match = reg.Match(text);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memoryCache.Dispose();
            }
        }
    }
}
