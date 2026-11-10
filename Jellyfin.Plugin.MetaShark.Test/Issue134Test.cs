using System.Collections.Generic;
using Jellyfin.Plugin.MetaShark.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Moq;

namespace Jellyfin.Plugin.MetaShark.Test
{
    /// <summary>
    /// issue #134：Jellyfin 12.4 上 ILibraryManager.GetItemList(InternalItemsQuery) 的返回类型
    /// 与插件编译期绑定的签名不一致，运行时会抛 MissingMethodException，导致合集定时任务
    /// 把整个 Jellyfin 进程带崩。这里验证兼容查询方法基于跨版本稳定的 QueryItems 正确返回结果。
    /// </summary>
    [TestClass]
    public class Issue134Test
    {
        [TestMethod]
        public void QueryItemsCompat_ShouldReturnItemsFromQueryResult()
        {
            var items = new List<BaseItem>
            {
                new Movie { Name = "movie-a" },
                new Movie { Name = "movie-b" },
            };

            var libraryManager = new Mock<ILibraryManager>();
            libraryManager
                .Setup(m => m.QueryItems(It.IsAny<InternalItemsQuery>()))
                .Returns(new QueryResult<BaseItem>(items));

            var result = libraryManager.Object.QueryItemsCompat(new InternalItemsQuery());

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("movie-a", result[0].Name);
            Assert.AreEqual("movie-b", result[1].Name);
        }

        [TestMethod]
        public void QueryItemsCompat_ShouldReturnEmptyWhenQueryResultHasNoItems()
        {
            var libraryManager = new Mock<ILibraryManager>();
            libraryManager
                .Setup(m => m.QueryItems(It.IsAny<InternalItemsQuery>()))
                .Returns(new QueryResult<BaseItem>(new List<BaseItem>()));

            var result = libraryManager.Object.QueryItemsCompat(new InternalItemsQuery());

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void QueryItemsCompat_ShouldPassQueryThroughToLibraryManager()
        {
            var query = new InternalItemsQuery { Recursive = true };
            var libraryManager = new Mock<ILibraryManager>();
            libraryManager
                .Setup(m => m.QueryItems(It.IsAny<InternalItemsQuery>()))
                .Returns(new QueryResult<BaseItem>(new List<BaseItem>()));

            libraryManager.Object.QueryItemsCompat(query);

            libraryManager.Verify(m => m.QueryItems(query), Times.Once);
        }
    }
}
