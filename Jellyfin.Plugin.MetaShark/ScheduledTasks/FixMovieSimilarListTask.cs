using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Providers;
using Jellyfin.Plugin.MetaShark.Model;

namespace Jellyfin.Plugin.MetaShark.ScheduledTasks
{
    public class FixMovieSimilarListTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        public string Key => $"{Plugin.PluginName}FixMovieSimilarList";

        public string Name => "修复电影推荐列表";

        public string Description => $"修复电影推荐列表只有一部影片的问题。";

        public string Category => Plugin.PluginName;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixMovieSimilarListTask"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public FixMovieSimilarListTask(ILoggerFactory loggerFactory, ILibraryManager libraryManager)
        {
            _logger = loggerFactory.CreateLogger<FixMovieSimilarListTask>();
            _libraryManager = libraryManager;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new List<TaskTriggerInfo>();
            // yield return new TaskTriggerInfo
            // {
            //     Type = TaskTriggerInfo.TriggerWeekly,
            //     DayOfWeek = DayOfWeek.Monday,
            //     TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
            // };
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            await Task.Yield();

            progress?.Report(0);
            // 只有电影有问题
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                // MediaTypes = new[] { MediaType.Video },
                HasAnyProviderId = new Dictionary<string, string>() { { Plugin.ProviderId, string.Empty } },
                IncludeItemTypes = new[] { BaseItemKind.Movie }
            }).ToList();

            _logger.LogInformation("Fix movie similar list for {0} videos.", items.Count);

            var successCount = 0;
            var failCount = 0;
            foreach (var (item, idx) in items.WithIndex())
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report((double)idx / items.Count * 100);

                try
                {
                    // 判断电影带有旧的元数据，进行替换处理
                    var sid = item.GetProviderId(BaseProvider.DoubanProviderId);
                    var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
                    var providerVal = item.GetProviderId(Plugin.ProviderId);
                    if (providerVal == "douban" && !string.IsNullOrEmpty(sid))
                    {
                        var detail = this._libraryManager.GetItemById(item.Id);
                        detail.SetProviderId(Plugin.ProviderId,  $"{MetaSource.Douban}_{sid}");
                        await detail.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
                    }

                    if (providerVal == "tmdb" && !string.IsNullOrEmpty(tmdbId))
                    {
                        var detail = this._libraryManager.GetItemById(item.Id);
                        detail.SetProviderId(Plugin.ProviderId,  $"{MetaSource.Tmdb}_{tmdbId}");
                        await detail.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
                    }

                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fix movie similar list for movie {0}: {1}", item.Name, ex.Message);
                    failCount++;
                }
            }

            progress?.Report(100);
            _logger.LogInformation("Exectue task completed. success: {0} fail: {1}", successCount, failCount);
        }

    }
}