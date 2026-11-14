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

using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Jube.Data.Concurrency;
using Microsoft.AspNetCore.Http;

namespace Jube.App.Middlewares
{
    public class VersionConflictMiddleware(RequestDelegate next)
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (VersionConflictException ex) when (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = MediaTypeNames.Application.Json;

                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    code = "VersionConflict",
                    message = ex.Message,
                    suppliedVersion = ex.SuppliedVersion,
                    currentVersion = ex.CurrentVersion
                }, Options));
            }
        }
    }
}
