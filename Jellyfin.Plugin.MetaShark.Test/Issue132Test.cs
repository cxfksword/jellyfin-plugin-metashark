using System.Collections.Generic;
using Jellyfin.Plugin.MetaShark.Model;
using Jellyfin.Plugin.MetaShark.Providers;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class Issue132Test
    {
        // issue #132: 搜索结果中同年份有多季时，取第一个会取错，应跳过季数不匹配的候选
        [TestMethod]
        public void SelectSeasonCandidateByYear_ShouldSkipMismatchedSeasonAndReturnMatching()
        {
            var candidates = new List<DoubanSubject>
            {
                new DoubanSubject { Name = "千谎百计 第一季", Year = 2009, Sid = "3103678" },
                new DoubanSubject { Name = "千谎百计", Year = 2008, Sid = "2371997" },
                new DoubanSubject { Name = "千谎百计  第二季", Year = 2009, Sid = "3882839" },
                new DoubanSubject { Name = "千谎百计  第三季", Year = 2010, Sid = "4848610" },
            };

            var sameYear = candidates.FindAll(x => x.Year == 2009);

            var season2 = BaseProvider.SelectSeasonCandidateByYear(sameYear, 2);
            Assert.AreEqual("3882839", season2?.Sid);

            var season1 = BaseProvider.SelectSeasonCandidateByYear(sameYear, 1);
            Assert.AreEqual("3103678", season1?.Sid);
        }

        [TestMethod]
        public void SelectSeasonCandidateByYear_ShouldReturnFirstWhenNoSeasonNumberParsed()
        {
            var candidates = new List<DoubanSubject>
            {
                new DoubanSubject { Name = "千谎百计", Year = 2008, Sid = "2371997" },
            };

            var item = BaseProvider.SelectSeasonCandidateByYear(candidates, 1);
            Assert.AreEqual("2371997", item?.Sid);
        }

        [TestMethod]
        public void SelectSeasonCandidateByYear_ShouldReturnNullWhenNoCandidates()
        {
            var item = BaseProvider.SelectSeasonCandidateByYear(new List<DoubanSubject>(), 2);
            Assert.IsNull(item);
        }

        // issue #132: 豆瓣标题中偶尔出现连续空格（如 "千谎百计  第二季"），比较前应先折叠空白
        [TestMethod]
        public void IsMatchingSeasonName_ShouldIgnoreExtraInternalWhitespace()
        {
            Assert.IsTrue(BaseProvider.IsMatchingSeasonName("千谎百计  第二季", "千谎百计2", "千谎百计 第二季"));
            Assert.IsTrue(BaseProvider.IsMatchingSeasonName("千谎百计 第二季", "千谎百计2", "千谎百计 第二季"));
            Assert.IsFalse(BaseProvider.IsMatchingSeasonName("千谎百计 第三季", "千谎百计2", "千谎百计 第二季"));
        }

        [TestMethod]
        public void NormalizeWhitespace_ShouldCollapseInternalWhitespaceAndTrim()
        {
            Assert.AreEqual("千谎百计 第二季", BaseProvider.NormalizeWhitespace("千谎百计  第二季"));
            Assert.AreEqual("千谎百计 第二季", BaseProvider.NormalizeWhitespace("  千谎百计   第二季  "));
            Assert.AreEqual("千谎百计", BaseProvider.NormalizeWhitespace("千谎百计"));
        }
    }
}
