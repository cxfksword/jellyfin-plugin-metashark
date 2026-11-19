using System.Collections.Generic;
using Jellyfin.Plugin.MetaShark.Model;
using Jellyfin.Plugin.MetaShark.Providers;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class Issue136Test
    {
        // issue #136: 文件名 Split.2016，豆瓣记为 2017（影展点映跨年），片名相同应允许 ±1 年
        [TestMethod]
        public void SelectSameNameWithinOneYear_ShouldMatchSameNameOneYearOff()
        {
            var candidates = new List<DoubanSubject>
            {
                new DoubanSubject { Name = "分裂", Year = 2017, Category = "电影", Sid = "split2017" },
            };

            Assert.AreEqual("split2017", BaseProvider.SelectSameNameWithinOneYear(candidates, "分裂", 2016, "电影")?.Sid);
            Assert.AreEqual("split2017", BaseProvider.SelectSameNameWithinOneYear(candidates, "分裂", 2018, "电影")?.Sid);
        }

        // 差 2 年及以上不放宽，翻拍（如 Mulan 1998 / 2020）仍能区分
        [TestMethod]
        public void SelectSameNameWithinOneYear_ShouldNotMatchTwoOrMoreYearsOff()
        {
            var candidates = new List<DoubanSubject>
            {
                new DoubanSubject { Name = "花木兰", Year = 2020, Category = "电影", Sid = "mulan2020" },
            };

            Assert.IsNull(BaseProvider.SelectSameNameWithinOneYear(candidates, "花木兰", 1998, "电影"));
            Assert.IsNull(BaseProvider.SelectSameNameWithinOneYear(candidates, "花木兰", 2018, "电影"));
        }

        // 仅在片名完全相同时放宽，避免跨年误配到其他条目
        [TestMethod]
        public void SelectSameNameWithinOneYear_ShouldRequireExactName()
        {
            var candidates = new List<DoubanSubject>
            {
                new DoubanSubject { Name = "分裂人格", Year = 2017, Category = "电影", Sid = "other" },
                new DoubanSubject { Name = "分裂", Year = 2017, Category = "电影", Sid = "split2017" },
            };

            Assert.AreEqual("split2017", BaseProvider.SelectSameNameWithinOneYear(candidates, "分裂", 2016, "电影")?.Sid);
        }

        // 搜索页需匹配类别；suggest 无类别信息时传 null 不过滤
        [TestMethod]
        public void SelectSameNameWithinOneYear_ShouldRespectCategoryWhenGiven()
        {
            var candidates = new List<DoubanSubject>
            {
                new DoubanSubject { Name = "分裂", Year = 2017, Category = "电视剧", Sid = "tv" },
            };

            Assert.IsNull(BaseProvider.SelectSameNameWithinOneYear(candidates, "分裂", 2016, "电影"));
            Assert.AreEqual("tv", BaseProvider.SelectSameNameWithinOneYear(candidates, "分裂", 2016, null)?.Sid);
        }

        [TestMethod]
        public void SelectSameNameWithinOneYear_ShouldSkipInvalidCandidatesAndInputs()
        {
            var candidates = new List<DoubanSubject>
            {
                new DoubanSubject { Name = "分裂", Year = 2017, Category = "电影", Sid = "" },
                new DoubanSubject { Name = "分裂", Year = 0, Category = "电影", Sid = "noyear" },
            };

            Assert.IsNull(BaseProvider.SelectSameNameWithinOneYear(candidates, "分裂", 2016, "电影"));
            Assert.IsNull(BaseProvider.SelectSameNameWithinOneYear(candidates, "分裂", 0, "电影"));
            Assert.IsNull(BaseProvider.SelectSameNameWithinOneYear(candidates, "", 2016, "电影"));
        }
    }
}
