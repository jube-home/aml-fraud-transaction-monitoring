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
using System.Threading.Tasks;
using Jube.App.Code;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace Jube.App.Middlewares
{
    public class ClientBuildAssetMiddleware(RequestDelegate next, ClientAssetStore store,
        IWebHostEnvironment webHostEnvironment)
    {
        private static readonly FileExtensionContentTypeProvider ContentTypes = new();

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)
                || request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
                || !request.Cookies.TryGetValue(ClientAssetStore.BuildCookieName, out var cookieBuild)
                || !ClientAssetStore.IsBuildId(cookieBuild)
                || cookieBuild == ClientBuildId.Read(webHostEnvironment.WebRootFileProvider))
            {
                await next(context);

                return;
            }

            var path = request.Path.Value?.TrimStart('/') ?? string.Empty;

            if (!ContentTypes.TryGetContentType(path, out var contentType))
            {
                await next(context);

                return;
            }

            var bytes = await store.TryGetAsync(cookieBuild, path);

            if (bytes == null)
            {
                await next(context);

                return;
            }

            context.Response.ContentType = contentType;
            context.Response.Headers.CacheControl = ClientAssetStore.IsHashedName(path)
                ? "public, max-age=31536000, immutable"
                : "no-cache";
            context.Response.ContentLength = bytes.Length;

            if (HttpMethods.IsHead(request.Method))
            {
                return;
            }

            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
        }
    }
}
