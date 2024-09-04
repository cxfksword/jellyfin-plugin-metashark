using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Text;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.MetaShark.Core;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaShark;

public class BoxSetManager : IHostedService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly Timer _timer;
    private readonly HashSet<string> _queuedTmdbCollection;
    private readonly ILogger<BoxSetManager> _logger; // TODO logging

    public BoxSetManager(ILibraryManager libraryManager, ICollectionManager collectionManager, ILoggerFactory loggerFactory)
    {
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _logger = loggerFactory.CreateLogger<BoxSetManager>();
        _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
        _queuedTmdbCollection = new HashSet<string>();
    }


    public async Task ScanLibrary(IProgress<double> progress)
    {
        var startIndex = 0;
        var pagesize = 1000;

        if (!(Plugin.Instance?.Configuration.EnableTmdbCollection ?? false))
        {
            _logger.LogInformation("插件配置中未打开自动创建合集功能");
            progress?.Report(100);
            return;
        }

        var boxSets = GetAllBoxSetsFromLibrary();
        var movieCollections = GetMoviesFromLibrary();

        _logger.LogInformation("共找到 {Count} 个合集信息", movieCollections.Count);
        int index = 0;
        foreach (var (collectionName, collectionMovies) in movieCollections)
        {
            progress?.Report(100.0 * index / movieCollections.Count);

            var boxSet = boxSets.FirstOrDefault(b => b?.Name == collectionName);
            await AddMoviesToCollection(collectionMovies, collectionName, boxSet).ConfigureAwait(false);
            index++;
        }

        progress?.Report(100);
    }

    private async Task AddMoviesToCollection(IList<Movie> movies, string collectionName, BoxSet boxSet)
    {
        if (movies.Count < 2)
        {
            // won't automatically create collection if only one movie in it
            return;
        }

        var movieIds = movies.Select(m => m.Id).ToList();
        if (boxSet is null)
        {
            _logger.LogInformation("创建合集 [{collectionName}]，添加电影：{moviesNames}", collectionName, movies.Select(m => m.Name).Aggregate((a, b) => a + ", " + b));
            boxSet = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
            {
                Name = collectionName,
            });

            await _collectionManager.AddToCollectionAsync(boxSet.Id, movieIds);

            // HACK: 等获取 boxset 元数据后再更新一次合集，用于修正刷新元数据后丢失关联电影的 BUG
            _queuedTmdbCollection.Add(collectionName);
            _timer.Change(60000, Timeout.Infinite);
        }
        else
        {
            _logger.LogInformation("更新合集 [{collectionName}]，添加电影：{moviesNames}", collectionName, movies.Select(m => m.Name).Aggregate((a, b) => a + ", " + b));
            await _collectionManager.AddToCollectionAsync(boxSet.Id, movieIds);
        }
    }


    private IReadOnlyCollection<BoxSet> GetAllBoxSetsFromLibrary()
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.BoxSet },
            CollapseBoxSetItems = false,
            Recursive = true
        }).Select(b => b as BoxSet).ToList();
    }

    
    public IDictionary<string, IList<Movie>> GetMoviesFromLibrary()
    {
        var collectionMoviesMap = new Dictionary<string, IList<Movie>>();

        foreach (var library in _libraryManager.RootFolder.Children)
        {
            // 判断当前是媒体库是否是电影，并开启了 metashark 插件
            var typeOptions = _libraryManager.GetLibraryOptions(library).TypeOptions;
            if (typeOptions.FirstOrDefault(x => x.Type == "Movie" && x.MetadataFetchers.Contains(Plugin.PluginName)) == null)
            {
                continue;
            }

            var startIndex = 0;
            var pagesize = 1000;
            
            while (true)
            {
                var movies = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie },
                    IsVirtualItem = false,
                    OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) },
                    Parent = library,
                    StartIndex = startIndex,
                    Limit = pagesize,
                    Recursive = true,
                    HasTmdbId = true
                }).Select(b => b as Movie).ToList();

                foreach (var movie in movies)
                {
                    if (string.IsNullOrEmpty(movie.CollectionName))
                    {
                        continue;
                    }
                
                    if (collectionMoviesMap.TryGetValue(movie.CollectionName, out var movieList))
                    {
                        movieList.Add(movie);
                    }
                    else
                    {
                        collectionMoviesMap[movie.CollectionName] = new List<Movie>() { movie };
                    }
                }

                if (movies.Count < pagesize)
                {
                    break;
                }

                startIndex += pagesize;
            }
        }

        return collectionMoviesMap;
    }

    private void OnLibraryManagerItemUpdated(object sender, ItemChangeEventArgs e)
    {
        if (!(Plugin.Instance?.Configuration.EnableTmdbCollection ?? false))
        {
            return;
        }

        // Only support movies at this time
        if (e.Item is not Movie movie || e.Item.LocationType == LocationType.Virtual)
        {
            return;
        }

        if (string.IsNullOrEmpty(movie.CollectionName))
        {
            return;
        }

        // 判断 item 所在的媒体库是否是电影，并开启了 metashark 插件
        var typeOptions = _libraryManager.GetLibraryOptions(movie).TypeOptions;
        if (typeOptions.FirstOrDefault(x => x.Type == "Movie" && x.MetadataFetchers.Contains(Plugin.PluginName)) == null)
        {
            return;
        }

        _queuedTmdbCollection.Add(movie.CollectionName);

        // Restart the timer. After idling for 60 seconds it should trigger the callback. This is to avoid clobbering during a large library update.
        _timer.Change(60000, Timeout.Infinite);
    }

    private void OnTimerElapsed()
    {
        // Stop the timer until next update
        _timer.Change(Timeout.Infinite, Timeout.Infinite);

        var tmdbCollectionNames = _queuedTmdbCollection.ToArray();
        // Clear the queue now, TODO what if it crashes? Should it be cleared after it's done?
        _queuedTmdbCollection.Clear();

        var boxSets = GetAllBoxSetsFromLibrary();
        var movies = GetMoviesFromLibrary();
        foreach (var collectionName in tmdbCollectionNames)
        {
            if (movies.TryGetValue(collectionName, out var collectionMovies))
            {
                var boxSet = boxSets.FirstOrDefault(b => b?.Name == collectionName);
                AddMoviesToCollection(collectionMovies, collectionName, boxSet).GetAwaiter().GetResult();
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemUpdated += OnLibraryManagerItemUpdated;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemUpdated -= OnLibraryManagerItemUpdated;
        return Task.CompletedTask;
    }
}