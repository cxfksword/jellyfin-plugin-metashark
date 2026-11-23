using System;
using System.Threading;
using System.Linq;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediaBrowser.Common.Net;
using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Model;

namespace Jellyfin.Plugin.MetaShark.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("/plugin/metashark")]
    public class ApiController : ControllerBase
    {
        private readonly DoubanApi _doubanApi;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiController"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        public ApiController(IHttpClientFactory httpClientFactory, DoubanApi doubanApi)
        {
            this._httpClientFactory = httpClientFactory;
            this._doubanApi = doubanApi;
        }


        /// <summary>
        /// 代理访问图片.
        /// </summary>
        [Route("proxy/image")]
        [HttpGet]
        public async Task<Stream> ProxyImage(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ResourceNotFoundException();
            }

            // 该接口匿名开放，只允许代理豆瓣图床域名，避免未授权 SSRF（issue #138）
            if (!IsAllowedDoubanImageUrl(url))
            {
                throw new ResourceNotFoundException();
            }

            HttpResponseMessage response;
            var httpClient = GetHttpClient();
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                requestMessage.Headers.Add("User-Agent", DoubanApi.HTTP_USER_AGENT);
                requestMessage.Headers.Add("Referer", DoubanApi.HTTP_REFERER);

                response = await httpClient.SendAsync(requestMessage);
            }

            // 只回传图片类型，避免把任意响应体原样透传给请求方
            if (!IsImageContentType(response.Content.Headers.ContentType?.MediaType))
            {
                response.Dispose();
                throw new ResourceNotFoundException();
            }

            var stream = await response.Content.ReadAsStreamAsync();

            Response.StatusCode = (int)response.StatusCode;
            Response.ContentType = response.Content.Headers.ContentType.ToString();
            Response.ContentLength = response.Content.Headers.ContentLength;

            foreach (var header in response.Headers)
            {
                Response.Headers.Add(header.Key, header.Value.First());
            }

            return stream;
        }

        /// <summary>
        /// 检查豆瓣cookie是否失效.
        /// </summary>
        [Route("douban/checklogin")]
        [HttpGet]
        public async Task<ApiResult> CheckDoubanLogin()
        {
            var loginInfo = await this._doubanApi.GetLoginInfoAsync(CancellationToken.None).ConfigureAwait(false);
            return new ApiResult(loginInfo.IsLogined ? 1 : 0, loginInfo.Name);
        }

        /// <summary>
        /// 判断 url 是否指向豆瓣图床域名（http/https）。
        /// </summary>
        /// <param name="url">待校验的 url。</param>
        /// <returns>是豆瓣图床域名时返回 true。</returns>
        public static bool IsAllowedDoubanImageUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var host = uri.IdnHost;
            return string.Equals(host, "doubanio.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".doubanio.com", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断 Content-Type 是否为图片类型。
        /// </summary>
        /// <param name="mediaType">上游返回的 media type。</param>
        /// <returns>是图片类型时返回 true。</returns>
        public static bool IsImageContentType(string mediaType)
        {
            return !string.IsNullOrEmpty(mediaType)
                && mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        private HttpClient GetHttpClient()
        {
            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            return client;
        }
    }
}
