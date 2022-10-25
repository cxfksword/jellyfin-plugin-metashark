using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Certifications;
using TMDbLib.Rest;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        public async Task<CertificationsContainer> GetMovieCertificationsAsync(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("certification/movie/list");

            CertificationsContainer resp = await req.GetOfT<CertificationsContainer>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<CertificationsContainer> GetTvCertificationsAsync(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("certification/tv/list");

            CertificationsContainer resp = await req.GetOfT<CertificationsContainer>(cancellationToken).ConfigureAwait(false);

            return resp;
        }
    }
}
