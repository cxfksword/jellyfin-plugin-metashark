using System;
using System.Net;

namespace TMDbLib.Utilities
{
    /// <summary>
    /// Represents a Web Proxy to use for TMDb API Requests.
    /// </summary>
    /// <remarks>
    /// This is a very simple implementation of a Web Proxy to be used when requesting data from TMDb API.
    /// It does not support proxy bypassing or multi-proxy configuration based on the destination URL, for instance.
    /// </remarks>
    public class TMDbAPIProxy : IWebProxy
    {
        private readonly Uri _proxyUri;

        /// <summary>
        /// Initializes a new instance for this Proxy
        /// </summary>
        public TMDbAPIProxy(Uri proxyUri, ICredentials credentials = null)
        {
            if (proxyUri == null)
                throw new ArgumentNullException(nameof(proxyUri));

            _proxyUri = proxyUri;
            Credentials = credentials;
        }

        /// <summary>
        /// Gets or sets the credentials to use for authenticating in the proxy server.
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// Gets the proxy server <see cref="Uri"/> to be used when accessing <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The destination URL to be accessed.</param>
        /// <returns></returns>
        public Uri GetProxy(Uri destination)
        {
            return _proxyUri;
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }
    }
}
