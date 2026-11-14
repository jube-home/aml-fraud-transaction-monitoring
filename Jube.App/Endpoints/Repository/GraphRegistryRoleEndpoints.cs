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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Jube.App.Code;
using Jube.Data.Concurrency;
using Jube.Data.Context;
using Jube.Dto.Repository.GraphRegistryRole;
using Jube.Service.Exceptions.Repository.GraphRegistryRole;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.GraphRegistryRole;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class GraphRegistryRoleEndpoints
    {
        private const string Base = "/api/GraphRegistryRole";

        public static void MapGraphRegistryRoleEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("GraphRegistryRole");

            group.MapGet("ByGraphRegistryGuid/{guid:guid}", ByGraphRegistryGuidAsync)
                .Produces<List<GraphRegistryRoleDto>>()
                .WithName("GraphRegistryRoleListByGraphRegistryGuid");

            group.MapPost("", CreateAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphRegistryRoleDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("GraphRegistryRoleCreate");

            group.MapDelete("{id:int}", DeleteAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("GraphRegistryRoleDelete");
        }

        private static Task<IResult> ByGraphRegistryGuidAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/ByGraphRegistryGuid/{guid}", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetByGraphRegistryGuidAsync(guid, token)), token);
        }

        private static Task<IResult> CreateAsync([FromBody] GraphRegistryRoleDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("POST", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.InsertAsync(model, token)), token);
        }

        private static Task<IResult> DeleteAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("DELETE", $"{Base}/{id}", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service =>
                {
                    await service.DeleteAsync(id, token);

                    return TypedResults.NoContent();
                }, token);
        }

        private static Task<IResult> ExecuteAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<GraphRegistryRoleService, Task<IResult>> action, CancellationToken token)
        {
            return GuardAsync(method, route, httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await GraphRegistryRoleService.CreateAsync(dbContext, httpContext.User.Identity?.Name, log,
                    stringLocalizerFactory, serviceChangeBus, token);

                return await action(service);
            });
        }

        private static async Task<IResult> GuardAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, Func<DbContext, Task<IResult>> action)
        {
            var user = httpContext.User.Identity?.Name;
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(dbContext);
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                return TypedResults.BadRequest(ex.Result);
            }
            catch (VersionConflictException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"{method} {route}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
