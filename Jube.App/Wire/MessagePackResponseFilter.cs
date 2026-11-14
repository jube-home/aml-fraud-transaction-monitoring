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
using System.Text.Json;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jube.App.Wire
{
    public sealed class MessagePackResponseFilter(WireTypeRegistry registry) : IEndpointFilter
    {
        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            var result = await next(context);
            var request = context.HttpContext.Request;
            var messagePack = registry.Mode != WireMode.Json && WireMediaType.AcceptsMessagePack(request);
            var columnarJson = WireMediaType.AcceptsColumnarJson(request);

            if (!messagePack && !columnarJson)
            {
                return result;
            }

            if (!TryUnwrap(result, out var value, out var statusCode)
                || statusCode is < 200 or >= 300 or 204 or 205)
            {
                return result;
            }

            var type = value.GetType();
            var shape = registry.ShapeOf(type);

            if (shape == null)
            {
                return result;
            }

            if (messagePack && (registry.Mode != WireMode.Mixed || registry.CarriesRows(shape)))
            {
                try
                {
                    var body = MessagePackSerializer.Serialize(type, value,
                        registry.Compression == WireCompression.Lz4
                            ? WireSerializer.Lz4Options
                            : WireSerializer.Options);

                    return new MessagePackHttpResult(body, statusCode, shape, registry.Hash,
                        compression: WireSerializer.IsLz4Block(body) ? WireMediaType.Lz4 : null);
                }
                catch (MessagePackSerializationException)
                {
                    return result;
                }
            }

            if (columnarJson && registry.CarriesRows(shape))
            {
                var json = context.HttpContext.RequestServices?.GetService<IOptions<JsonOptions>>()?.Value
                    .SerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

                return new MessagePackHttpResult(ColumnarJson.Serialize(value, type, json), statusCode, shape,
                    registry.Hash, WireMediaType.ColumnarJson);
            }

            return result;
        }

        private static bool TryUnwrap(object result, out object value, out int statusCode)
        {
            value = null;
            statusCode = StatusCodes.Status200OK;

            switch (result)
            {
                case null:
                case string:
                    return false;
                case IValueHttpResult valueResult:
                    if (result is IContentTypeHttpResult { ContentType: not null } typed
                        && !typed.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (valueResult.Value == null)
                    {
                        return false;
                    }

                    value = valueResult.Value;
                    statusCode = (result as IStatusCodeHttpResult)?.StatusCode ?? StatusCodes.Status200OK;

                    return true;
                case IResult:
                    return false;
                default:
                    value = result;

                    return true;
            }
        }
    }
}
