using System.Net;
using System.Reflection;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MetaShark.Configuration;


/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    public const int MAX_CAST_MEMBERS = 15;
    public const int MAX_SEARCH_RESULT = 5;

    /// <summary>
    /// 插件版本
    /// </summary>
    public string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    public string DoubanCookies { get; set; } = string.Empty;
    /// <summary>
    /// 豆瓣开启防封禁
    /// </summary>
    public bool EnableDoubanAvoidRiskControl { get; set; } = false;
    /// <summary>
    /// 豆瓣海报使用大图
    /// </summary>
    public bool EnableDoubanLargePoster { get; set; } = false;
    /// <summary>
    /// 豆瓣背景图使用原图
    /// </summary>
    public bool EnableDoubanBackdropRaw { get; set; } = false;
    /// <summary>
    /// 豆瓣图片代理地址
    /// </summary>
    public string DoubanImageProxyBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 启用获取tmdb元数据
    /// </summary>
    public bool EnableTmdb { get; set; } = true;

    /// <summary>
    /// 启用显示tmdb搜索结果
    /// </summary>
    public bool EnableTmdbSearch { get; set; } = false;

    /// <summary>
    /// 启用tmdb获取背景图
    /// </summary>
    public bool EnableTmdbBackdrop { get; set; } = true;

    /// <summary>
    /// 启用tmdb获取商标
    /// </summary>
    public bool EnableTmdbLogo { get; set; } = true;
    
    /// <summary>
    /// 是否根据电影系列自动创建合集
    /// </summary>
    public bool EnableTmdbCollection { get; set; } = true;
    /// <summary>
    /// 是否获取tmdb分级信息
    /// </summary>
    public bool EnableTmdbOfficialRating { get; set; } = true;
    /// <summary>
    /// tmdb api key
    /// </summary>
    public string TmdbApiKey { get; set; } = string.Empty;
    /// <summary>
    /// tmdb api host
    /// </summary>
    public string TmdbHost { get; set; } = string.Empty;
    /// <summary>
    /// 代理服务器类型，0-禁用，1-http，2-https，3-socket5
    /// </summary>
    public string TmdbProxyType { get; set; } = string.Empty;
    /// <summary>
    /// 代理服务器host
    /// </summary>
    public string TmdbProxyPort { get; set; } = string.Empty;
    /// <summary>
    /// 代理服务器端口
    /// </summary>
    public string TmdbProxyHost { get; set; } = string.Empty;


    public IWebProxy GetTmdbWebProxy()
    {

        if (!string.IsNullOrEmpty(TmdbProxyType))
        {
            return new WebProxy($"{TmdbProxyType}://{TmdbProxyHost}:{TmdbProxyPort}", true);
        }

        return null;
    }
}
