using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.MetaShark.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    /// <summary>
    /// BoxSet provider powered by TMDb.
    /// </summary>
    public class BoxSetProvider : BaseProvider, IRemoteMetadataProvider<BoxSet, BoxSetInfo>
    {
        public BoxSetProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi, ImdbApi imdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<BoxSetProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi, imdbApi)
        {
        }

        public string Name => Plugin.PluginName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BoxSetInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(searchInfo.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            var language = searchInfo.MetadataLanguage;

            if (tmdbId > 0)
            {
                var collection = await _tmdbApi.GetCollectionAsync(tmdbId, language, language, cancellationToken).ConfigureAwait(false);

                if (collection is null)
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                var result = new RemoteSearchResult
                {
                    Name = collection.Name,
                    SearchProviderName = Name
                };

                if (collection.Images is not null)
                {
                    result.ImageUrl = _tmdbApi.GetPosterUrl(collection.PosterPath);
                }

                result.SetProviderId(MetadataProvider.Tmdb, collection.Id.ToString(CultureInfo.InvariantCulture));

                return new[] { result };
            }

            var collectionSearchResults = await _tmdbApi.SearchCollectionAsync(searchInfo.Name, language, cancellationToken).ConfigureAwait(false);

            var collections = new RemoteSearchResult[collectionSearchResults.Count];
            for (var i = 0; i < collectionSearchResults.Count; i++)
            {
                var result = collectionSearchResults[i];
                var collection = new RemoteSearchResult
                {
                    Name = result.Name,
                    SearchProviderName = Name,
                    ImageUrl = _tmdbApi.GetPosterUrl(result.PosterPath)
                };
                collection.SetProviderId(MetadataProvider.Tmdb, result.Id.ToString(CultureInfo.InvariantCulture));

                collections[i] = collection;
            }

            return collections;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<BoxSet>> GetMetadata(BoxSetInfo info, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(info.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            var language = info.MetadataLanguage;
            this.Log($"GetBoxSetMetadata of [name]: {info.Name} [tmdbId]: {tmdbId} EnableTmdb: {config.EnableTmdb}");

            // We don't already have an Id, need to fetch it
            if (tmdbId <= 0)
            {
                // ParseName is required here.
                // Caller provides the filename with extension stripped and NOT the parsed filename
                var parsedName = _libraryManager.ParseName(info.Name);
                var searchResults = await _tmdbApi.SearchCollectionAsync(parsedName.Name, language, cancellationToken).ConfigureAwait(false);

                if (searchResults is not null && searchResults.Count > 0)
                {
                    tmdbId = searchResults[0].Id;
                }
            }

            var result = new MetadataResult<BoxSet>();

            if (tmdbId > 0)
            {
                var collection = await _tmdbApi.GetCollectionAsync(tmdbId, language, language, cancellationToken).ConfigureAwait(false);

                if (collection is not null)
                {
                    var item = new BoxSet
                    {
                        Name = collection.Name,
                        Overview = collection.Overview,
                    };

                    var oldBotSet = _libraryManager.GetItemList(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                        CollapseBoxSetItems = false,
                        Recursive = true
                    }).Select(b => b as BoxSet).FirstOrDefault(x => x.Name == collection.Name);
                    if (oldBotSet != null)
                    {
                        item.LinkedChildren = oldBotSet.LinkedChildren;
                    }

                    item.SetProviderId(MetadataProvider.Tmdb, collection.Id.ToString(CultureInfo.InvariantCulture));

                    result.HasMetadata = true;
                    result.Item = item;
                }
            }

            return result;
        }

    }
}