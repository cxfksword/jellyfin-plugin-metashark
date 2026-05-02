using Jellyfin.Plugin.MetaShark.Api;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class DoubanApiCookieParsingTest
    {
        [TestMethod]
        public void ParseConfiguredCookies_ShouldKeepEqualsCharactersInCookieValue()
        {
            var cookies = DoubanApi.ParseConfiguredCookies("bid=abc==; ll=\"118318\"; __utma=value=with=equals");

            Assert.AreEqual(3, cookies.Count);
            Assert.AreEqual("abc==", cookies[0].Value);
            Assert.AreEqual("\"118318\"", cookies[1].Value);
            Assert.AreEqual("value=with=equals", cookies[2].Value);
        }

        [TestMethod]
        public void IsLoginRedirectUrl_ShouldDetectKnownLoggedOutUrls()
        {
            Assert.IsTrue(DoubanApi.IsLoginRedirectUrl("https://accounts.douban.com/passport/login?redir=https%3A%2F%2Fwww.douban.com%2Fmine%2F"));
            Assert.IsTrue(DoubanApi.IsLoginRedirectUrl("https://sec.douban.com/c"));
            Assert.IsFalse(DoubanApi.IsLoginRedirectUrl("https://www.douban.com/mine/"));
        }
    }
}
