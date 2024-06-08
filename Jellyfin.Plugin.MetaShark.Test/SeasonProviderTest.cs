using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Providers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
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
    public class SeasonProviderTest
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
            var info = new SeasonInfo() { Name = "第 18 季", IndexNumber = 18, SeriesProviderIds = new Dictionary<string, string>() { { BaseProvider.DoubanProviderId, "2059529" }, { MetadataProvider.Tmdb.ToString(), "34860" } } };
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var imdbApi = new ImdbApi(loggerFactory);

            Task.Run(async () =>
            {
                var provider = new SeasonProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi, imdbApi);
                var result = await provider.GetMetadata(info, CancellationToken.None);
                Assert.IsNotNull(result);

                var str = result.ToJson();
                Console.WriteLine(result.ToJson());
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGuessSeasonNumberByFileName()
        {
            var info = new SeasonInfo() { };
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var imdbApi = new ImdbApi(loggerFactory);

            var provider = new SeasonProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi, imdbApi);

            var result = provider.GuessSeasonNumberByDirectoryName("/data/downloads/jellyfin/tv/冰与火之歌S01-S08.Game.of.Thrones.1080p.Blu-ray.x265.10bit.AC3/冰与火之歌S2.列王的纷争.2012.1080p.Blu-ray.x265.10bit.AC3");
            Assert.AreEqual(result, 2);

            result = provider.GuessSeasonNumberByDirectoryName("/data/downloads/jellyfin/tv/向往的生活/第2季");
            Assert.AreEqual(result, 2);

            result = provider.GuessSeasonNumberByDirectoryName("/data/downloads/jellyfin/tv/向往的生活 第2季");
            Assert.AreEqual(result, 2);

            result = provider.GuessSeasonNumberByDirectoryName("/data/downloads/jellyfin/tv/向往的生活/第三季");
            Assert.AreEqual(result, 3);

            result = provider.GuessSeasonNumberByDirectoryName("/data/downloads/jellyfin/tv/攻壳机动队Ghost_in_The_Shell_S.A.C._2nd_GIG");
            Assert.AreEqual(result, 2);

            // result = provider.GuessSeasonNumberByDirectoryName("/data/downloads/jellyfin/tv/Spice and Wolf/Spice and Wolf 2");
            // Assert.AreEqual(result, 2);

            result = provider.GuessSeasonNumberByDirectoryName("/data/downloads/jellyfin/tv/Spice and Wolf/Spice and Wolf 2 test");
            Assert.AreEqual(result, null);

            result = provider.GuessSeasonNumberByDirectoryName("/data/downloads/jellyfin/tv/[BDrip] Made in Abyss S02 [7鲁ACG x Sakurato]");
            Assert.AreEqual(result, 2);
        }

        [TestMethod]
        public void TestGuestDoubanSeasonByYearAsync()
        {
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var imdbApi = new ImdbApi(loggerFactory);

            Task.Run(async () =>
            {
                var provider = new SeasonProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi, imdbApi);
                var result = await provider.GuestDoubanSeasonByYearAsync("机动战士高达0083 星尘的回忆", 1991, null, CancellationToken.None);
                Assert.AreEqual(result, "1766564");
            }).GetAwaiter().GetResult();
        }

    }
}
