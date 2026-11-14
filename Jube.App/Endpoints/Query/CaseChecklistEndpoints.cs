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
using Jube.Data.Context;
using Jube.Dto.Query.CaseChecklist;
using Jube.Service.Exceptions.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseChecklistEndpoints
    {
        private const string Base = "/api/CaseChecklist";
        private const long MaxJsonBodyBytes = 64 * 1024;

        public static void MapCaseChecklistEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseChecklist");

            group.MapGet("{caseId:int}", GetAsync)
                .Produces<CaseChecklistDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseChecklistGet");

            group.MapGet("{caseId:int}/History", HistoryAsync)
                .Produces<List<CaseChecklistHistoryEntryDto>>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseChecklistHistory");

            group.MapPost("{caseId:int}/Check/{itemId:long}", CheckAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseChecklistDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseChecklistCheck");

            group.MapPost("{caseId:int}/Uncheck/{itemId:long}", UncheckAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseChecklistDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseChecklistUncheck");

            group.MapPost("{caseId:int}/Override", OverrideAsync)
                .ValidateAntiforgeryForCookieSessions()
                .WithMetadata(new RequestSizeLimitAttribute(MaxJsonBodyBytes))
                .Produces<CaseChecklistOverrideDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseChecklistOverride");
        }

        private static Task<IResult> GetAsync(int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/{caseId}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetAsync(caseId, token)), token);
        }

        private static Task<IResult> HistoryAsync(int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/{caseId}/History", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetHistoryAsync(caseId, token)), token);
        }

        private static Task<IResult> CheckAsync(int caseId, long itemId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/{caseId}/Check/{itemId}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.CheckAsync(caseId, itemId, token)), token);
        }

        private static Task<IResult> UncheckAsync(int caseId, long itemId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/{caseId}/Uncheck/{itemId}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.UncheckAsync(caseId, itemId, token)), token);
        }

        private static Task<IResult> OverrideAsync(int caseId, [FromBody] CaseChecklistOverrideRequestDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/{caseId}/Override", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.OverrideAsync(caseId, model, token)), token);
        }

        private static async Task<IResult> ExecuteAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<CaseChecklistService, Task<IResult>> action,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseChecklistService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, token);

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
