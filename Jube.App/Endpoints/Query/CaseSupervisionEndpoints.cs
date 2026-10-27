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
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Dto.Query.CaseAnalytics;
using Jube.Dto.Query.CaseSupervision;
using Jube.Service.Exceptions.Query.CaseSupervision;
using Jube.Service.Query.CaseSupervision;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseSupervisionEndpoints
    {
        private const string Base = "/api/CaseSupervision";

        public static void MapCaseSupervisionEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseSupervision");

            group.MapGet("", WorkloadAsync)
                .Produces<CaseSupervisionDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSupervisionWorkload");

            group.MapGet("StuckLocks", StuckLocksAsync)
                .Produces<System.Collections.Generic.List<CaseStuckLockDto>>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSupervisionStuckLocks");

            group.MapPost("BreakLock", BreakLockAsync)
                .Produces((int)HttpStatusCode.NoContent)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSupervisionBreakLock");

            group.MapPost("Reassign", ReassignAsync)
                .Produces((int)HttpStatusCode.NoContent)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSupervisionReassign");
        }

        private static Task<IResult> WorkloadAsync([AsParameters] CaseAnalyticsRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.WorkloadAsync(request, token)),
                token);
        }

        private static Task<IResult> StuckLocksAsync([FromQuery] Guid caseWorkflowGuid, [FromQuery] int? idleMinutes,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/StuckLocks", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.StuckLocksAsync(caseWorkflowGuid,
                    idleMinutes ?? CaseSupervisionService.DefaultIdleMinutes, token)), token);
        }

        private static Task<IResult> BreakLockAsync([FromBody] CaseBreakLockDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/BreakLock", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, async service =>
                {
                    await service.BreakLockAsync(model, token);

                    return TypedResults.NoContent();
                }, token);
        }

        private static Task<IResult> ReassignAsync([FromBody] CaseReassignDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Reassign", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, async service =>
                {
                    await service.ReassignAsync(model, token);

                    return TypedResults.NoContent();
                }, token);
        }

        private static async Task<IResult> ExecuteAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<CaseSupervisionService, Task<IResult>> action,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseSupervisionService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);

                return await action(service);
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
                log.Error($"{method} {route}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
