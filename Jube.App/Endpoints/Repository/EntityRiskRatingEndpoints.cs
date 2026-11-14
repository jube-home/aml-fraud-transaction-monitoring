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
using Jube.Dto.Repository.EntityRiskRating;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.EntityRiskRating;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.App.Endpoints.Repository
{
    public static class EntityRiskRatingEndpoints
    {
        private const string Base = "/api/EntityRiskRating";

        public static void MapEntityRiskRatingEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base).RequireAuthorization().WithTags("EntityRiskRating");

            group.MapGet("ByKey", GetAsync)
                .Produces<EntityRiskRatingHistoryDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("EntityRiskRatingByKey");

            group.MapGet("Due", DueAsync)
                .Produces<List<EntityRiskRatingDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("EntityRiskRatingDue");

            group.MapGet("Requests", RequestsAsync)
                .Produces<List<EntityRiskRatingRequestDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("EntityRiskRatingRequests");

            group.MapPost("Approve", ApproveAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<EntityRiskRatingRequestDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("EntityRiskRatingApprove");

            group.MapPost("Reject", RejectAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<EntityRiskRatingRequestDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("EntityRiskRatingReject");

            group.MapPost("Retract/{guid:guid}", RetractAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<EntityRiskRatingRequestDto>()
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("EntityRiskRatingRetract");

            group.MapPost("Set", SetAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<EntityRiskRatingRequestDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("EntityRiskRatingSet");

            group.MapPost("Import", ImportAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<EntityRiskRatingImportResultDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("EntityRiskRatingImport");
        }

        private static Task<IResult> GetAsync(string keyName, string keyValue, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.GetAsync(keyName, keyValue, token)), token);
        }

        private static Task<IResult> RequestsAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ListPendingAsync(token)), token);
        }

        private static Task<IResult> ApproveAsync([FromBody] EntityRiskRatingDecisionDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ApproveAsync(model, token)), token);
        }

        private static Task<IResult> RejectAsync([FromBody] EntityRiskRatingDecisionDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.RejectAsync(model, token)), token);
        }

        private static Task<IResult> RetractAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.RetractAsync(guid, token)), token);
        }

        private static Task<IResult> DueAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ListDueAsync(token)), token);
        }

        private static Task<IResult> SetAsync([FromBody] EntityRiskRatingDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.SetAsync(model, token)), token);
        }

        private static Task<IResult> ImportAsync([FromBody] EntityRiskRatingImportDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ImportAsync(model, token)), token);
        }

        private static async Task<IResult> ExecuteAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            Func<EntityRiskRatingService, Task<IResult>> action, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(await EntityRiskRatingService.CreateAsync(dbContext, user, log, bus, token));
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
