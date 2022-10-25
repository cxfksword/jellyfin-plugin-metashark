using System;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Authentication;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Lists;
using TMDbLib.Rest;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        private async Task<bool> GetManipulateMediaListAsyncInternal(string listId, int movieId, string method, CancellationToken cancellationToken = default)
        {
            RequireSessionId(SessionType.UserSession);

            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException(nameof(listId));

            // Movie Id is expected by the API and can not be null
            if (movieId <= 0)
                throw new ArgumentOutOfRangeException(nameof(movieId));

            RestRequest req = _client.Create("list/{listId}/{method}");
            req.AddUrlSegment("listId", listId);
            req.AddUrlSegment("method", method);
            AddSessionId(req, SessionType.UserSession);

            req.SetBody(new { media_id = movieId });

            using RestResponse<PostReply> response = await req.Post<PostReply>(cancellationToken).ConfigureAwait(false);

            // Status code 12 = "The item/record was updated successfully"
            // Status code 13 = "The item/record was deleted successfully"
            PostReply item = await response.GetDataObject().ConfigureAwait(false);

            // TODO: Previous code checked for item=null
            return item.StatusCode == 12 || item.StatusCode == 13;
        }

        /// <summary>
        /// Retrieve a list by it's id
        /// </summary>
        /// <param name="listId">The id of the list you want to retrieve</param>
        /// <param name="language">If specified the api will attempt to return a localized result. ex: en,it,es </param>
        /// <param name="cancellationToken">A cancellation token</param>
        public async Task<GenericList> GetListAsync(string listId, string language = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException(nameof(listId));

            RestRequest req = _client.Create("list/{listId}");
            req.AddUrlSegment("listId", listId);

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            GenericList resp = await req.GetOfT<GenericList>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        /// <summary>
        /// Will check if the provided movie id is present in the specified list
        /// </summary>
        /// <param name="listId">Id of the list to check in</param>
        /// <param name="movieId">Id of the movie to check for in the list</param>
        /// <param name="cancellationToken">A cancellation token</param>
        public async Task<bool> GetListIsMoviePresentAsync(string listId, int movieId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException(nameof(listId));

            if (movieId <= 0)
                throw new ArgumentOutOfRangeException(nameof(movieId));

            RestRequest req = _client.Create("list/{listId}/item_status");
            req.AddUrlSegment("listId", listId);
            req.AddParameter("movie_id", movieId.ToString());

            using RestResponse<ListStatus> response = await req.Get<ListStatus>(cancellationToken).ConfigureAwait(false);

            return (await response.GetDataObject().ConfigureAwait(false)).ItemPresent;
        }

        /// <summary>
        /// Adds a movie to a specified list
        /// </summary>
        /// <param name="listId">The id of the list to add the movie to</param>
        /// <param name="movieId">The id of the movie to add</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>True if the method was able to add the movie to the list, will retrun false in case of an issue or when the movie was already added to the list</returns>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<bool> ListAddMovieAsync(string listId, int movieId, CancellationToken cancellationToken = default)
        {
            return await GetManipulateMediaListAsyncInternal(listId, movieId, "add_item", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Clears a list, without confirmation.
        /// </summary>
        /// <param name="listId">The id of the list to clear</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>True if the method was able to remove the movie from the list, will retrun false in case of an issue or when the movie was not present in the list</returns>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<bool> ListClearAsync(string listId, CancellationToken cancellationToken = default)
        {
            RequireSessionId(SessionType.UserSession);

            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException(nameof(listId));

            RestRequest request = _client.Create("list/{listId}/clear");
            request.AddUrlSegment("listId", listId);
            request.AddParameter("confirm", "true");
            AddSessionId(request, SessionType.UserSession);

            using RestResponse<PostReply> response = await request.Post<PostReply>(cancellationToken).ConfigureAwait(false);

            // Status code 12 = "The item/record was updated successfully"
            PostReply item = await response.GetDataObject().ConfigureAwait(false);

            // TODO: Previous code checked for item=null
            return item.StatusCode == 12;
        }

        /// <summary>
        /// Creates a new list for the user associated with the current session
        /// </summary>
        /// <param name="name">The name of the new list</param>
        /// <param name="description">Optional description for the list</param>
        /// <param name="language">Optional language that might indicate the language of the content in the list</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<string> ListCreateAsync(string name, string description = "", string language = null, CancellationToken cancellationToken = default)
        {
            RequireSessionId(SessionType.UserSession);

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            // Description is expected by the API and can not be null
            if (string.IsNullOrWhiteSpace(description))
                description = "";

            RestRequest req = _client.Create("list");
            AddSessionId(req, SessionType.UserSession);

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
            {
                req.SetBody(new { name = name, description = description, language = language });

            }
            else
            {
                req.SetBody(new { name = name, description = description });
            }

            using RestResponse<ListCreateReply> response = await req.Post<ListCreateReply>(cancellationToken).ConfigureAwait(false);

            return (await response.GetDataObject().ConfigureAwait(false)).ListId;
        }

        /// <summary>
        /// Deletes the specified list that is owned by the user
        /// </summary>
        /// <param name="listId">A list id that is owned by the user associated with the current session id</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<bool> ListDeleteAsync(string listId, CancellationToken cancellationToken = default)
        {
            RequireSessionId(SessionType.UserSession);

            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException(nameof(listId));

            RestRequest req = _client.Create("list/{listId}");
            req.AddUrlSegment("listId", listId);
            AddSessionId(req, SessionType.UserSession);

            using RestResponse<PostReply> response = await req.Delete<PostReply>(cancellationToken).ConfigureAwait(false);

            // Status code 13 = success
            PostReply item = await response.GetDataObject().ConfigureAwait(false);

            // TODO: Previous code checked for item=null
            return item.StatusCode == 13;
        }

        /// <summary>
        /// Removes a movie from the specified list
        /// </summary>
        /// <param name="listId">The id of the list to add the movie to</param>
        /// <param name="movieId">The id of the movie to add</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>True if the method was able to remove the movie from the list, will retrun false in case of an issue or when the movie was not present in the list</returns>
        /// <remarks>Requires a valid user session</remarks>
        /// <exception cref="UserSessionRequiredException">Thrown when the current client object doens't have a user session assigned.</exception>
        public async Task<bool> ListRemoveMovieAsync(string listId, int movieId, CancellationToken cancellationToken = default)
        {
            return await GetManipulateMediaListAsyncInternal(listId, movieId, "remove_item", cancellationToken).ConfigureAwait(false);
        }
    }
}