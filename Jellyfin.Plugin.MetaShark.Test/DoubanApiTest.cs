using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Model;
using Jellyfin.Plugin.MetaShark.Providers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class DoubanApiTest
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
        public void TestSearch()
        {
            var keyword = "重返少年时";
            var api = new DoubanApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.SearchAsync(keyword, CancellationToken.None);
                    var str = result.ToJson();
                    TestContext.WriteLine(result.ToJson());
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }


        [TestMethod]
        public void TestGetVideoByBvidAsync()
        {
            var sid = "26654184";

            var api = new DoubanApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetMovieAsync(sid, CancellationToken.None);
                    TestContext.WriteLine(result.ToJson());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetCelebritiesBySidAsync()
        {
            var sid = "26654184";

            var api = new DoubanApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var result = await api.GetCelebritiesBySidAsync(sid, CancellationToken.None);
                    TestContext.WriteLine(result.ToJson());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }
    }
}
