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
using Jube.Dto.Repository.Erasure;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.Erasure;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.App.Endpoints.Repository
{
    public static class ErasureEndpoints
    {
        private const string Base = "/api/Erasure";

        public static void MapErasureEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base).RequireAuthorization().WithTags("Erasure");

            group.MapGet("", ListAsync)
                .Produces<List<ErasureRequestDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("ErasureList");
            group.MapPost("Preview", PreviewAsync).ValidateAntiforgeryForCookieSessions()
                .Produces<ErasurePreviewDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("ErasurePreview");
            group.MapPost("Request", RequestAsync).ValidateAntiforgeryForCookieSessions()
                .Produces<ErasureRequestDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("ErasureRequest");
            group.MapPost("Approve", ApproveAsync).ValidateAntiforgeryForCookieSessions()
                .Produces<ErasureRequestDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("ErasureApprove");
            group.MapPost("Reject", RejectAsync).ValidateAntiforgeryForCookieSessions()
                .Produces<ErasureRequestDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("ErasureReject");
            group.MapPost("Retract/{guid:guid}", RetractAsync).ValidateAntiforgeryForCookieSessions()
                .Produces<ErasureRequestDto>()
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("ErasureRetract");
            group.MapPost("Export", ExportAsync).ValidateAntiforgeryForCookieSessions()
                .Produces<SubjectExportDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("ErasureExport");
        }

        private static Task<IResult> ListAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ListAsync(token)), token);
        }

        private static Task<IResult> PreviewAsync([FromBody] ErasureSubjectDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.PreviewAsync(model, token)), token);
        }

        private static Task<IResult> RequestAsync([FromBody] ErasureRequestCreateDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.RequestAsync(model, token)), token);
        }

        private static Task<IResult> ApproveAsync([FromBody] ErasureDecisionDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ApproveAsync(model, token)), token);
        }

        private static Task<IResult> RejectAsync([FromBody] ErasureDecisionDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            CancellationToken token)
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

        private static Task<IResult> ExportAsync([FromBody] ErasureSubjectDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ExportAsync(model, token)), token);
        }

        private static async Task<IResult> ExecuteAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            Func<ErasureService, Task<IResult>> action, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var minimum = int.Parse(dynamicEnvironment.AppSettings("ErasureRetentionMinimumDays"));

                return await action(await ErasureService.CreateAsync(dbContext, user, log, bus, minimum, token));
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
