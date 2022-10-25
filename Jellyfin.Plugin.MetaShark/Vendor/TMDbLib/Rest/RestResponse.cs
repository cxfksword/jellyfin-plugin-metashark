using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TMDbLib.Utilities.Serializer;

namespace TMDbLib.Rest
{
    internal class RestResponse :IDisposable
    {
        private readonly HttpResponseMessage Response;

        public RestResponse(HttpResponseMessage response)
        {
            Response = response;
        }

        public bool IsValid => Response != null;

        public HttpStatusCode StatusCode => Response.StatusCode;

        public async Task<Stream> GetContent()
        {
            return await Response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        public string GetHeader(string name, string @default = null)
        {
            return Response.Headers.GetValues(name).FirstOrDefault() ?? @default;
        }

        [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected.", Justification = "RestResponse owns the response")]
        public virtual void Dispose()
        {
            Response?.Dispose();
        }
    }

    internal class RestResponse<T> : RestResponse
    {
        private readonly RestClient _client;

        public RestResponse(HttpResponseMessage response, RestClient client)
            : base(response)
        {
            _client = client;
        }

        public async Task<T> GetDataObject()
        {
            using Stream content = await GetContent().ConfigureAwait(false);

            return _client.Serializer.Deserialize<T>(content);
        }
    }
}