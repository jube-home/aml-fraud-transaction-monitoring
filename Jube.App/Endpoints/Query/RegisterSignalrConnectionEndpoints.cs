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
using Jube.Service.Exceptions.Query.RegisterSignalrConnection;
using Jube.Service.Query.RegisterSignalrConnection;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class RegisterSignalrConnectionEndpoints
    {
        private const string Base = "/api/RegisterSignalrConnection";

        public static void MapRegisterSignalrConnectionEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("RegisterSignalrConnection");

            group.MapGet("{id}", RegisterAsync)
                .WithName("RegisterSignalrConnectionRegister");
        }

        private static async Task<IResult> RegisterAsync(string id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, IHubContext<WatcherHub> watcherHub, CancellationToken token)
        {
            var route = $"GET {Base}/{id}";
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"{route}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await RegisterSignalrConnectionService.CreateAsync(dbContext, user,
                    new SignalrGroupRegistrar(watcherHub.Groups, WatcherConnectionRegistry.Instance), log,
                    stringLocalizerFactory, serviceChangeBus, token);
                await service.RegisterAsync(id, token);
                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{route}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (InvalidConnectionIdException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{route}: 400 (malformed connection id) user={user}");
                }

                return TypedResults.BadRequest();
            }
            catch (ConnectionNotOwnedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{route}: 403 (connection not owned) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{route}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{route}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"{route}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}