using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Credit;
using TMDbLib.Rest;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        public async Task<Credit> GetCreditsAsync(string id, CancellationToken cancellationToken = default)
        {
            return await GetCreditsAsync(id, DefaultLanguage, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Credit> GetCreditsAsync(string id, string language, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("credit/{id}");

            if (!string.IsNullOrEmpty(language))
                req.AddParameter("language", language);

            req.AddUrlSegment("id", id);

            Credit resp = await req.GetOfT<Credit>(cancellationToken).ConfigureAwait(false);

            return resp;
        }
    }
}
