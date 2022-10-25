using System.Threading;
using System.Threading.Tasks;

namespace TMDbLib.Rest
{
    internal static class RestRequestExtensions
    {
        public static async Task<T> DeleteOfT<T>(this RestRequest request, CancellationToken cancellationToken)
        {
            using RestResponse<T> resp = await request.Delete<T>(cancellationToken).ConfigureAwait(false);

            if (!resp.IsValid)
                return default;

            return await resp.GetDataObject().ConfigureAwait(false);
        }

        public static async Task<T> GetOfT<T>(this RestRequest request, CancellationToken cancellationToken)
        {
            using RestResponse<T> resp = await request.Get<T>(cancellationToken).ConfigureAwait(false);

            if (!resp.IsValid)
                return default;

            return await resp.GetDataObject().ConfigureAwait(false);
        }

        public static async Task<T> PostOfT<T>(this RestRequest request, CancellationToken cancellationToken)
        {
            using RestResponse<T> resp = await request.Post<T>(cancellationToken).ConfigureAwait(false);

            if (!resp.IsValid)
                return default;

            return await resp.GetDataObject().ConfigureAwait(false);
        }
    }
}