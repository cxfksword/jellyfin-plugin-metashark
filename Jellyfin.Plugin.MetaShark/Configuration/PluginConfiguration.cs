using MediaBrowser.Model.Plugins;
using System.Reflection;

namespace Jellyfin.Plugin.MetaShark.Configuration;


/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    public const int MAX_CAST_MEMBERS = 15;
    public const int MAX_SEARCH_RESULT = 5;

    public string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    public string DoubanCookies { get; set; } = string.Empty;
    /// <summary>
    /// 豆瓣开启防封禁
    /// </summary>
    public bool EnableDoubanAvoidRiskControl { get; set; } = false;
    /// <summary>
    /// 豆瓣背景图使用原图
    /// </summary>
    public bool EnableDoubanBackdropRaw { get; set; } = false;
    /// <summary>
    /// 豆瓣图片代理地址
    /// </summary>
    public string DoubanImageProxyBaseUrl { get; set; } = string.Empty;

    public bool EnableTmdb { get; set; } = true;

    public bool EnableTmdbSearch { get; set; } = false;

    public bool EnableTmdbBackdrop { get; set; } = true;
    /// <summary>
    /// 是否获取电影系列信息
    /// </summary>
    public bool EnableTmdbCollection { get; set; } = true;
    /// <summary>
    /// 是否获取tmdb分级信息
    /// </summary>
    public bool EnableTmdbOfficialRating { get; set; } = true;

    public string TmdbApiKey { get; set; } = string.Empty;

    public string TmdbHost { get; set; } = string.Empty;

}
