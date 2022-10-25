using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    /// <summary>
    /// OddbPersonProvider.
    /// </summary>
    public class PersonProvider : BaseProvider, IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MovieImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{OddbImageProvider}"/> interface.</param>
        /// <param name="doubanApi">Instance of <see cref="DoubanApi"/>.</param>
        public PersonProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<SeriesProvider>(), libraryManager, doubanApi, tmdbApi, omdbApi)
        {
        }

        /// <inheritdoc />
        public string Name => Plugin.PluginName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            return await Task.FromResult<IEnumerable<RemoteSearchResult>>(new List<RemoteSearchResult>());
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            MetadataResult<Person> result = new MetadataResult<Person>();

            var cid = info.GetProviderId(DoubanProviderId);
            this.Log($"GetPersonMetadata of [cid]: {cid}");
            if (!string.IsNullOrEmpty(cid))
            {

                var c = await this._doubanApi.GetCelebrityAsync(cid, cancellationToken).ConfigureAwait(false);
                if (c != null)
                {
                    Person p = new Person
                    {
                        Name = c.Name,
                        HomePageUrl = c.Site,
                        Overview = c.Intro,
                        PremiereDate = DateTime.ParseExact(c.Birthdate, "yyyy年MM月dd日", System.Globalization.CultureInfo.CurrentCulture)
                    };

                    p.SetProviderId(Plugin.ProviderId, c.Id);

                    if (!string.IsNullOrWhiteSpace(c.Birthplace))
                    {
                        p.ProductionLocations = new[] { c.Birthplace };
                    }

                    if (!string.IsNullOrEmpty(c.Imdb))
                    {
                        p.SetProviderId(MetadataProvider.Imdb, c.Imdb);
                    }

                    result.HasMetadata = true;
                    result.Item = p;
                    return result;
                }
            }

            var personTmdbId = info.GetProviderId(MetadataProvider.Tmdb);
            this.Log($"GetPersonMetadata of [personTmdbId]: {personTmdbId}");
            if (!string.IsNullOrEmpty(personTmdbId))
            {
                var person = await this._tmdbApi.GetPersonAsync(personTmdbId.ToInt(), cancellationToken).ConfigureAwait(false);
                if (person != null)
                {

                    result.HasMetadata = true;

                    var item = new Person
                    {
                        // Take name from incoming info, don't rename the person
                        // TODO: This should go in PersonMetadataService, not each person provider
                        Name = info.Name,
                        HomePageUrl = person.Homepage,
                        Overview = person.Biography,
                        PremiereDate = person.Birthday?.ToUniversalTime(),
                        EndDate = person.Deathday?.ToUniversalTime()
                    };

                    if (!string.IsNullOrWhiteSpace(person.PlaceOfBirth))
                    {
                        item.ProductionLocations = new[] { person.PlaceOfBirth };
                    }

                    item.SetProviderId(MetadataProvider.Tmdb, person.Id.ToString(CultureInfo.InvariantCulture));

                    if (!string.IsNullOrEmpty(person.ImdbId))
                    {
                        item.SetProviderId(MetadataProvider.Imdb, person.ImdbId);
                    }

                    result.HasMetadata = true;
                    result.Item = item;
                    return result;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            this.Log("Person GetImageResponse url: {0}", url);
            return await this._httpClientFactory.CreateClient().GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
        }

        private void Log(string? message, params object?[] args)
        {
            this._logger.LogInformation($"[MetaShark] {message}", args);
        }
    }
}
