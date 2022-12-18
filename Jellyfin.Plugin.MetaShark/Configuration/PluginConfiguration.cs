using MediaBrowser.Model.Plugins;
using System.Net;
using System.Reflection;

namespace Jellyfin.Plugin.MetaShark.Configuration;

/// <summary>
/// The configuration options.
/// </summary>
public enum SomeOptions
{
    /// <summary>
    /// Option one.
    /// </summary>
    OneOption,

    /// <summary>
    /// Second option.
    /// </summary>
    AnotherOption
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    public string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    public bool EnableTmdb { get; set; } = true;

    public bool EnableTmdbSearch { get; set; } = false;

    public string TmdbApiKey { get; set; } = string.Empty;

    public string TmdbHost { get; set; } = string.Empty;

    public string DoubanCookies { get; set; } = string.Empty;

    public int MaxCastMembers { get; set; } = 15;

    public int MaxSearchResult { get; set; } = 3;



}
