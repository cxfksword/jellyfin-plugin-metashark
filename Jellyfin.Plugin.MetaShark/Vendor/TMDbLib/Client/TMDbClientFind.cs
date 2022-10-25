using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Find;
using TMDbLib.Rest;
using TMDbLib.Utilities;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        /// <summary>
        /// FindAsync movies, people and tv shows by an external id.
        /// The following types can be found based on the specified external id's
        /// - Movies: Imdb
        /// - People: Imdb, FreeBaseMid, FreeBaseId, TvRage
        /// - TV Series: Imdb, FreeBaseMid, FreeBaseId, TvRage, TvDb
        /// </summary>
        /// <param name="source">The source the specified id belongs to</param>
        /// <param name="id">The id of the object you wish to located</param>
        /// <returns>A list of all objects in TMDb that matched your id</returns>
        /// <param name="cancellationToken">A cancellation token</param>
        public Task<FindContainer> FindAsync(FindExternalSource source, string id, CancellationToken cancellationToken = default)
        {
            return FindAsync(source, id, null, cancellationToken);
        }

        /// <summary>
        /// FindAsync movies, people and tv shows by an external id.
        /// The following types can be found based on the specified external id's
        /// - Movies: Imdb
        /// - People: Imdb, FreeBaseMid, FreeBaseId, TvRage
        /// - TV Series: Imdb, FreeBaseMid, FreeBaseId, TvRage, TvDb
        /// </summary>
        /// <param name="source">The source the specified id belongs to</param>
        /// <param name="id">The id of the object you wish to located</param>
        /// <returns>A list of all objects in TMDb that matched your id</returns>
        /// <param name="language">If specified the api will attempt to return a localized result. ex: en,it,es.</param>
        /// <param name="cancellationToken">A cancellation token</param>
        public async Task<FindContainer> FindAsync(FindExternalSource source, string id, string language, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("find/{id}");

            req.AddUrlSegment("id", WebUtility.UrlEncode(id));
            req.AddParameter("external_source", source.GetDescription());

            language ??= DefaultLanguage;
            if (!string.IsNullOrEmpty(language))
                req.AddParameter("language", language);

            FindContainer resp = await req.GetOfT<FindContainer>(cancellationToken).ConfigureAwait(false);

            return resp;
        }
    }
}