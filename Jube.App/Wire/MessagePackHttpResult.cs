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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Jube.App.Wire
{
    public sealed class MessagePackHttpResult(
        byte[] body,
        int statusCode,
        string shape,
        string schemaHash,
        string contentType = WireMediaType.MessagePack,
        string compression = null) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var response = httpContext.Response;

            response.StatusCode = statusCode;
            response.ContentType = contentType;
            response.ContentLength = body.Length;
            response.Headers[WireMediaType.ShapeHeader] = shape;
            response.Headers[WireMediaType.SchemaHeader] = schemaHash;

            if (compression != null)
            {
                response.Headers[WireMediaType.CompressionHeader] = compression;
            }

            response.Headers.Vary = StringValues.Concat(response.Headers.Vary, "Accept");

            await response.Body.WriteAsync(body);
        }
    }
}
