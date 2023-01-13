using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.MetaShark.Core;

namespace Jellyfin.Plugin.MetaShark.Model;

public class DoubanSuggest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;


    public string Sid
    {
        get
        {
            var regSid = new Regex(@"subject\/(\d+?)\/", RegexOptions.Compiled);
            return this.Url.GetMatchGroup(regSid);
        }
    }
}
