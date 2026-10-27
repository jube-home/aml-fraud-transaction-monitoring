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
using Jube.Data.Concurrency;
using Jube.Data.Context;
using Jube.Dto.Repository.CaseWorkflowFormValidator;
using Jube.Service.Exceptions.Repository.CaseWorkflowFormValidator;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseWorkflowFormValidatorEndpoints
    {
        private const string Base = "/api/CaseWorkflowFormValidator";

        public static void MapCaseWorkflowFormValidatorEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseWorkflowFormValidator");

            group.MapGet("", (HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("GET", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.GetAsync(token))))
                .Produces<List<CaseWorkflowFormValidatorDto>>()
                .WithName("CaseWorkflowFormValidatorList");

            group.MapGet("ByCaseWorkflowId/{caseWorkflowId:int}", (int caseWorkflowId,
                    HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                    IStringLocalizerFactory localizers, IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("GET ByCaseWorkflowId", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.GetByCaseWorkflowIdAsync(
                            caseWorkflowId, token))))
                .Produces<List<CaseWorkflowFormValidatorDto>>()
                .WithName("CaseWorkflowFormValidatorListByCaseWorkflowId");

            group.MapGet("{id:int}", (int id, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("GET id", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.GetByIdAsync(id, token))))
                .Produces<CaseWorkflowFormValidatorDto>()
                .WithName("CaseWorkflowFormValidatorGetById");

            group.MapPost("", ([FromBody] CaseWorkflowFormValidatorDto model, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("POST", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.InsertAsync(model, token))))
                .Produces<CaseWorkflowFormValidatorDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("CaseWorkflowFormValidatorCreate");

            group.MapPut("", ([FromBody] CaseWorkflowFormValidatorDto model, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("PUT", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.UpdateAsync(model, token))))
                .Produces<CaseWorkflowFormValidatorDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("CaseWorkflowFormValidatorUpdate");

            group.MapDelete("{id:int}", (int id, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("DELETE", httpContext, log, dynamicEnvironment, localizers, bus, token, async service =>
                    {
                        await service.DeleteAsync(id, token);

                        return TypedResults.Ok();
                    }))
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("CaseWorkflowFormValidatorDelete");

            group.MapPost("Test", ([FromBody] CaseWorkflowFormValidatorTestDto model, HttpContext httpContext,
                    ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                    IStringLocalizerFactory localizers, IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("POST Test", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.TestAsync(model, token))))
                .Produces<CaseWorkflowFormValidatorTestResultDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("CaseWorkflowFormValidatorTest");
        }

        private static async Task<IResult> RunAsync(string what, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
            IServiceChangeBus bus, CancellationToken token, Func<CaseWorkflowFormValidatorService, Task<IResult>> run)
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
                var service = await CaseWorkflowFormValidatorService.CreateAsync(dbContext, user, log, localizers,
                    bus, dynamicEnvironment, token);

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
                return Refused(log, what, user, "204 (not found)", TypedResults.StatusCode((int)HttpStatusCode.NoContent));
            }
            catch (VersionConflictException ex)
            {
                return Refused(log, what, user, $"409 supplied={ex.SuppliedVersion} current={ex.CurrentVersion}",
                    TypedResults.Conflict(new
                    {
                        code = "VersionConflict",
                        message = ex.Message,
                        suppliedVersion = ex.SuppliedVersion,
                        currentVersion = ex.CurrentVersion
                    }));
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
