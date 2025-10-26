using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.MetaShark.Core;
using Jellyfin.Plugin.MetaShark.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaShark.ScheduledTasks
{
    /// <summary>
    /// Task to refresh metadata for items missing provider IDs.
    /// </summary>
    public class RefreshMetadataTask : IScheduledTask
    {
        private readonly ILogger<RefreshMetadataTask> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshMetadataTask"/> class.
        /// </summary>
        public RefreshMetadataTask(ILogger<RefreshMetadataTask> logger, ILibraryManager libraryManager, IProviderManager providerManager,
        IFileSystem fileSystem)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
        }

        /// <inheritdoc />
        public string Name => "重新刮削失败的影片";

        /// <inheritdoc />
        public string Key => $"{Plugin.PluginName}RefreshMissingMetadata";

        /// <inheritdoc />
        public string Description => "重新刮削之前刮削失败的影片.";

        /// <inheritdoc />
        public string Category => Plugin.PluginName;

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // No default triggers, meant to be run manually.
            return Enumerable.Empty<TaskTriggerInfo>();
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting task to refresh items with missing provider IDs.");

            var itemsToRefresh = GetItemsWithoutProviderIds();
            int totalItems = itemsToRefresh.Count;
            int processedCount = 0;

            if (totalItems == 0)
            {
                _logger.LogInformation("No items found missing both Douban and TMDB provider IDs.");
                progress.Report(100);
                return;
            }

            _logger.LogInformation("Found {Count} items to refresh.", totalItems);

            foreach (var item in itemsToRefresh)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Queueing refresh for item: {Name} (Id: {Id})", item.Name, item.Id);

                var refreshOptions = new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ImageRefreshMode = MetadataRefreshMode.FullRefresh,
                    ReplaceAllMetadata = false,
                    ReplaceAllImages = false,
                };

                this._providerManager.QueueRefresh(item.Id, refreshOptions, RefreshPriority.Normal);

                processedCount++;
                progress.Report(processedCount * 100.0 / totalItems);

                // 等待5秒，避免短时间内请求过多
                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Finished queueing refreshes for {Count} items.", totalItems);
        }

        private List<BaseItem> GetItemsWithoutProviderIds()
        {
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                IsMissing = false,
                Recursive = true
            };

            var items = _libraryManager.GetItemList(query);

            return items.Where(item =>
            (!item.ProviderIds.ContainsKey(BaseProvider.DoubanProviderId) && !item.HasImage(ImageType.Primary)) ||
             (File.Exists(item.Path) && !item.HasImage(ImageType.Primary)))
            .ToList();
        }
    }
}
