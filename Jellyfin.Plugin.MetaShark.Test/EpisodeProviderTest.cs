using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Providers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class EpisodeProviderTest
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
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var imdbApi = new ImdbApi(loggerFactory);

            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();

            Task.Run(async () =>
            {
                var info = new EpisodeInfo()
                {
                    Name = "Spice and Wolf",
                    Path = "/test/Spice and Wolf/S00/[VCB-Studio] Spice and Wolf II [01][Hi444pp_1080p][x264_flac].mkv",
                    MetadataLanguage = "zh",
                    ParentIndexNumber = 0,
                    SeriesProviderIds = new Dictionary<string, string>() { { MetadataProvider.Tmdb.ToString(), "26707" } },
                    IsAutomated = false,
                };
                var provider = new EpisodeProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi, imdbApi);
                var result = await provider.GetMetadata(info, CancellationToken.None);
                Assert.IsNotNull(result);

                var str = result.ToJson();
                Console.WriteLine(result.ToJson());
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestFixParseInfo()
        {
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var imdbApi = new ImdbApi(loggerFactory);

            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();


            var provider = new EpisodeProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi, imdbApi);
            var parseResult = provider.FixParseInfo(new EpisodeInfo() { Path = "/test/[POPGO][Stand_Alone_Complex][05][1080P][BluRay][x264_FLACx2_AC3x1][chs_jpn][D87C36B6].mkv" });
            Assert.AreEqual(parseResult.IndexNumber, 5);

            parseResult = provider.FixParseInfo(new EpisodeInfo() { Path = "/test/Fullmetal Alchemist Brotherhood.E05.1920X1080" });
            Assert.AreEqual(parseResult.IndexNumber, 5);

            parseResult = provider.FixParseInfo(new EpisodeInfo() { Path = "/test/[SAIO-Raws] Neon Genesis Evangelion 05 [BD 1440x1080 HEVC-10bit OPUSx2 ASSx2].mkv" });
            Assert.AreEqual(parseResult.IndexNumber, 5);

            parseResult = provider.FixParseInfo(new EpisodeInfo() { Path = "/test/[Moozzi2] Samurai Champloo [SP03] Battlecry (Opening) PV (BD 1920x1080 x.264 AC3).mkv" });
            Assert.AreEqual(parseResult.IndexNumber, 3);
            Assert.AreEqual(parseResult.ParentIndexNumber, 0);
        }

    }
}
