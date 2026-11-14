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
using System.Diagnostics;
using System.Linq;
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
        private const int MaximumConnectionsPerUser = 10;
        private static readonly TimeSpan authorisationRecheckInterval = TimeSpan.FromSeconds(60);

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

            if (registry.Tracked().Count(c => string.Equals(c.UserName, user, StringComparison.Ordinal)) >=
                MaximumConnectionsPerUser)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/Stream: 429 too many open streams user={user}");
                }

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

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

                var recheck = Stopwatch.StartNew();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (recheck.Elapsed >= authorisationRecheckInterval)
                    {
                        recheck.Restart();

                        await using var recheckContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                            dynamicEnvironment.AppSettings("ConnectionString"), log);

                        if (!await WatcherStreamAuthorizationService.StillAuthorisedAsync(recheckContext, user,
                                tenantRegistryId, log, stringLocalizerFactory, cancellationTokenSource.Token))
                        {
                            if (log.IsWarnEnabled)
                            {
                                log.Warn($"GET {Base}/Stream: connection {connectionId} no longer authorised user={user}");
                            }

                            registry.Abort(connectionId);

                            break;
                        }
                    }

                    using var iteration = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationTokenSource.Token);

                    var readTask = channel.Reader.WaitToReadAsync(iteration.Token).AsTask();

                    var heartbeatTask = Task.Delay(TimeSpan.FromSeconds(heartbeatIntervalSeconds),
                        iteration.Token);

                    var completed = await Task.WhenAny(readTask, heartbeatTask);

                    await iteration.CancelAsync();

                    if (completed == heartbeatTask)
                    {
                        try
                        {
                            await readTask;
                        }
                        catch (OperationCanceledException)
                        {
                            cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        }

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
                        var frame = WatcherStreamFrame.Build(payload);

                        if (frame == null)
                        {
                            if (log.IsWarnEnabled)
                            {
                                log.Warn($"GET {Base}/Stream: dropped an empty or oversize message for connection {connectionId}");
                            }

                            continue;
                        }

                        await httpContext.Response.WriteAsync(frame, cancellationTokenSource.Token);

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