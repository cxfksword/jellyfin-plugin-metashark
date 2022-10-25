using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Core
{
    public static class ElementExtension
    {
        public static string? GetText(this IElement el, string css)
        {
            var node = el.QuerySelector(css);
            if (node != null)
            {
                return node.Text().Trim();
            }

            return null;
        }

        public static string? GetAttr(this IElement el, string css, string attr)
        {
            var node = el.QuerySelector(css);
            if (node != null)
            {
                return node.GetAttribute(attr).Trim();
            }

            return null;
        }
    }
}
