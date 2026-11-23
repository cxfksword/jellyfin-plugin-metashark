using Jellyfin.Plugin.MetaShark.Controllers;

namespace Jellyfin.Plugin.MetaShark.Test
{
    [TestClass]
    public class Issue138Test
    {
        // issue #138: /plugin/metashark/proxy/image 存在未授权 SSRF，需要限制为豆瓣图床域名并只回传图片类型

        [DataTestMethod]
        [DataRow("https://img1.doubanio.com/view/photo/m/public/p2893270877.jpg")]
        [DataRow("https://img9.doubanio.com/view/photo/s_ratio_poster/public/p2614949805.webp")]
        [DataRow("http://img2.doubanio.com/view/celebrity/raw/public/p1598199472.61.jpg")]
        [DataRow("https://doubanio.com/view/photo/m/public/p1.jpg")]
        [DataRow("https://IMG1.DOUBANIO.COM/view/photo/m/public/p1.jpg")]
        public void IsAllowedDoubanImageUrl_ShouldAcceptDoubanImageHosts(string url)
        {
            Assert.IsTrue(ApiController.IsAllowedDoubanImageUrl(url));
        }

        [DataTestMethod]
        [DataRow("http://127.0.0.1:8096/System/Info/Public")]
        [DataRow("http://192.168.1.1/")]
        [DataRow("http://169.254.169.254/latest/meta-data/")]
        [DataRow("http://2130706433:18080/t.png")]
        [DataRow("http://0x7f000001:18080/t.png")]
        [DataRow("file:///etc/passwd")]
        [DataRow("127.0.0.1:18080/t.png")]
        [DataRow("https://evil.com/")]
        [DataRow("https://doubanio.com.evil.com/t.png")]
        [DataRow("https://notdoubanio.com/t.png")]
        [DataRow("https://img1.doubanio.com.cn/t.png")]
        [DataRow("")]
        [DataRow(null)]
        public void IsAllowedDoubanImageUrl_ShouldRejectOtherTargets(string url)
        {
            Assert.IsFalse(ApiController.IsAllowedDoubanImageUrl(url));
        }

        [DataTestMethod]
        [DataRow("image/jpeg")]
        [DataRow("image/png")]
        [DataRow("image/webp")]
        [DataRow("IMAGE/PNG")]
        public void IsImageContentType_ShouldAcceptImageTypes(string mediaType)
        {
            Assert.IsTrue(ApiController.IsImageContentType(mediaType));
        }

        [DataTestMethod]
        [DataRow("text/html")]
        [DataRow("application/json")]
        [DataRow("text/plain")]
        [DataRow("")]
        [DataRow(null)]
        public void IsImageContentType_ShouldRejectNonImageTypes(string mediaType)
        {
            Assert.IsFalse(ApiController.IsImageContentType(mediaType));
        }
    }
}
