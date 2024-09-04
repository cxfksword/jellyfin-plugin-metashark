using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.MetaShark.Providers
{
    public static class EnumerableExtensions
    {
        private const int MaxPriority = 99;

        public static IEnumerable<RemoteImageInfo> OrderByLanguageDescending(this IEnumerable<RemoteImageInfo> remoteImageInfos, params string[] requestedLanguages)
        {
            if (requestedLanguages.Length <= 0)
            {
                requestedLanguages = new[] { "en" };
            }

            var requestedLanguagePriorityMap = new Dictionary<string, int>();
            for (int i = 0; i < requestedLanguages.Length; i++)
            {
                if (string.IsNullOrEmpty(requestedLanguages[i]))
                {
                    continue;
                }
                requestedLanguagePriorityMap.Add(NormalizeLanguage(requestedLanguages[i]), MaxPriority - i);
            }

            return remoteImageInfos.OrderByDescending(delegate (RemoteImageInfo i)
            {
                if (string.IsNullOrEmpty(i.Language))
                {
                    return 3;
                }

                if (requestedLanguagePriorityMap.TryGetValue(NormalizeLanguage(i.Language), out int priority))
                {
                    return priority;
                }

                return string.Equals(i.Language, "en", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
            }).ThenByDescending((RemoteImageInfo i) => i.CommunityRating.GetValueOrDefault()).ThenByDescending((RemoteImageInfo i) => i.VoteCount.GetValueOrDefault());
        }

        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return language;
            }

            return language.Split('-')[0].ToLower();
        }
    }
}