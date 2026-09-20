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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;

namespace Jube.App.Middlewares
{
    public class EmptyBodyGuardMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var endpointMetadata = context.GetEndpoint()?.Metadata;

            if (endpointMetadata?.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize is { } maxRequestBodySize
                && context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } sizeFeature)
            {
                sizeFeature.MaxRequestBodySize = maxRequestBodySize;
            }

            var accepts = endpointMetadata?.GetMetadata<IAcceptsMetadata>();
            if (accepts != null
                && accepts.ContentTypes.Any(c => c.Contains("json", StringComparison.OrdinalIgnoreCase))
                && (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method)
                                                       || HttpMethods.IsPatch(request.Method) ||
                                                       HttpMethods.IsDelete(request.Method))
                && request.ContentLength == 0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (accepts != null
                && accepts.ContentTypes.Any(c => c.Contains("json", StringComparison.OrdinalIgnoreCase))
                && (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method)
                                                       || HttpMethods.IsPatch(request.Method))
                && (request.ContentLength is null || request.ContentLength > 0))
            {
                if (request.ContentLength > MaxInspectedBodyBytes)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    return;
                }

                request.EnableBuffering(bufferThreshold: 30 * 1024, bufferLimit: MaxInspectedBodyBytes + 32 * 1024L);
                using var memory = new MemoryStream(request.ContentLength.HasValue
                    ? (int)request.ContentLength.Value
                    : 0);
                var chunk = new byte[16 * 1024];
                int count;
                while ((count = await request.Body.ReadAsync(chunk.AsMemory(), context.RequestAborted)) > 0)
                {
                    if (memory.Length + count > MaxInspectedBodyBytes)
                    {
                        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                        return;
                    }

                    await memory.WriteAsync(chunk.AsMemory(0, count), context.RequestAborted);
                }

                request.Body.Position = 0;
                var body = memory.GetBuffer().AsSpan(0, (int)memory.Length);
                if (request.ContentLength is null && memory.Length == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                if (ContainsNul(body) || HasNonFiniteNumber(body))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
            }

            await next(context);
        }

        private const int MaxInspectedBodyBytes = 4 * 1024 * 1024;

        private static bool HasNonFiniteNumber(ReadOnlySpan<byte> body)
        {
            try
            {
                var reader = new Utf8JsonReader(body,
                    new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.Number
                        && (!reader.TryGetDouble(out var value) || !double.IsFinite(value)))
                    {
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                //Ignored
            }

            return false;
        }

        private static bool ContainsNul(ReadOnlySpan<byte> body)
        {
            for (var i = 0; i < body.Length; i++)
            {
                switch (body[i])
                {
                    case 0:
                        return true;
                    case (byte)'\\' when i + 1 < body.Length:
                        if (body[i + 1] == (byte)'u' && i + 5 < body.Length && body[i + 2] == (byte)'0'
                            && body[i + 3] == (byte)'0' && body[i + 4] == (byte)'0' && body[i + 5] == (byte)'0')
                        {
                            return true;
                        }

                        i++;
                        break;
                }
            }

            return false;
        }
    }
}