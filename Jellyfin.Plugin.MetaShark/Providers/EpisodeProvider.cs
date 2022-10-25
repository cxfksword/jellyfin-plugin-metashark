using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Model;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public class EpisodeProvider : BaseProvider, IRemoteMetadataProvider<Episode, EpisodeInfo>
    {

        private static readonly Regex[] EpisodeFileNameRegex =
        {
            new(@"\[([\d\.]{2,})\]"),
            new(@"- ?([\d\.]{2,})"),
            new(@"EP?([\d\.]{2,})", RegexOptions.IgnoreCase),
            new(@"\[([\d\.]{2,})"),
            new(@"#([\d\.]{2,})"),
            new(@"(\d{2,})")
        };

        public EpisodeProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, ILibraryManager libraryManager, DoubanApi doubanApi, TmdbApi tmdbApi, OmdbApi omdbApi)
            : base(httpClientFactory, loggerFactory.CreateLogger<SeriesProvider>(), libraryManager, doubanApi, tmdbApi, omdbApi)
        {
        }

        public string Name => Plugin.PluginName;


        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo info, CancellationToken cancellationToken)
        {
            this.Log($"GetSearchResults of [name]: {info.Name}");
            return await Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            this.Log($"GetEpisodeMetadata of [name]: {info.Name} number: {info.IndexNumber}");
            Console.WriteLine(info.ToJson());
            Console.WriteLine(info.SeriesProviderIds.ToJson());
            var result = new MetadataResult<Episode>();

            // 剧集信息只有tmdb有
            info.SeriesProviderIds.TryGetValue(MetadataProvider.Tmdb.ToString(), out var seriesTmdbId);
            var seasonNumber = info.ParentIndexNumber; // 没有season级目录时，会为null
            var episodeNumber = info.IndexNumber;
            var indexNumberEnd = info.IndexNumberEnd;
            Console.WriteLine($"seasonNumber:{seasonNumber} episodeNumber:{episodeNumber} indexNumberEnd:{indexNumberEnd}");
            if (episodeNumber is null or 0)
            {
                // 从文件名获取剧集的indexNumber
                var fileName = Path.GetFileName(info.Path) ?? string.Empty;
                episodeNumber = this.GuessEpisodeNumber(episodeNumber, fileName);
                this.Log("GuessEpisodeNumber: fileName: {0} episodeNumber: {1}", fileName, episodeNumber);
            }

            if (episodeNumber is null or 0 || seasonNumber is null or 0 || string.IsNullOrEmpty(seriesTmdbId))
            {
                return result;
            }

            // 利用season缓存取剧集信息会更快
            var seasonResult = await this._tmdbApi
                .GetSeasonAsync(seriesTmdbId.ToInt(), seasonNumber.Value, info.MetadataLanguage, info.MetadataLanguage, cancellationToken)
                .ConfigureAwait(false);
            if (seasonResult == null || seasonResult.Episodes.Count < episodeNumber.Value)
            {
                return result;
            }

            var episodeResult = seasonResult.Episodes[episodeNumber.Value - 1];

            result.HasMetadata = true;
            result.QueriedById = true;

            if (!string.IsNullOrEmpty(episodeResult.Overview))
            {
                // if overview is non-empty, we can assume that localized data was returned
                result.ResultLanguage = info.MetadataLanguage;
            }

            var item = new Episode
            {
                IndexNumber = episodeNumber,
                ParentIndexNumber = seasonNumber,
                IndexNumberEnd = info.IndexNumberEnd
            };


            item.PremiereDate = episodeResult.AirDate;
            item.ProductionYear = episodeResult.AirDate?.Year;
            item.Name = episodeResult.Name;
            item.Overview = episodeResult.Overview;
            item.CommunityRating = Convert.ToSingle(episodeResult.VoteAverage);

            result.Item = item;

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            this.Log("GetImageResponse url: {0}", url);
            return _httpClientFactory.CreateClient().GetAsync(new Uri(url), cancellationToken);
        }

        private int? GuessEpisodeNumber(int? current, string fileName, double max = double.PositiveInfinity)
        {
            var episodeIndex = current;

            var result = AnitomySharp.AnitomySharp.Parse(fileName).FirstOrDefault(x => x.Category == AnitomySharp.Element.ElementCategory.ElementEpisodeNumber);
            if (result != null)
            {
                episodeIndex = result.Value.ToInt();
            }

            foreach (var regex in EpisodeFileNameRegex)
            {
                if (!regex.IsMatch(fileName))
                    continue;
                if (!int.TryParse(regex.Match(fileName).Groups[1].Value.Trim('.'), out var index))
                    continue;
                episodeIndex = index;
                break;
            }

            return episodeIndex;
        }

    }
}
