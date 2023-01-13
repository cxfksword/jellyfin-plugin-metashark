using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaShark.Api.Http
{
    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> _logger;
        public LoggingHandler(HttpMessageHandler innerHandler, ILoggerFactory loggerFactory)
            : base(innerHandler)
        {
            _logger = loggerFactory.CreateLogger<LoggingHandler>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogInformation((request.RequestUri?.ToString() ?? string.Empty));

            return await base.SendAsync(request, cancellationToken);
        }
    }

}