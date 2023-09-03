using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Providers;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Persistence;
using System.Net.Http;
using System.Net;

namespace Jellyfin.Plugin.MetaShark
{
    /// <inheritdoc />
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<DoubanApi>((ctx) =>
            {
                return new DoubanApi(ctx.GetRequiredService<ILoggerFactory>());
            });
            serviceCollection.AddSingleton<TmdbApi>((ctx) =>
            {
                return new TmdbApi(ctx.GetRequiredService<ILoggerFactory>());
            });
            serviceCollection.AddSingleton<OmdbApi>((ctx) =>
            {
                return new OmdbApi(ctx.GetRequiredService<ILoggerFactory>());
            });
            serviceCollection.AddSingleton<ImdbApi>((ctx) =>
            {
                return new ImdbApi(ctx.GetRequiredService<ILoggerFactory>());
            });

            // douban httpclient 忽略 ssl 证书校验
            serviceCollection.AddHttpClient("douban", client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", DoubanApi.HTTP_USER_AGENT);
                client.DefaultRequestHeaders.Add("Referer", DoubanApi.HTTP_REFERER);
            }).ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
                };
                return handler;
            });

        }
    }
}
