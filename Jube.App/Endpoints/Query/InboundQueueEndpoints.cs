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
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Dto.Query.InboundQueue;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Query.InboundQueue;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.App.Endpoints.Query
{
    public static class InboundQueueEndpoints
    {
        private const string Base = "/api/InboundQueue";

        public static void MapInboundQueueEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base).RequireAuthorization().WithTags("InboundQueue");

            group.MapGet("Depth", DepthAsync)
                .Produces<InboundQueueDepthDto>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("InboundQueueDepth");

            group.MapGet("Dead", DeadAsync)
                .Produces<List<InboundQueueDeadDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("InboundQueueDead");

            group.MapPost("Dead/{id:long}/Requeue", RequeueAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("InboundQueueRequeue");

            group.MapDelete("Dead/{id:long}", DeleteAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("InboundQueueDelete");
        }

        private static Task<IResult> DepthAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.DepthAsync(token)), token);
        }

        private static Task<IResult> DeadAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ListDeadAsync(token)), token);
        }

        private static Task<IResult> RequeueAsync(long id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus, async service =>
            {
                await service.RequeueAsync(id, token);

                return TypedResults.NoContent();
            }, token);
        }

        private static Task<IResult> DeleteAsync(long id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus, async service =>
            {
                await service.DeleteAsync(id, token);

                return TypedResults.NoContent();
            }, token);
        }

        private static async Task<IResult> ExecuteAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            Func<InboundQueueAdminService, Task<IResult>> action, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(await InboundQueueAdminService.CreateAsync(dbContext, user, log, bus, token));
            }
            catch (Exception ex) when (ex is CaseExceptions.NotAuthenticatedException
                                           or CaseExceptions.ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (CaseExceptions.NotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (CaseExceptions.ConflictException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
        }
    }
}
