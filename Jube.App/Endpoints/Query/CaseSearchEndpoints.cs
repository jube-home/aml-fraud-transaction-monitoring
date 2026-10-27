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
using Jube.Data.Context;
using Jube.Dto.Query.CaseSearch;
using Jube.Service.Charts;
using Jube.Service.Exceptions.Query.CaseSearch;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseSearchEndpoints
    {
        private const string Base = "/api/CaseSearch";

        public static void MapCaseSearchEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseSearch");

            group.MapGet("", RunAsync)
                .Produces<CaseSearchPageDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchRun");

            group.MapGet("Workflows", WorkflowsAsync)
                .Produces<List<CaseWorkflowChoiceDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchWorkflows");

            group.MapGet("Counts/{caseWorkflowGuid:Guid}", CountsAsync)
                .Produces<List<CaseSearchCountDto>>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchCounts");

            group.MapGet("RiskMatrix", RiskMatrixAsync)
                .Produces<CaseRiskMatrixDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchRiskMatrix");

            group.MapGet("RiskMatrixChart", RiskMatrixChartAsync)
                .Produces<string>(contentType: "image/svg+xml")
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchRiskMatrixChart");

            group.MapPost("Skim", SkimAsync)
                .Produces<CaseSkimDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchSkim");
        }

        private static Task<IResult> RunAsync([AsParameters] CaseSearchRequestDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.RunAsync(request, token)), token);
        }

        private static Task<IResult> WorkflowsAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Workflows", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.WorkflowsAsync(token)), token);
        }

        private static Task<IResult> CountsAsync(Guid caseWorkflowGuid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Counts/{caseWorkflowGuid}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.CountsAsync(caseWorkflowGuid, token)), token);
        }

        private static Task<IResult> RiskMatrixAsync([AsParameters] CaseSearchRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/RiskMatrix", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.RiskMatrixAsync(request, token)), token);
        }

        private static Task<IResult> RiskMatrixChartAsync([AsParameters] CaseSearchRequestDto request,
            [FromQuery] bool? dark, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/RiskMatrixChart", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, async service =>
                {
                    var matrix = await service.RiskMatrixAsync(request, token);

                    return Results.Content(CaseAnalyticsChartRenderer.RiskMatrix(matrix, dark ?? false),
                        "image/svg+xml");
                }, token);
        }

        private static Task<IResult> SkimAsync([FromBody] CaseSearchRequestDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Skim", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.SkimAsync(request, token)), token);
        }

        private static async Task<IResult> ExecuteAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<CaseSearchService, Task<IResult>> action,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"{method} {route}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseSearchService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, token);

                return await action(service);
            }
            catch (Npgsql.PostgresException e) when (e.SqlState is "22025" or "22P02" or "22007" or "22008" or "22003")
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 400 (unusable filter value) user={user}", e);
                }

                return TypedResults.BadRequest();
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
