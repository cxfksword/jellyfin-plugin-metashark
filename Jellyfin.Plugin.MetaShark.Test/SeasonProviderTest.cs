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
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();

            Task.Run(async () =>
            {
                var provider = new SeasonProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi);
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
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();

            var provider = new SeasonProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi);
            var result = provider.GuessSeasonNumberByFileName("/data/downloads/jellyfin/tv/向往的生活/第2季");
            Assert.AreEqual(result, 2);

            result = provider.GuessSeasonNumberByFileName("/data/downloads/jellyfin/tv/向往的生活 第2季");
            Assert.AreEqual(result, 2);

            result = provider.GuessSeasonNumberByFileName("/data/downloads/jellyfin/tv/向往的生活/第三季");
            Assert.AreEqual(result, 3);

            result = provider.GuessSeasonNumberByFileName("/data/downloads/jellyfin/tv/攻壳机动队Ghost_in_The_Shell_S.A.C._2nd_GIG");
            Assert.AreEqual(result, 2);

            result = provider.GuessSeasonNumberByFileName("/data/downloads/jellyfin/tv/Spice and Wolf/Spice and Wolf 2");
            Assert.AreEqual(result, 2);

            result = provider.GuessSeasonNumberByFileName("/data/downloads/jellyfin/tv/Spice and Wolf/Spice and Wolf 2 test");
            Assert.AreEqual(result, null);
        }

    }
}
