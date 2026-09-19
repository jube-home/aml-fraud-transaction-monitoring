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
using Jube.Dto.Repository.CaseWorkflowDisplay;
using Jube.Service.Repository.CaseWorkflowDisplay;
using Jube.Service.Exceptions.Repository.CaseWorkflowDisplay;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseWorkflowDisplayEndpoints
    {
        private const string Base = "/api/CaseWorkflowDisplay";

        public static void MapCaseWorkflowDisplayEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseWorkflowDisplay");

            group.MapGet("", GetAsync)
                .Produces<List<CaseWorkflowDisplayDto>>()
                .WithName("CaseWorkflowDisplayList");

            group.MapGet("ByCasesWorkflowIdActiveOnly/{id:int}", GetByCasesWorkflowIdActiveOnlyAsync)
                .Produces<List<CaseWorkflowDisplayDto>>()
                .WithName("CaseWorkflowDisplayListByWorkflowIdActiveOnly");

            group.MapGet("ByCasesWorkflowGuidActiveOnly/{guid:guid}", GetByCasesWorkflowGuidActiveOnlyAsync)
                .Produces<List<CaseWorkflowDisplayDto>>()
                .WithName("CaseWorkflowDisplayListByWorkflowGuidActiveOnly");

            group.MapGet("ByCasesWorkflowId/{casesWorkflowId:int}", GetByCaseWorkflowIdAsync)
                .Produces<List<CaseWorkflowDisplayDto>>()
                .WithName("CaseWorkflowDisplayListByWorkflow");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<CaseWorkflowDisplayDto>()
                .WithName("CaseWorkflowDisplayGetById");

            group.MapPost("", InsertAsync)
                .Produces<CaseWorkflowDisplayDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("CaseWorkflowDisplayCreate");

            group.MapPut("", UpdateAsync)
                .Produces<CaseWorkflowDisplayDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("CaseWorkflowDisplayUpdate");

            group.MapDelete("{id:int}", DeleteAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("CaseWorkflowDisplayDelete");
        }

        private static async Task<IResult> GetAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseWorkflowDisplayService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                return TypedResults.Ok(await service.GetAsync(token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByCasesWorkflowIdActiveOnlyAsync(int id, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ByCasesWorkflowIdActiveOnly/{id}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseWorkflowDisplayService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                return TypedResults.Ok(await service.GetByCasesWorkflowIdActiveOnlyAsync(id, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCasesWorkflowIdActiveOnly/{id}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCasesWorkflowIdActiveOnly/{id}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/ByCasesWorkflowIdActiveOnly/{id}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByCasesWorkflowIdActiveOnly/{id}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByCasesWorkflowGuidActiveOnlyAsync(Guid guid, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ByCasesWorkflowGuidActiveOnly/{guid}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseWorkflowDisplayService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                return TypedResults.Ok(await service.GetByCasesWorkflowGuidActiveOnlyAsync(guid, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCasesWorkflowGuidActiveOnly/{guid}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCasesWorkflowGuidActiveOnly/{guid}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/ByCasesWorkflowGuidActiveOnly/{guid}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByCasesWorkflowGuidActiveOnly/{guid}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByCaseWorkflowIdAsync(int casesWorkflowId, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseWorkflowDisplayService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                return TypedResults.Ok(await service.GetByCaseWorkflowIdAsync(casesWorkflowId, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByIdAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/{id}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseWorkflowDisplayService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                return TypedResults.Ok(await service.GetByIdAsync(id, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/{id}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/{id}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/{id}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/{id}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> InsertAsync([FromBody] CaseWorkflowDisplayDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseWorkflowDisplayService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                return TypedResults.Ok(await service.InsertAsync(model, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 400 user={user} errors={ex.Result.Errors.Count}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> UpdateAsync([FromBody] CaseWorkflowDisplayDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"PUT {Base}: entry id={model?.Id} user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseWorkflowDisplayService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                return TypedResults.Ok(await service.UpdateAsync(model, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}: 400 user={user} errors={ex.Result.Errors.Count}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}: 204 (not found) user={user}");
                }

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"PUT {Base}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"PUT {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> DeleteAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"DELETE {Base}/{id}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseWorkflowDisplayService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                await service.DeleteAsync(id, token);
                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"DELETE {Base}/{id}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"DELETE {Base}/{id}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"DELETE {Base}/{id}: 204 (not found) user={user}");
                }

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"DELETE {Base}/{id}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"DELETE {Base}/{id}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}