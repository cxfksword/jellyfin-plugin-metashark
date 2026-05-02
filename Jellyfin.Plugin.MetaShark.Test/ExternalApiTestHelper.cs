using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Jellyfin.Plugin.MetaShark.Test
{
    internal static class ExternalApiTestHelper
    {
        public static void RunOrInconclusive(Func<Task> action, params string[] hosts)
        {
            try
            {
                action().GetAwaiter().GetResult();
            }
            catch (AssertInconclusiveException)
            {
                throw;
            }
            catch (Exception ex) when (IsNetworkRelated(ex))
            {
                var hostInfo = hosts.Length > 0 ? $" ({string.Join(", ", hosts)})" : string.Empty;
                Assert.Inconclusive($"External service unavailable{hostInfo}: {ex.Message}");
            }
        }

        public static void AssertNotNullOrInconclusive<T>(T? value, string host, string message)
            where T : class
        {
            if (value != null)
            {
                return;
            }

            if (!CanResolveHost(host))
            {
                Assert.Inconclusive($"External service unavailable ({host})");
            }

            Assert.IsNotNull(value, message);
        }

        private static bool IsNetworkRelated(Exception ex)
        {
            return ex switch
            {
                HttpRequestException => true,
                SocketException => true,
                WebException => true,
                _ when ex.InnerException != null => IsNetworkRelated(ex.InnerException),
                _ => false
            };
        }

        private static bool CanResolveHost(string host)
        {
            try
            {
                return Dns.GetHostAddressesAsync(host).GetAwaiter().GetResult().Length > 0;
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
