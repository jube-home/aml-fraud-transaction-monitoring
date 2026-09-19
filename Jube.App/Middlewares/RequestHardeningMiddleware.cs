/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Jube.App.Middlewares
{
    public class RequestHardeningMiddleware(RequestDelegate next, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                }

                return Task.CompletedTask;
            });

            if (ContainsNul(request.QueryString.Value) || ContainsNul(request.Path.Value))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (request.Path.Value != null
                && request.Path.Value.StartsWith("/api/Authentication/ByNegotiate", StringComparison.OrdinalIgnoreCase)
                && !dynamicEnvironment.AppSettings("NegotiateAuthentication")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var cookieOnly = request.Cookies.ContainsKey("authentication-jwt")
                             && !request.Headers.ContainsKey("Authorization")
                             && !request.Headers.ContainsKey("X-API-KEY");
            if (cookieOnly && CsrfOriginCheckEnabled()
                           && (IsStateChanging(request.Method) && !IsSameOrigin(request)
                               || IsSideEffectingGet(request) && IsCrossSiteRequest(request)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (IsStateChanging(request.Method) && IsJsonRequest(request) && !IsExemptFromNullCheck(request)
                && await IsJsonNullLiteralAsync(request))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            try
            {
                await next(context);
            }
            catch (BadHttpRequestException ex) when (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = ex.StatusCode;
            }
            catch (Exception ex) when (!context.Response.HasStarted && IsInvalidUtf8(ex))
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private bool CsrfOriginCheckEnabled()
        {
            return !"False".Equals(dynamicEnvironment.AppSettings("CsrfOriginCheck"),
                StringComparison.OrdinalIgnoreCase);
        }

        private static readonly string[] sideEffectingGetPrefixes =
        [
            "/api/ActivationWatcher/Replay", "/api/RegisterSignalrConnection", "/api/GetCaseByIdQuery",
            "/api/GetCaseBySessionCaseSearchCompileQuery", "/api/SessionCaseSearchCompiledSql",
            "/api/Invoke/EntityAnalysisModel/Callback"
        ];

        private static bool IsSideEffectingGet(HttpRequest request)
        {
            var path = request.Path.Value;
            return path != null && HttpMethods.IsGet(request.Method)
                                && sideEffectingGetPrefixes.Any(prefix =>
                                    path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsCrossSiteRequest(HttpRequest request)
        {
            var fetchSite = request.Headers["Sec-Fetch-Site"].ToString();
            return !string.IsNullOrEmpty(fetchSite)
                   && !fetchSite.Equals("same-origin", StringComparison.OrdinalIgnoreCase)
                   && !fetchSite.Equals("none", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSameOrigin(HttpRequest request)
        {
            var fetchSite = request.Headers["Sec-Fetch-Site"].ToString();
            if (!string.IsNullOrEmpty(fetchSite))
            {
                return fetchSite.Equals("same-origin", StringComparison.OrdinalIgnoreCase)
                       || fetchSite.Equals("none", StringComparison.OrdinalIgnoreCase);
            }

            var origin = request.Headers.Origin.ToString();
            if (!string.IsNullOrEmpty(origin))
            {
                return MatchesRequestHost(request, origin);
            }

            var referer = request.Headers.Referer.ToString();
            return !string.IsNullOrEmpty(referer) && MatchesRequestHost(request, referer);
        }

        private static bool MatchesRequestHost(HttpRequest request, string originOrUrl)
        {
            if (!Uri.TryCreate(originOrUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }

            var authority = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            if (authority.Equals(HostWithoutDefaultPort(request.Scheme, request.Host.Value),
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var forwardedHost = request.Headers["X-Forwarded-Host"].ToString().Split(',')[0].Trim();
            return !string.IsNullOrEmpty(forwardedHost)
                   && authority.Equals(HostWithoutDefaultPort(uri.Scheme, forwardedHost),
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string HostWithoutDefaultPort(string scheme, string host)
        {
            var defaultPort = scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? ":443" : ":80";
            return host.EndsWith(defaultPort, StringComparison.Ordinal) ? host[..^defaultPort.Length] : host;
        }

        private static bool IsInvalidUtf8(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is DecoderFallbackException)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsJsonRequest(HttpRequest request)
        {
            var contentType = request.ContentType;
            return !string.IsNullOrEmpty(contentType)
                   && (contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
                       || contentType.Contains("+json", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsExemptFromNullCheck(HttpRequest request)
        {
            var path = request.Path.Value;
            return path != null
                   && (path.StartsWith("/api/MockHttpAdaptation", StringComparison.OrdinalIgnoreCase)
                       || path.StartsWith("/api/Mfa", StringComparison.OrdinalIgnoreCase)
                       || path.StartsWith("/api/Invoke", StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<bool> IsJsonNullLiteralAsync(HttpRequest request)
        {
            const int window = 1024;
            if (request.ContentLength is > window)
            {
                return false;
            }

            request.EnableBuffering();
            var buffer = new byte[window];
            var read = 0;
            int count;
            while (read < window
                   && (count = await request.Body.ReadAsync(buffer.AsMemory(read, window - read))) > 0)
            {
                read += count;
            }

            request.Body.Position = 0;
            if (read == window)
            {
                return false;
            }

            var text = Encoding.UTF8.GetString(buffer, 0, read).Trim('\uFEFF', ' ', '\t', '\r', '\n');
            return text.Equals("null", StringComparison.Ordinal);
        }

        private static bool IsStateChanging(string method)
        {
            return HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method)
                   || HttpMethods.IsDelete(method);
        }

        private static bool ContainsNul(string value)
        {
            return !string.IsNullOrEmpty(value)
                   && (value.Contains('\0') || value.Contains("%00", StringComparison.Ordinal));
        }
    }
}