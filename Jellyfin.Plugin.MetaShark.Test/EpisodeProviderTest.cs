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
            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();

            var provider = new EpisodeProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, doubanApi, tmdbApi, omdbApi);
            var indexNumber = provider.GuessEpisodeNumber("[POPGO][Stand_Alone_Complex][05][1080P][BluRay][x264_FLACx2_AC3x1][chs_jpn][D87C36B6].mkv");
            Assert.AreEqual(indexNumber, 5);

            indexNumber = provider.GuessEpisodeNumber("Fullmetal Alchemist Brotherhood.E05.1920X1080");
            Assert.AreEqual(indexNumber, 5);
        }

    }
}
