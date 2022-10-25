
using Jellyfin.Plugin.MetaShark.Core;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class StringSimilarityTest
    {
        [TestMethod]
        public void TestString()
        {

            var str1 = "雄狮少年";
            var str2 = "我是特优声 剧团季";

            var score = str1.Distance(str2);

            str1 = "雄狮少年";
            str2 = "雄狮少年 第二季";

            score = str1.Distance(str2);

            var score2 = "君子和而不同".Distance("小人同而不和");

            Assert.IsTrue(score > 0);
        }
    }
}
