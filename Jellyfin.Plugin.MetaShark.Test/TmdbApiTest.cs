using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Languages;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class TmdbApiTest
    {
        private TestContext testContextInstance;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                }));


        [TestMethod]
        public void TestGetMovie()
        {
            var api = new TmdbApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetMovieAsync(752, "zh", "zh", CancellationToken.None)
               .ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    TestContext.WriteLine(result.Images.ToJson());
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }


        [TestMethod]
        public void TestGetSeries()
        {
            var api = new TmdbApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetSeriesAsync(13372, "zh", "zh", CancellationToken.None)
               .ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    TestContext.WriteLine(result.Images.ToJson());
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }


        [TestMethod]
        public void TestGetEpisode()
        {
            var api = new TmdbApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetEpisodeAsync(13372, 1, 1, "zh", "zh", CancellationToken.None)
               .ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    TestContext.WriteLine(result.Images.Stills.ToJson());
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }



        [TestMethod]
        public void TestSearch()
        {
            var keyword = "狼与香辛料";
            var api = new TmdbApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.SearchSeriesAsync(keyword, "zh", CancellationToken.None).ConfigureAwait(false);
                    Assert.IsNotNull(result);
                    TestContext.WriteLine(result.ToJson());
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

    }
}
