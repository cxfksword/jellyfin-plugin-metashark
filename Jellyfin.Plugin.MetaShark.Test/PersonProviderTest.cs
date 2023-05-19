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
    public class PersonProviderTest
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
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();

            Task.Run(async () =>
            {
                var info = new PersonLookupInfo() { ProviderIds = new Dictionary<string, string>() { { BaseProvider.DoubanProviderId, "1016771" } } };
                var provider = new PersonProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi);
                var result = await provider.GetMetadata(info, CancellationToken.None);
                Assert.IsNotNull(result);

                var str = result.ToJson();
                Console.WriteLine(result.ToJson());
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void TestGetMetadataByTmdb()
        {

            var doubanApi = new DoubanApi(loggerFactory);
            var tmdbApi = new TmdbApi(loggerFactory);
            var omdbApi = new OmdbApi(loggerFactory);
            var httpClientFactory = new DefaultHttpClientFactory();
            var libraryManagerStub = new Mock<ILibraryManager>();
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();

            Task.Run(async () =>
            {
                var info = new PersonLookupInfo() { ProviderIds = new Dictionary<string, string>() { { MetadataProvider.Tmdb.ToString(), "78871" } } };
                var provider = new PersonProvider(httpClientFactory, loggerFactory, libraryManagerStub.Object, httpContextAccessorStub.Object, doubanApi, tmdbApi, omdbApi);
                var result = await provider.GetMetadata(info, CancellationToken.None);
                Assert.IsNotNull(result);

                var str = result.ToJson();
                Console.WriteLine(result.ToJson());
            }).GetAwaiter().GetResult();
        }


    }
}
