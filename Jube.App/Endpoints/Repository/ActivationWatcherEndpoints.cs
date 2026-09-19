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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.App.Code.signalr;
using Jube.Data.Context;
using Jube.Service.Exceptions.Repository.ActivationWatcher;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.ActivationWatcher;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jube.App.Endpoints.Repository
{
    public static class ActivationWatcherEndpoints
    {
        private const string Base = "/api/ActivationWatcher";

        private static readonly JsonSerializerSettings replaySerializerSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        public static void MapActivationWatcherEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("ActivationWatcher");

            group.MapGet("Replay", ReplayAsync)
                .WithName("ActivationWatcherReplay");
        }

        private static async Task<IResult> ReplayAsync(
            DateTimeOffset? dateFrom, DateTimeOffset? dateTo, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IHubContext<WatcherHub> watcherHub, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/Replay: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await ActivationWatcherService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var rows = await service.ReplayAsync(dateFrom, dateTo, token);

                foreach (var row in rows)
                {
                    var stringRepresentationOfObj = JsonConvert.SerializeObject(row, replaySerializerSettings);

                    if (TenantGroup.TryName(row.TenantRegistryId) is not { } groupName)
                    {
                        continue;
                    }

                    await watcherHub.Clients.Group(groupName)
                        .SendAsync("ReceiveMessage", "Replay", stringRepresentationOfObj, token);
                }

                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/Replay: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/Replay: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/Replay: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/Replay: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}