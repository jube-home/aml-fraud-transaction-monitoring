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
using System.Threading;
using System.Threading.Tasks;
using Jube.App.Code.Watcher;
using Jube.Data.Context;
using Jube.Data.Security;
using Jube.Service.Exceptions.Query.WatcherStream;
using Jube.Service.Query.WatcherStream;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class WatcherStreamEndpoints
    {
        private const string Base = "/api/Watcher";

        public static void MapWatcherStreamEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("Watcher");

            group.MapGet("Stream", StreamAsync)
                .WithName("WatcherStream");
        }

        private static async Task StreamAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, WatcherStreamRegistry registry,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/Stream: entry user={user}");
            }

            int tenantRegistryId;
            try
            {
                await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                    dynamicEnvironment.AppSettings("ConnectionString"), log);

                var authorization = await WatcherStreamAuthorizationService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, token);

                tenantRegistryId = authorization.TenantRegistryId;
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/Stream: 403 (not authenticated) user={user}");
                }

                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/Stream: 403 user={user}");
                }

                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            var issuedMilliseconds = httpContext.User.FindFirst(TokenValidity.IssuedMillisecondsClaim)?.Value;
            var connectionId = Guid.NewGuid().ToString("N");
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            var channel = registry.Track(connectionId, user, issuedMilliseconds, tenantRegistryId,
                cancellationTokenSource);

            try
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                httpContext.Response.Headers.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers["X-Accel-Buffering"] = "no";

                await httpContext.Response.StartAsync(cancellationTokenSource.Token);
                await httpContext.Response.WriteAsync("retry: 3000\n\n", cancellationTokenSource.Token);
                await httpContext.Response.Body.FlushAsync(cancellationTokenSource.Token);

                var heartbeatIntervalSeconds =
                    int.TryParse(dynamicEnvironment.AppSettings("WatcherStreamHeartbeatIntervalSeconds"),
                        out var configuredHeartbeatSeconds) && configuredHeartbeatSeconds > 0
                        ? configuredHeartbeatSeconds
                        : 15;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var readTask = channel.Reader.WaitToReadAsync(cancellationTokenSource.Token).AsTask();
                    var heartbeatTask = Task.Delay(TimeSpan.FromSeconds(heartbeatIntervalSeconds),
                        cancellationTokenSource.Token);
                    var completed = await Task.WhenAny(readTask, heartbeatTask);

                    if (completed == heartbeatTask)
                    {
                        await httpContext.Response.WriteAsync(": keep-alive\n\n", cancellationTokenSource.Token);
                        await httpContext.Response.Body.FlushAsync(cancellationTokenSource.Token);
                        continue;
                    }

                    if (!await readTask)
                    {
                        break;
                    }

                    while (channel.Reader.TryRead(out var payload))
                    {
                        await httpContext.Response.WriteAsync($"event: activation\ndata: {payload}\n\n",
                            cancellationTokenSource.Token);
                        await httpContext.Response.Body.FlushAsync(cancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/Stream: connection {connectionId} ended user={user}");
                }
            }
            finally
            {
                var revoked = registry.IsRevoked(connectionId);
                registry.Remove(connectionId);
                channel.Writer.TryComplete();

                if (revoked)
                {
                    try
                    {
                        await httpContext.Response.WriteAsync("event: revoked\ndata: {}\n\n", CancellationToken.None);
                        await httpContext.Response.Body.FlushAsync(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        if (log.IsDebugEnabled)
                        {
                            log.Debug($"GET {Base}/Stream: could not write final revoked frame: {ex}");
                        }
                    }
                }
            }
        }
    }
}