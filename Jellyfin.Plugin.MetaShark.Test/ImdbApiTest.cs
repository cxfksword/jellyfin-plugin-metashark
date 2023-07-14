using System.Reflection;
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
    public class ImdbApiTest
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
        public void TestCheckPersonNewImdbID()
        {
            var api = new ImdbApi(loggerFactory);

            Task.Run(async () =>
            {
                try
                {
                    var id = "nm1123737";
                    var result = await api.CheckPersonNewIDAsync(id, CancellationToken.None);
                    Assert.AreEqual("nm0170924", result);

                    id = "nm0170924";
                    result = await api.CheckPersonNewIDAsync(id, CancellationToken.None);
                    Assert.AreEqual(null, result);
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine(ex.Message);
                }
            }).GetAwaiter().GetResult();
        }


    }
}
