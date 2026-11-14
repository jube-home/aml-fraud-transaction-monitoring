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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Jube.Cache.Resp.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;

namespace Jube.App.Wire
{
    public sealed class WireEncryptionMiddleware(
        RequestDelegate next,
        WireCipher cipher,
        ICacheWireReplayRepository replay,
        TimeProvider clock)
    {
        public const int SkewSeconds = 300;
        private const int MaximumWrappedKeyBytes = 1024;
        private const long MaximumBodyBytes = 64L * 1024 * 1024;

        private static long LimitFor(HttpContext context)
        {
            return Math.Max(MaximumBodyBytes,
                context.GetEndpoint()?.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize ?? 0);
        }

        private static readonly string[] InnerTypes = [WireMediaType.Json, WireMediaType.MessagePack];

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            if (context.User.Identity?.IsAuthenticated != true
                || !request.Headers.TryGetValue(WireMediaType.KeyHeader, out var keyValues))
            {
                await next(context);

                return;
            }

            var time = request.Headers[WireMediaType.TimeHeader].ToString();

            if (keyValues.Count != 1
                || !TryDecode(keyValues[0], out var wrapped)
                || wrapped.Length > MaximumWrappedKeyBytes
                || !long.TryParse(time, out var sent)
                || Math.Abs(clock.GetUtcNow().ToUnixTimeMilliseconds() - sent) > SkewSeconds * 1000L)
            {
                await Reject(context);

                return;
            }

            var key = cipher.Unwrap(wrapped);

            if (key == null)
            {
                await Reject(context);

                return;
            }

            if (WireMediaType.IsEncrypted(request.ContentType))
            {
                if (!await TryOpenBodyAsync(context, key, time))
                {
                    await Reject(context);

                    return;
                }
            }

            if (!await replay.TryClaimAsync(Fingerprint(wrapped), TimeSpan.FromSeconds(SkewSeconds * 2)))
            {
                await Reject(context);

                return;
            }

            var original = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await next(context);
            }
            finally
            {
                context.Response.Body = original;
            }

            await SealResponseAsync(context, buffer, original, key, time);
        }

        private static async Task Reject(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            await Task.CompletedTask;
        }

        private static async Task<bool> TryOpenBodyAsync(HttpContext context, byte[] key, string time)
        {
            var request = context.Request;
            var inner = request.Headers[WireMediaType.InnerHeader].ToString();

            if (!InnerTypes.Contains(inner, StringComparer.OrdinalIgnoreCase)
                || request.ContentLength is null || request.ContentLength > LimitFor(context))
            {
                return false;
            }

            using var body = new MemoryStream();
            await request.Body.CopyToAsync(body, context.RequestAborted);

            byte[] plaintext;

            try
            {
                plaintext = WireCipher.Open(key, body.ToArray(),
                    WireCipher.RequestAssociatedData(request.Method, TargetOf(context), time, inner));
            }
            catch (CryptographicException)
            {
                return false;
            }

            request.Body = new MemoryStream(plaintext);
            request.ContentLength = plaintext.Length;
            request.ContentType = inner;

            return true;
        }

        private static async Task SealResponseAsync(HttpContext context, MemoryStream buffer, Stream original,
            byte[] key, string time)
        {
            var response = context.Response;

            if (response.HasStarted || buffer.Length == 0
                                    || response.StatusCode is StatusCodes.Status204NoContent
                                        or StatusCodes.Status205ResetContent
                                        or StatusCodes.Status304NotModified)
            {
                await buffer.CopyToAsync(original, context.RequestAborted);

                return;
            }

            var inner = response.ContentType ?? "application/octet-stream";
            var sealedData = WireCipher.Seal(key, buffer.ToArray(),
                WireCipher.ResponseAssociatedData(time, response.StatusCode, inner));

            response.ContentType = WireMediaType.Encrypted;
            response.Headers[WireMediaType.InnerHeader] = inner;
            response.ContentLength = sealedData.Length;

            await original.WriteAsync(sealedData, context.RequestAborted);
        }

        private static string TargetOf(HttpContext context)
        {
            var raw = context.Features.Get<IHttpRequestFeature>()?.RawTarget;

            return string.IsNullOrEmpty(raw)
                ? context.Request.Path.Value + context.Request.QueryString.Value
                : raw;
        }

        private static bool TryDecode(string value, out byte[] bytes)
        {
            bytes = null;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            try
            {
                bytes = Convert.FromBase64String(value);

                return bytes.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string Fingerprint(byte[] wrapped)
        {
            return Convert.ToHexString(SHA256.HashData(wrapped));
        }
    }
}
