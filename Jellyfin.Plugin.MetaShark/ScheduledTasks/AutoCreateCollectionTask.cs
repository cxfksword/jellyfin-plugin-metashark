using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Collections;

namespace Jellyfin.Plugin.MetaShark.ScheduledTasks
{
    public class AutoCreateCollectionTask : IScheduledTask
    {
        private readonly BoxSetManager _boxSetManager;
        private readonly ILogger _logger;

        public string Key => $"{Plugin.PluginName}AutoCreateCollection";

        public string Name => "扫描自动创建合集";

        public string Description => $"扫描媒体库创建合集，需要先在配置中开启获取电影系列信息";

        public string Category => Plugin.PluginName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoCreateCollectionTask"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public AutoCreateCollectionTask(ILoggerFactory loggerFactory, ILibraryManager libraryManager, ICollectionManager collectionManager)
        {
            _logger = loggerFactory.CreateLogger<AutoCreateCollectionTask>();
            _boxSetManager = new BoxSetManager(libraryManager, collectionManager, loggerFactory);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(0).Ticks
            };
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始扫描媒体库自动创建合集...");
            await _boxSetManager.ScanLibrary(progress).ConfigureAwait(false);
            _logger.LogInformation("扫描媒体库自动创建合集执行完成");
        }
    }
}