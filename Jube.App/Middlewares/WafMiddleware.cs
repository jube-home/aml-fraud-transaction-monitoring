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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Jube.App.Code.Waf;
using Jube.App.Code.Waf.Models;
using log4net;
using Microsoft.AspNetCore.Http;

namespace Jube.App.Middlewares
{
    public class WafMiddleware(
        RequestDelegate next,
        WafInspector inspector,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsEnabled())
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var fields = new List<WafFieldValue>();
            AddPathAndQuery(context.Request, fields);

            if (!context.WebSockets.IsWebSocketRequest && IsInspectableBody(context.Request))
            {
                await AddBodyAsync(context.Request, fields).ConfigureAwait(false);
            }

            var request = new WafInspectionRequest(
                context.WebSockets.IsWebSocketRequest ? "WebSocket" : "Http",
                context.Request.Path.Value ?? string.Empty,
                context.Request.Method,
                context.Connection.RemoteIpAddress?.ToString(),
                context.User.Identity?.Name,
                context.TraceIdentifier,
                fields);

            WafInspectionResult result;
            try
            {
                result = inspector.Inspect(request);
            }
            catch (Exception ex)
            {
                log.Error($"Waf: Inspection raised an error and the request has been allowed to continue as {ex}.");
                await next(context).ConfigureAwait(false);
                return;
            }

            if (result.Blocked && !context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await next(context).ConfigureAwait(false);
        }

        private static void AddPathAndQuery(HttpRequest request, List<WafFieldValue> fields)
        {
            if (request.Path.HasValue)
            {
                fields.Add(new WafFieldValue("$path", request.Path.Value, WafTargetScope.Path));
            }

            foreach (var pair in request.Query)
            {
                foreach (var value in pair.Value)
                {
                    fields.Add(new WafFieldValue("$query." + pair.Key, value, WafTargetScope.Query));
                }
            }
        }

        private async Task AddBodyAsync(HttpRequest request, List<WafFieldValue> fields)
        {
            var maxBytes = MaxInspectBytes();
            if (request.ContentLength is { } length && length > maxBytes)
            {
                return;
            }

            request.EnableBuffering();
            try
            {
                var bufferSize = request.ContentLength is { } knownLength
                    ? (int)Math.Min(knownLength, maxBytes)
                    : maxBytes;
                var buffer = new byte[bufferSize];
                var read = 0;
                int count;
                while (read < bufferSize
                       && (count = await request.Body.ReadAsync(buffer.AsMemory(read, bufferSize - read))
                           .ConfigureAwait(false)) > 0)
                {
                    read += count;
                }

                if (read == 0)
                {
                    return;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, read);
                fields.AddRange(JsonBodyFlattener.Flatten(json));
            }
            finally
            {
                request.Body.Position = 0;
            }
        }

        private static bool IsInspectableBody(HttpRequest request)
        {
            if (request.ContentLength is 0)
            {
                return false;
            }

            var contentType = request.ContentType;
            return !string.IsNullOrEmpty(contentType)
                   && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsEnabled()
        {
            return "True".Equals(dynamicEnvironment.AppSettings("WafEnabled"), StringComparison.OrdinalIgnoreCase);
        }

        private int MaxInspectBytes()
        {
            return int.TryParse(dynamicEnvironment.AppSettings("WafMaxInspectBytes"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var value) && value > 0
                ? value
                : 262144;
        }
    }
}