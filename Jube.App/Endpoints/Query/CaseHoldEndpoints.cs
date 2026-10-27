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
using Jube.Dto.Query.CaseHold;
using Jube.Service.Exceptions.Query.CaseHold;
using Jube.Service.Query.CaseHold;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseHoldEndpoints
    {
        private const string Base = "/api/CaseHold";

        public static void MapCaseHoldEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseHold");

            group.MapGet("ByCaseId/{caseId:int}", (int caseId, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("GET ByCaseId", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.ListAsync(caseId, token))))
                .Produces<List<CaseHoldDto>>()
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("CaseHoldListByCaseId");

            group.MapPost("Release", ([FromBody] CaseHoldDecisionDto model, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("POST Release", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.ReleaseAsync(model, token))))
                .Produces<CaseHoldDecisionResultDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.BadGateway)
                .WithName("CaseHoldRelease");

            group.MapPost("Refuse", ([FromBody] CaseHoldDecisionDto model, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("POST Refuse", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.RefuseAsync(model, token))))
                .Produces<CaseHoldDecisionResultDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.BadGateway)
                .WithName("CaseHoldRefuse");
        }

        private static async Task<IResult> RunAsync(string what, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
            IServiceChangeBus bus, CancellationToken token, Func<CaseHoldService, Task<IResult>> run)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"{what} {Base}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseHoldService.CreateAsync(dbContext, user, log, localizers, bus, dynamicEnvironment, token);

                return await run(service);
            }
            catch (NotAuthenticatedException)
            {
                return Refused(log, what, user, "403 (not authenticated)", TypedResults.Forbid());
            }
            catch (ForbiddenException)
            {
                return Refused(log, what, user, "403", TypedResults.Forbid());
            }
            catch (DtoValidationException ex)
            {
                return Refused(log, what, user, $"400 errors={ex.Result.Errors.Count}",
                    TypedResults.BadRequest(ex.Result));
            }
            catch (NotFoundException)
            {
                return Refused(log, what, user, "404 (not found)", TypedResults.NotFound());
            }
            catch (HoldStateException ex)
            {
                return Refused(log, what, user, "409 (not held)",
                    TypedResults.Conflict(new { code = ex.Code, message = ex.Message }));
            }
            catch (CaseLockedException ex)
            {
                return Refused(log, what, user, "409 (locked)",
                    TypedResults.Conflict(new { code = ex.Code, message = ex.Message }));
            }
            catch (NoReleaseHookException ex)
            {
                return Refused(log, what, user, "409 (no release hook)",
                    TypedResults.Conflict(new { code = ex.Code, message = ex.Message }));
            }
            catch (HoldHookException ex)
            {
                return Refused(log, what, user, $"502 status={ex.HttpStatus}",
                    TypedResults.Json(new { code = ex.Code, message = ex.Message, httpStatus = ex.HttpStatus },
                        statusCode: (int)HttpStatusCode.BadGateway));
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{what} {Base}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"{what} {Base}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static IResult Refused(ILog log, string what, string user, string outcome, IResult result)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn($"{what} {Base}: {outcome} user={user}");
            }

            return result;
        }
    }
}
