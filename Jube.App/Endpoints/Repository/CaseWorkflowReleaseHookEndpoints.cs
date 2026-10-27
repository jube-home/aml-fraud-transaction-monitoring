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
using Jube.Dto.Repository.CaseWorkflowReleaseHook;
using Jube.Service.Exceptions.Repository.CaseWorkflowReleaseHook;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowReleaseHook;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseWorkflowReleaseHookEndpoints
    {
        private const string Base = "/api/CaseWorkflowReleaseHook";

        public static void MapCaseWorkflowReleaseHookEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseWorkflowReleaseHook");

            group.MapGet("", (HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("GET", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.GetAsync(token))))
                .Produces<List<CaseWorkflowReleaseHookDto>>()
                .WithName("CaseWorkflowReleaseHookList");

            group.MapGet("ByCaseWorkflowId/{caseWorkflowId:int}", (int caseWorkflowId,
                    HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                    IStringLocalizerFactory localizers, IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("GET ByCaseWorkflowId", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.GetByCaseWorkflowIdAsync(
                            caseWorkflowId, token))))
                .Produces<List<CaseWorkflowReleaseHookDto>>()
                .WithName("CaseWorkflowReleaseHookListByCaseWorkflowId");

            group.MapGet("{id:int}", (int id, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("GET id", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.GetByIdAsync(id, token))))
                .Produces<CaseWorkflowReleaseHookDto>()
                .WithName("CaseWorkflowReleaseHookGetById");

            group.MapPost("", ([FromBody] CaseWorkflowReleaseHookDto model, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("POST", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.InsertAsync(model, token))))
                .Produces<CaseWorkflowReleaseHookDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("CaseWorkflowReleaseHookCreate");

            group.MapPost("Validate", ([FromBody] CaseWorkflowReleaseHookDto model, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("POST Validate", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.ValidateAsync(model, token))))
                .Produces<Jube.Dto.Validation.ValidationResultDto>()
                .WithName("CaseWorkflowReleaseHookValidate");

            group.MapPut("", ([FromBody] CaseWorkflowReleaseHookDto model, HttpContext httpContext, ILog log,
                    DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
                    IServiceChangeBus bus, CancellationToken token) =>
                    RunAsync("PUT", httpContext, log, dynamicEnvironment, localizers, bus, token,
                        async service => TypedResults.Ok(await service.UpdateAsync(model, token))))
                .Produces<CaseWorkflowReleaseHookDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("CaseWorkflowReleaseHookUpdate");

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
                .WithName("CaseWorkflowReleaseHookDelete");
        }

        private static async Task<IResult> RunAsync(string what, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
            IServiceChangeBus bus, CancellationToken token, Func<CaseWorkflowReleaseHookService, Task<IResult>> run)
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
                var service = await CaseWorkflowReleaseHookService.CreateAsync(dbContext, user, log, localizers,
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
