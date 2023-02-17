using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.MetaShark.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.MetaShark;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public const string PluginName = "MetaShark";

    /// <summary>
    /// Gets the provider id.
    /// </summary>
    public const string ProviderId = "MetaSharkID";

    protected readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IHttpContextAccessor httpContextAccessor)
        : base(applicationPaths, xmlSerializer)
    {
        this._httpContextAccessor = httpContextAccessor;

        Plugin.Instance = this;
    }

    /// <inheritdoc />
    public override string Name => PluginName;

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("9A19103F-16F7-4668-BE54-9A1E7A4F7556");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }


    /// <summary>
    /// jellyfin web服务域名
    /// 注意：经过nginx代理后，会拿不到真正的域名，需要用户配置http服务传入真正host
    /// </summary>
    public string BaseUrl
    {
        get
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                // // 使用web浏览器访问直接使用相对链接？？？可解决用户配置了http反代
                // var userAgent = _httpContextAccessor.HttpContext.Request.Headers.UserAgent.ToString();
                // var fromWeb = userAgent.Contains("Chrome") || userAgent.Contains("Safari");
                // if (fromWeb) return string.Empty;

                return _httpContextAccessor.HttpContext.Request.Scheme + System.Uri.SchemeDelimiter + _httpContextAccessor.HttpContext.Request.Host;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
