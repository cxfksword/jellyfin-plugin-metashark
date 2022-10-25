using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.TvShows;
using TMDbLib.Rest;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        /// <summary>
        /// Retrieve a collection of tv episode groups by id
        /// </summary>
        /// <param name="id">Episode group id</param>
        /// <param name="language">If specified the api will attempt to return a localized result. ex: en,it,es </param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The requested collection of tv episode groups</returns>
        public async Task<TvGroupCollection> GetTvEpisodeGroupsAsync(string id, string language = null, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("tv/episode_group/{id}");
            req.AddUrlSegment("id", id);

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            using RestResponse<TvGroupCollection> response = await req.Get<TvGroupCollection>(cancellationToken).ConfigureAwait(false);

            if (!response.IsValid)
                return null;

            return await response.GetDataObject().ConfigureAwait(false);
        }
    }
}