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
using Jube.Dto.Repository.Case;
using Jube.Service.Repository.Case;
using Jube.Service.Exceptions.Repository.Case;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseEndpoints
    {
        private const string Base = "/api/Case";

        public static void MapCaseEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("Case");

            group.MapGet("", GetAsync)
                .Produces<System.Collections.Generic.List<CaseDto>>()
                .WithName("CaseList");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<CaseDto>()
                .WithName("CaseGetByIdActiveOnly");

            group.MapPost("", InsertAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("CaseCreate");

            group.MapPost("CreateFromCaseKeyValue", CreateFromCaseKeyValueAsync)
                .Produces<CaseDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseCreateFromCaseKeyValue");

            group.MapPut("", UpdateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseUpdate");
        }

        private static async Task<IResult> GetAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Engine.Helpers.JsonSerializationHelper jsonSerializationHelper,
            CancellationToken token)
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
                var service = await CaseService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, jsonSerializationHelper, token);
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

        private static async Task<IResult> GetByIdAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Engine.Helpers.JsonSerializationHelper jsonSerializationHelper,
            CancellationToken token)
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
                var service = await CaseService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, jsonSerializationHelper, token);
                var found = await service.GetByIdAsync(id, token);
                return found is null ? TypedResults.NotFound() : TypedResults.Ok(found);
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

        private static async Task<IResult> InsertAsync([FromBody] CaseDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Engine.Helpers.JsonSerializationHelper jsonSerializationHelper,
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
                var service = await CaseService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, jsonSerializationHelper, token);
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
            catch (NotFoundException)
            {
                return TypedResults.NotFound();
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

        private static async Task<IResult> CreateFromCaseKeyValueAsync([FromBody] CreateCaseDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            Engine.Helpers.JsonSerializationHelper jsonSerializationHelper, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/CreateFromCaseKeyValue: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, jsonSerializationHelper, token);
                return TypedResults.Ok(await service.CreateFromCaseKeyValueAsync(model, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/CreateFromCaseKeyValue: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/CreateFromCaseKeyValue: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (InvalidCaseWorkflowStatusException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/CreateFromCaseKeyValue: 403 (invalid case workflow status) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/CreateFromCaseKeyValue: 400 user={user} errors={ex.Result.Errors.Count}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
            catch (ConflictException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/CreateFromCaseKeyValue: 409 user={user}");
                }

                return TypedResults.Conflict(ex.Message);
            }
            catch (NotFoundException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/CreateFromCaseKeyValue: 404 user={user}");
                }

                return TypedResults.NotFound(ex.Message);
            }
            catch (CaseCreationFailedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/CreateFromCaseKeyValue: 500 (case creation did not complete) user={user}");
                }

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/CreateFromCaseKeyValue: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/CreateFromCaseKeyValue: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> UpdateAsync([FromBody] CaseDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Engine.Helpers.JsonSerializationHelper jsonSerializationHelper,
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
                var service = await CaseService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, jsonSerializationHelper, token);
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
            catch (InvalidCaseWorkflowStatusException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}: 403 (invalid case workflow status) user={user}");
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
            catch (ConflictException)
            {
                return TypedResults.Conflict();
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
    }
}