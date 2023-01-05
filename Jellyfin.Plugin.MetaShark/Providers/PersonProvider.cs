using System.Net.Mime;
using System.Xml.Schema;
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
using TMDbLib.Objects.Find;
using Microsoft.AspNetCore.Http;

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
        public PersonProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, IHttpContextAccessor httpContextAccessor, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<PersonProvider>(), libraryManager, httpContextAccessor, doubanApi, tmdbApi, omdbApi)
        {
        }

        /// <inheritdoc />
        public string Name => Plugin.PluginName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            this.Log($"GetPersonSearchResults of [name]: {searchInfo.Name}");

            var result = new List<RemoteSearchResult>();
            var cid = searchInfo.GetProviderId(DoubanProviderId);
            if (!string.IsNullOrEmpty(cid))
            {
                var celebrity = await this._doubanApi.GetCelebrityAsync(cid, cancellationToken).ConfigureAwait(false);
                if (celebrity != null)
                {
                    result.Add(new RemoteSearchResult
                    {
                        SearchProviderName = DoubanProviderName,
                        ProviderIds = new Dictionary<string, string> { { DoubanProviderId, celebrity.Id } },
                        ImageUrl = this.GetProxyImageUrl(celebrity.Img),
                        Name = celebrity.Name,
                    }
                    );

                    return result;
                }
            }



            var res = await this._doubanApi.SearchCelebrityAsync(searchInfo.Name, cancellationToken).ConfigureAwait(false);
            result.AddRange(res.Take(this.config.MaxSearchResult).Select(x =>
            {
                return new RemoteSearchResult
                {
                    SearchProviderName = DoubanProviderName,
                    ProviderIds = new Dictionary<string, string> { { DoubanProviderId, x.Id } },
                    ImageUrl = this.GetProxyImageUrl(x.Img),
                    Name = x.Name,
                };
            }));

            return result;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Person>?> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>();

            var cid = info.GetProviderId(DoubanProviderId);
            this.Log($"GetPersonMetadata of [name]: {info.Name} [cid]: {cid}");
            if (!string.IsNullOrEmpty(cid))
            {

                var c = await this._doubanApi.GetCelebrityAsync(cid, cancellationToken).ConfigureAwait(false);
                if (c != null)
                {
                    var item = new Person
                    {
                        // Name = c.Name.Trim(),  // 名称需保持和info.Name一致，不然会导致关联不到影片，自动被删除
                        HomePageUrl = c.Site,
                        Overview = c.Intro,
                        PremiereDate = DateTime.ParseExact(c.Birthdate, "yyyy年MM月dd日", System.Globalization.CultureInfo.CurrentCulture)
                    };
                    if (!string.IsNullOrWhiteSpace(c.Birthplace))
                    {
                        item.ProductionLocations = new[] { c.Birthplace };
                    }

                    item.SetProviderId(DoubanProviderId, cid);
                    if (!string.IsNullOrEmpty(c.Imdb))
                    {
                        item.SetProviderId(MetadataProvider.Imdb, c.Imdb);
                        // 通过imdb获取TMDB id
                        var findResult = await this._tmdbApi.FindByExternalIdAsync(c.Imdb, FindExternalSource.Imdb, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);
                        if (findResult?.PersonResults != null && findResult.PersonResults.Count > 0)
                        {
                            this.Log($"GetPersonMetadata of found tmdb [id]: {findResult.PersonResults[0].Id}");
                            item.SetProviderId(MetadataProvider.Tmdb, $"{findResult.PersonResults[0].Id}");
                        }
                    }

                    result.QueriedById = true;
                    result.HasMetadata = true;
                    result.Item = item;

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
                    var item = new Person
                    {
                        // Name = info.Name.Trim(),   // 名称需保持和info.Name一致，不然会导致关联不到影片，自动被删除
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
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            this.Log("Person GetImageResponse url: {0}", url);
            return this._httpClientFactory.CreateClient().GetAsync(new Uri(url), cancellationToken);
        }
    }
}
