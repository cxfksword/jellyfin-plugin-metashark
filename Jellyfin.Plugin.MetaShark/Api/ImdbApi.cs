using Jellyfin.Plugin.MetaShark.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Api
{
    public class ImdbApi : IDisposable
    {
        private readonly ILogger<DoubanApi> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly HttpClient httpClient;

        Regex regId = new Regex(@"/(tt\d+)", RegexOptions.Compiled);
        Regex regPersonId = new Regex(@"/(nm\d+)", RegexOptions.Compiled);

        public ImdbApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DoubanApi>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };
            httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// 通过imdb获取信息（会返回最新的imdb id）
        /// </summary>
        public async Task<string?> CheckNewIDAsync(string id, CancellationToken cancellationToken)
        {
            var cacheKey = $"CheckNewImdbID_{id}";
            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            if (this._memoryCache.TryGetValue<string?>(cacheKey, out var item))
            {
                return item;
            }

            try
            {
                var url = $"https://www.imdb.com/title/{id}/";
                var resp = await this.httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (resp.Headers.TryGetValues("Location", out var values))
                {
                    var location = values.First();
                    var newId = location.GetMatchGroup(this.regId);
                    if (!string.IsNullOrEmpty(newId))
                    {
                        item = newId;
                    }
                }
                this._memoryCache.Set(cacheKey, item, expiredOption);
                return item;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "CheckNewImdbID error. id: {0}", id);
                this._memoryCache.Set<string?>(cacheKey, null, expiredOption);
                return null;
            }

            return null;
        }

        /// <summary>
        /// 通过imdb获取信息（会返回最新的imdb id）
        /// </summary>
        public async Task<string?> CheckPersonNewIDAsync(string id, CancellationToken cancellationToken)
        {
            var cacheKey = $"CheckPersonNewImdbID_{id}";
            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            if (this._memoryCache.TryGetValue<string?>(cacheKey, out var item))
            {
                return item;
            }

            try
            {
                var url = $"https://www.imdb.com/name/{id}/";
                var resp = await this.httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (resp.Headers.TryGetValues("Location", out var values))
                {
                    var location = values.First();
                    var newId = location.GetMatchGroup(this.regPersonId);
                    if (!string.IsNullOrEmpty(newId))
                    {
                        item = newId;
                    }
                }
                this._memoryCache.Set(cacheKey, item, expiredOption);
                return item;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "CheckPersonNewImdbID error. id: {0}", id);
                this._memoryCache.Set<string?>(cacheKey, null, expiredOption);
                return null;
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memoryCache.Dispose();
            }
        }

        private bool IsEnable()
        {
            return Plugin.Instance?.Configuration.EnableTmdb ?? true;
        }
    }
}
