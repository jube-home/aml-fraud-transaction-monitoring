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
using Jube.Dto.Repository.CaseSuppression;
using Jube.Service.Exceptions.Repository.CaseSuppression;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseSuppression;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseSuppressionEndpoints
    {
        private const string Base = "/api/CaseSuppression";

        public static void MapCaseSuppressionEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseSuppression");

            group.MapPost("", RequestAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseSuppressionDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("CaseSuppressionRequest");

            group.MapPost("Retract", RetractAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseSuppressionDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("CaseSuppressionRetract");

            group.MapPost("Resubmit", ResubmitAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseSuppressionDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("CaseSuppressionResubmit");

            group.MapPost("Approve", ApproveAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseSuppressionDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("CaseSuppressionApprove");

            group.MapPost("Reject", RejectAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseSuppressionDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("CaseSuppressionReject");

            group.MapGet("ByCase/{caseId:int}", ListByCaseAsync)
                .Produces<List<CaseSuppressionDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseSuppressionListByCase");

            group.MapGet("Awaiting", ListAwaitingAsync)
                .Produces<List<CaseSuppressionDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseSuppressionListAwaiting");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<CaseSuppressionDto>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("CaseSuppressionGet");

            group.MapGet("{id:int}/Versions", GetVersionsAsync)
                .Produces<List<CaseSuppressionVersionDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("CaseSuppressionVersions");
        }

        private static Task<IResult> RequestAsync([FromBody] CaseSuppressionRequestDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus, "POST",
                async service => await service.RequestAsync(model, token), token);
        }

        private static Task<IResult> RetractAsync([FromBody] CaseSuppressionDecisionDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "POST Retract", async service => await service.RetractAsync(model, token), token);
        }

        private static Task<IResult> ResubmitAsync([FromBody] CaseSuppressionDecisionDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "POST Resubmit", async service => await service.ResubmitAsync(model, token), token);
        }

        private static Task<IResult> ApproveAsync([FromBody] CaseSuppressionDecisionDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "POST Approve", async service => await service.ApproveAsync(model, token), token);
        }

        private static Task<IResult> RejectAsync([FromBody] CaseSuppressionDecisionDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "POST Reject", async service => await service.RejectAsync(model, token), token);
        }

        private static Task<IResult> ListByCaseAsync(int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "GET ByCase", async service => await service.ListByCaseAsync(caseId, token), token);
        }

        private static Task<IResult> ListAwaitingAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token, int limit = 100)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "GET Awaiting", async service => await service.ListAwaitingAsync(limit, token), token);
        }

        private static Task<IResult> GetByIdAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus, "GET",
                async service => await service.GetByIdAsync(id, token), token);
        }

        private static Task<IResult> GetVersionsAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "GET Versions", async service => await service.GetVersionsAsync(id, token), token);
        }

        private static async Task<IResult> RunAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, string route, Func<CaseSuppressionService, Task<object>> action,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseSuppressionService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, token);

                return TypedResults.Ok(await action(service));
            }
            catch (Exception e) when (e is NotAuthenticatedException or ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{route} {Base}: 403 user={user}");
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
            catch (RefusedException ex)
            {
                return TypedResults.Conflict(new { code = ex.Code, message = ex.Message });
            }
            catch (ArgumentNullException)
            {
                return TypedResults.BadRequest();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"{route} {Base}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
