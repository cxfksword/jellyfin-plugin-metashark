using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Core
{
    public static class JsonExtension
    {
        public static string ToJson(this object obj)
        {
            if (obj == null) return string.Empty;

            return JsonSerializer.Serialize(obj);
        }
    }
}
