using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaShark.Api
{
    // DelegatingHandler that detects Douban sec.douban.com challenge pages,
    // solves the SHA-512 nonce challenge and retries the original request once.
    public class DoubanSecHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        public DoubanSecHandler(ILogger logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Save original request target (before any redirects/rewrites by inner handlers)
            var originalRequestUri = request.RequestUri;

            // Send initial request down the handler chain
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            try
            {
                var respHost = response.RequestMessage?.RequestUri?.Host ?? string.Empty;
                if (!string.Equals(respHost, "sec.douban.com", StringComparison.OrdinalIgnoreCase))
                {
                    return response;
                }

                // Read body and detect challenge form only for sec.douban.com redirect.
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(body) && (body.Contains("name=\"cha\"", StringComparison.OrdinalIgnoreCase) || body.Contains("id=\"cha\"", StringComparison.OrdinalIgnoreCase) || body.Contains("name=\"tok\"", StringComparison.OrdinalIgnoreCase)))
                {
                    var context = BrowsingContext.New();
                    var doc = await context.OpenAsync(req => req.Content(body), cancellationToken).ConfigureAwait(false);

                    var tok = doc.QuerySelector("#tok")?.GetAttribute("value") ?? doc.QuerySelector("input[name=tok]")?.GetAttribute("value") ?? string.Empty;
                    var cha = doc.QuerySelector("#cha")?.GetAttribute("value") ?? doc.QuerySelector("input[name=cha]")?.GetAttribute("value") ?? string.Empty;
                    var diffStr = doc.QuerySelector("#difficulty")?.GetAttribute("value") ?? doc.QuerySelector("input[name=difficulty]")?.GetAttribute("value");
                    var difficulty = 4;
                    if (!string.IsNullOrEmpty(diffStr) && int.TryParse(diffStr, out var d))
                    {
                        difficulty = d;
                    }

                    if (!string.IsNullOrEmpty(cha))
                    {
                        var sol = await SolveNonceAsync(cha, difficulty, cancellationToken).ConfigureAwait(false);

                        // Prefer form action if present; otherwise use current response request URI.
                        var formEl = doc.QuerySelector("form");
                        var action = formEl?.GetAttribute("action") ?? "/c";
                        // Resolve action to absolute URI when necessary
                        var postUri = new Uri("https://sec.douban.com" + action);

                        var form = new List<KeyValuePair<string, string>>() {
                            new KeyValuePair<string, string>("tok", tok),
                            new KeyValuePair<string, string>("cha", cha),
                            new KeyValuePair<string, string>("sol", sol.ToString())
                        };

                        using (var req = new HttpRequestMessage(HttpMethod.Post, postUri))
                        {
                            req.Content = new FormUrlEncodedContent(form);
                            // set referrer to original request if available
                            if (request.RequestUri != null)
                            {
                                req.Headers.Referrer = request.RequestUri;
                            }

                            // Send the validation POST so the inner handler can store cookies
                            using var postResp = await base.SendAsync(req, cancellationToken).ConfigureAwait(false);
                        }

                        // Retry the original request once using the saved original target URI.
                        var retry = await CloneHttpRequestMessageAsync(request, cancellationToken).ConfigureAwait(false);
                        retry.RequestUri = originalRequestUri ?? request.RequestUri;
                    
                        return await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "处理 douban 验证页面失败: {0}", originalRequestUri);
            }

            return response;
        }

        private static string ComputeSha512Hex(string input)
        {
            using (var sha = SHA512.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private async Task<long> SolveNonceAsync(string data, int difficulty, CancellationToken cancellationToken)
        {
            var targetPrefix = new string('0', Math.Max(0, difficulty));
            long nonce = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                nonce++;
                var hash = ComputeSha512Hex(data + nonce.ToString());
                if (hash.StartsWith(targetPrefix, StringComparison.Ordinal))
                {
                    return nonce;
                }
                if ((nonce & 0xFFF) == 0)
                {
                    await Task.Yield();
                }
            }
        }

        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req, CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);

            // Copy the request content (if any)
            if (req.Content != null)
            {
                var ms = await req.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                var content = new ByteArrayContent(ms);
                // copy content headers
                foreach (var h in req.Content.Headers)
                {
                    content.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
                clone.Content = content;
            }

            // copy headers
            foreach (var header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // // copy properties (Options) if any
            // foreach (var prop in req.Options)
            // {
            //     clone.Options.Set(prop.Key, prop.Value);
            // }

            return clone;
        }
    }
}