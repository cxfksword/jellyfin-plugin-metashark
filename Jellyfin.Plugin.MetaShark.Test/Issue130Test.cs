using Jellyfin.Plugin.MetaShark.Api;
using Jellyfin.Plugin.MetaShark.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class Issue130Test
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                }));

        [TestMethod]
        public void GetTitle_ShouldDecodeHtmlEntities()
        {
            var api = new DoubanApi(loggerFactory);

            // 豆瓣 keywords meta 中的单引号会被 HTML 转义（如 That's -> That&#39;s）
            var body = "<html><head><meta name=\"keywords\" content=\"随兴旅 -That&#39;s Journey-,2025\" /></head></html>";
            Assert.AreEqual("随兴旅 -That's Journey-", api.GetTitle(body));

            body = "<html><head><title>随兴旅 -That&#39;s Journey- (豆瓣)</title></head></html>";
            Assert.AreEqual("随兴旅 -That's Journey-", api.GetTitle(body));
        }

        [TestMethod]
        public void RemoveSeasonSuffix_ShouldKeepTrailingNumberAsTitle()
        {
            var provider = new TestableBaseProvider(loggerFactory);

            // issue #130：标题自带的尾数（第X季/Season X 除外）不应被删除
            Assert.AreEqual("命运石之门 0", provider.ExposedRemoveSeasonSuffix("命运石之门 0"));
            Assert.AreEqual("命运石之门0", provider.ExposedRemoveSeasonSuffix("命运石之门0"));
            Assert.AreEqual("ID-0", provider.ExposedRemoveSeasonSuffix("ID-0"));
            Assert.AreEqual("随兴旅 -That's Journey-", provider.ExposedRemoveSeasonSuffix("随兴旅 -That's Journey-"));
            Assert.AreEqual("神探狄仁杰2", provider.ExposedRemoveSeasonSuffix("神探狄仁杰2"));
            Assert.AreEqual("白色相簿2", provider.ExposedRemoveSeasonSuffix("白色相簿2"));

            // 真正的季后缀仍需去除
            Assert.AreEqual("向往的生活", provider.ExposedRemoveSeasonSuffix("向往的生活 第2季"));
            Assert.AreEqual("Bright Future", provider.ExposedRemoveSeasonSuffix("Bright Future Season 2"));
        }

        private sealed class TestableBaseProvider : BaseProvider
        {
            public TestableBaseProvider(ILoggerFactory loggerFactory)
                : base(new DefaultHttpClientFactory(), loggerFactory.CreateLogger<TestableBaseProvider>(), null!, null!, null!, null!, null!, null!)
            {
            }

            public string ExposedRemoveSeasonSuffix(string name) => RemoveSeasonSuffix(name);
        }
    }
}
