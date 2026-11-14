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
using Jube.Dto.Repository.SanctionExemption;
using Jube.Service.Exceptions.Repository.SanctionExemption;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.SanctionExemption;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Jube.App.Endpoints.Repository
{
    public static class SanctionExemptionEndpoints
    {
        private const string Base = "/api/SanctionExemption";

        public static void MapSanctionExemptionEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("SanctionExemption");

            group.MapGet(string.Empty, ListAsync)
                .Produces<List<SanctionExemptionDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("SanctionExemptionList");

            group.MapPost("Request", RequestAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<SanctionExemptionDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("SanctionExemptionRequest");

            group.MapPost("Approve", ApproveAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("SanctionExemptionApprove");

            group.MapPost("Reject", RejectAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("SanctionExemptionReject");

            group.MapPost("Retract/{id:int}", RetractAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("SanctionExemptionRetract");
        }

        private static Task<IResult> ListAsync([FromQuery] short? stateId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, serviceChangeBus,
                async service => TypedResults.Ok(await service.ListAsync(stateId, token)), token);
        }

        private static Task<IResult> RequestAsync([FromBody] SanctionExemptionRequestDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, serviceChangeBus,
                async service => TypedResults.Ok(await service.RequestAsync(model, token)), token);
        }

        private static Task<IResult> ApproveAsync([FromBody] SanctionExemptionDecisionDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, serviceChangeBus, async service =>
            {
                await service.DecideAsync(model, true, token);

                return TypedResults.NoContent();
            }, token);
        }

        private static Task<IResult> RejectAsync([FromBody] SanctionExemptionDecisionDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, serviceChangeBus, async service =>
            {
                await service.DecideAsync(model, false, token);

                return TypedResults.NoContent();
            }, token);
        }

        private static Task<IResult> RetractAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, serviceChangeBus, async service =>
            {
                await service.RetractAsync(id, token);

                return TypedResults.NoContent();
            }, token);
        }

        private static async Task<IResult> ExecuteAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus serviceChangeBus,
            Func<SanctionExemptionService, Task<IResult>> action, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await SanctionExemptionService.CreateAsync(dbContext, user, log, serviceChangeBus, token);

                return await action(service);
            }
            catch (Exception ex) when (ex is NotAuthenticatedException or ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (ConflictException ex)
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
                log.Error($"{httpContext.Request.Method} {Base}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
