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
        public void TestGuessEpisodeNumber()
        {
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();

            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);

            var provider = new EpisodeProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi);
            var guessInfo = provider.GuessEpisodeNumber("[POPGO][Stand_Alone_Complex][05][1080P][BluRay][x264_FLACx2_AC3x1][chs_jpn][D87C36B6].mkv");
            Assert.AreEqual(guessInfo.episodeNumber, 5);

            guessInfo = provider.GuessEpisodeNumber("Fullmetal Alchemist Brotherhood.E05.1920X1080");
            Assert.AreEqual(guessInfo.episodeNumber, 5);

            guessInfo = provider.GuessEpisodeNumber("[SAIO-Raws] Neon Genesis Evangelion 05 [BD 1440x1080 HEVC-10bit OPUSx2 ASSx2].mkv");
            Assert.AreEqual(guessInfo.episodeNumber, 5);

            guessInfo = provider.GuessEpisodeNumber("[Moozzi2] Samurai Champloo [SP03] Battlecry (Opening) PV (BD 1920x1080 x.264 AC3).mkv");
            Assert.AreEqual(guessInfo.episodeNumber, 3);
            Assert.AreEqual(guessInfo.seasonNumber, 0);
        }

    }
}
