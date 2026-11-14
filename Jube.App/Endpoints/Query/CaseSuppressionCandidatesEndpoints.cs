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
using Jube.Dto.Query.CaseSuppressionCandidates;
using Jube.Dto.Repository.CaseSuppression;
using Jube.Service.Exceptions.Repository.CaseSuppression;
using Jube.Service.Query.CaseSuppressionCandidates;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseSuppressionCandidatesEndpoints
    {
        private const string Base = "/api/CaseSuppressionCandidates";

        public static void MapCaseSuppressionCandidatesEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseSuppressionCandidates");

            group.MapGet("Keys", KeysAsync)
                .Produces<List<CaseSuppressionKeyDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("CaseSuppressionCandidateKeys");

            group.MapGet("Values", ValuesAsync)
                .Produces<List<CaseSuppressionValueDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("CaseSuppressionCandidateValues");

            group.MapGet("Transactions", TransactionsAsync)
                .Produces<List<CaseSuppressionCandidateTransactionDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("CaseSuppressionCandidateTransactions");

            group.MapGet("InForce", InForceAsync)
                .Produces<List<CaseSuppressionInForceDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("CaseSuppressionInForce");

            group.MapPost("ReasonDraft", ReasonDraftAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseSuppressionReasonDraftDto>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("CaseSuppressionReasonDraft");
        }

        private static Task<IResult> KeysAsync(int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus, "Keys",
                async service => await service.KeysAsync(caseId, token), token);
        }

        private static Task<IResult> ValuesAsync(int caseId, string key, string merge, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus, "Values",
                async service => await service.ValuesAsync(caseId, key, merge, token), token);
        }

        private static Task<IResult> TransactionsAsync(int caseId, string key, string value, string merge,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "Transactions", async service => await service.TransactionsAsync(caseId, key, value, merge, token),
                token);
        }

        private static Task<IResult> InForceAsync(int caseId, string merge, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "InForce", async service => await service.InForceAsync(caseId, merge, token), token);
        }

        private static Task<IResult> ReasonDraftAsync([FromBody] CaseSuppressionRequestDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync(httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus,
                "ReasonDraft", async service => await service.ReasonDraftAsync(model, token), token);
        }

        private static async Task<IResult> RunAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, string route,
            Func<CaseSuppressionCandidatesService, Task<object>> action, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseSuppressionCandidatesService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, token);

                return TypedResults.Ok(await action(service));
            }
            catch (Exception e) when (e is NotAuthenticatedException or ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/{route}: 403 user={user}");
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
                log.Error($"GET {Base}/{route}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
