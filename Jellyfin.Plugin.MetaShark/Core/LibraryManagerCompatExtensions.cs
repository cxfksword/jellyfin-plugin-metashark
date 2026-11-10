using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.MetaShark.Core
{
    /// <summary>
    /// 兼容不同 Jellyfin 版本的 <see cref="ILibraryManager"/> 查询扩展方法。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ILibraryManager.GetItemList(InternalItemsQuery)</c> 的返回类型在 Jellyfin 版本间发生过变化：
    /// 10.9 返回 <c>List&lt;BaseItem&gt;</c>，10.11 / 12.x 返回 <c>IReadOnlyList&lt;BaseItem&gt;</c>。
    /// 插件在编译期会绑定到某个具体的方法签名，若运行时的宿主提供的是另一种返回类型，
    /// 就会在 JIT 解析时抛出 <see cref="MissingMethodException"/>。
    /// </para>
    /// <para>
    /// 这正是 issue #134 的原因：MetaShark 2.1.1.0 基于 Jellyfin.Controller 10.9 编译，
    /// 调用的是返回 <c>List&lt;BaseItem&gt;</c> 的 <c>GetItemList</c>，在 Jellyfin 12.4 上找不到该签名而崩溃；
    /// 又因为 <c>BoxSetManager.OnTimerElapsed</c> 运行在 <see cref="System.Threading.Timer"/> 回调里，
    /// 该异常成为未处理异常并导致整个 Jellyfin 进程退出。
    /// </para>
    /// <para>
    /// <c>QueryItems(InternalItemsQuery)</c> 自 10.9 起返回类型一直是 <c>QueryResult&lt;BaseItem&gt;</c>，
    /// 跨版本稳定，因此统一改用 <c>QueryItems</c> 来规避 <c>GetItemList</c> 的签名漂移问题。
    /// </para>
    /// </remarks>
    public static class LibraryManagerCompatExtensions
    {
        /// <summary>
        /// 查询媒体库条目，返回稳定的 <see cref="IReadOnlyList{T}"/> 结果。
        /// </summary>
        /// <param name="libraryManager">媒体库管理器实例。</param>
        /// <param name="query">查询条件。</param>
        /// <returns>查询到的条目列表。</returns>
        public static IReadOnlyList<BaseItem> QueryItemsCompat(this ILibraryManager libraryManager, InternalItemsQuery query)
        {
            if (libraryManager is null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return libraryManager.QueryItems(query).Items ?? Array.Empty<BaseItem>();
        }
    }
}
