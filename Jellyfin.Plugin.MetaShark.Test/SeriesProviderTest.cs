using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Providers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class SeriesProviderTest
    {

        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                }));


        [TestMethod]
        public void TestGetMetadata()
        {
            var info = new SeriesInfo() { Name = "天下长河" };
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var imdbApi = new ImdbApi(loggerFactory);

            ExternalApiTestHelper.RunOrInconclusive(async () =>
            {
                var provider = new SeriesProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi, imdbApi);
                var result = await provider.GetMetadata(info, CancellationToken.None);
                Assert.IsNotNull(result);

                Console.WriteLine(result.ToJson());
            }, "www.douban.com", "movie.douban.com", "api.tmdb.org");
        }

        [TestMethod]
        public void TestGetAnimeMetadata()
        {
            var info = new SeriesInfo() { Name = "命运-冠位嘉年华" };
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var imdbApi = new ImdbApi(loggerFactory);

            ExternalApiTestHelper.RunOrInconclusive(async () =>
            {
                var provider = new SeriesProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi, imdbApi);
                var result = await provider.GetMetadata(info, CancellationToken.None);
                ExternalApiTestHelper.AssertNotNullOrInconclusive(result.Item, "www.douban.com", "Series metadata should not be null");
                Assert.AreEqual(result.Item.Name, "命运/冠位指定嘉年华 公元2020奥林匹亚英灵限界大祭");
                Assert.AreEqual(result.Item.OriginalTitle, "Fate/Grand Carnival");

                Console.WriteLine(result.ToJson());
            }, "www.douban.com", "movie.douban.com", "api.tmdb.org");
        }

        [TestMethod]
        public void TestGetMetadataWithTmdbIdAttr()
        {
            // 目录名显式指定的[tmdbid-xxx]应优先于imdb反查结果，不应被覆盖（issue #127）
            // 狐妖小红娘在TMDB存在两个条目，豆瓣imdb反查得到的是75787，目录名指定的是按篇分季的298533
            var info = new SeriesInfo() { Name = "狐妖小红娘" };
            info.Path = "/tmp/狐妖小红娘 (2015) [tmdbid-298533]";
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var imdbApi = new ImdbApi(loggerFactory);

            ExternalApiTestHelper.RunOrInconclusive(async () =>
            {
                var provider = new SeriesProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi, imdbApi);
                var result = await provider.GetMetadata(info, CancellationToken.None);
                ExternalApiTestHelper.AssertNotNullOrInconclusive(result.Item, "www.douban.com", "Series metadata should not be null");
                Assert.AreEqual("298533", result.Item.ProviderIds[MediaBrowser.Model.Entities.MetadataProvider.Tmdb.ToString()]);

                Console.WriteLine(result.ToJson());
            }, "www.douban.com", "movie.douban.com", "api.tmdb.org", "www.omdbapi.com");
        }

    }
}
