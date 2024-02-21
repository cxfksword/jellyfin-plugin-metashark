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

            HttpResponseMessage response;
            var httpClient = GetHttpClient();
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                requestMessage.Headers.Add("User-Agent", DoubanApi.HTTP_USER_AGENT);
                requestMessage.Headers.Add("Referer", DoubanApi.HTTP_REFERER);

                response = await httpClient.SendAsync(requestMessage);
            }
            var stream = await response.Content.ReadAsStreamAsync();

            Response.StatusCode = (int)response.StatusCode;
            if (response.Content.Headers.ContentType != null)
            {
                Response.ContentType = response.Content.Headers.ContentType.ToString();
            }
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


        private HttpClient GetHttpClient()
        {
            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            return client;
        }
    }
}
