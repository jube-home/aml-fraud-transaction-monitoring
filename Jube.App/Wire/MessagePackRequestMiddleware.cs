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
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;

namespace Jube.App.Wire
{
    public class MessagePackRequestMiddleware(
        RequestDelegate next,
        WireTypeRegistry registry,
        IOptions<JsonOptions> jsonOptions)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            if (!WireMediaType.IsMessagePack(request.ContentType))
            {
                await next(context);

                return;
            }

            if (registry.Mode != WireMode.MessagePack)
            {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;

                return;
            }

            var accepts = context.GetEndpoint()?.Metadata.GetMetadata<IAcceptsMetadata>();

            if (accepts?.RequestType == null
                || !accepts.ContentTypes.Any(c => c.Contains("json", StringComparison.OrdinalIgnoreCase))
                || registry.ShapeOf(accepts.RequestType) == null)
            {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;

                return;
            }

            if (context.GetEndpoint()?.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize is { } limit
                && context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } sizeFeature)
            {
                sizeFeature.MaxRequestBodySize = limit;
            }

            using var buffer = new MemoryStream();

            try
            {
                await request.Body.CopyToAsync(buffer, context.RequestAborted);
            }
            catch (BadHttpRequestException exception)
            {
                context.Response.StatusCode = exception.StatusCode;

                return;
            }

            object body;

            try
            {
                body = MessagePackSerializer.Deserialize(accepts.RequestType, buffer.ToArray(), WireSerializer.Options,
                    context.RequestAborted);
            }
            catch (MessagePackSerializationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                return;
            }

            if (body == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                return;
            }

            var json = JsonSerializer.SerializeToUtf8Bytes(body, accepts.RequestType,
                jsonOptions.Value.SerializerOptions);

            request.Body = new MemoryStream(json);
            request.ContentLength = json.Length;
            request.ContentType = "application/json";

            await next(context);
        }
    }
}
