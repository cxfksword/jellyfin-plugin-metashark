using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Api.Http
{
    public class HttpClientHandlerEx : HttpClientHandler
    {
        public HttpClientHandlerEx()
        {
            // 忽略SSL证书问题
            ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            CookieContainer = new CookieContainer();
            UseCookies = true;
        }

        protected override Task<HttpResponseMessage> SendAsync(
       HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
    }
}
