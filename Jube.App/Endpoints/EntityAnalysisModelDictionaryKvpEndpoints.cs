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
using Jube.Dto.EntityAnalysisModelDictionaryKvp;
using Jube.Service.EntityAnalysisModelDictionaryKvp;
using Jube.Service.Exceptions.EntityAnalysisModelDictionaryKvp;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints
{
    public static class EntityAnalysisModelDictionaryKvpEndpoints
    {
        private const string Base = "/api/EntityAnalysisModelDictionaryKvp";

        public static void MapEntityAnalysisModelDictionaryKvpEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("EntityAnalysisModelDictionaryKvp");

            group.MapGet("", GetAsync)
                .Produces<List<EntityAnalysisModelDictionaryKvpDto>>()
                .WithName("EntityAnalysisModelDictionaryKvpGetAll");

            group.MapGet("ByEntityAnalysisModelDictionaryId", GetByEntityAnalysisModelDictionaryIdAsync)
                .Produces<List<EntityAnalysisModelDictionaryKvpDto>>()
                .WithName("EntityAnalysisModelDictionaryKvpGetByEntityAnalysisModelDictionaryId");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<EntityAnalysisModelDictionaryKvpDto>()
                .WithName("EntityAnalysisModelDictionaryKvpGetById");

            group.MapPost("", CreateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("EntityAnalysisModelDictionaryKvpCreate");

            group.MapPut("", UpdateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("EntityAnalysisModelDictionaryKvpUpdate");

            group.MapDelete("", DeleteAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.BadRequest)
                .WithName("EntityAnalysisModelDictionaryKvpDelete");
        }

        private static async Task<int?> ResolveIdAsync(HttpContext httpContext)
        {
            if (httpContext.Request.Query.TryGetValue("id", out var queryValue) &&
                int.TryParse(queryValue, out var queryId))
                return queryId;

            if (httpContext.Request.HasFormContentType)
            {
                var form = await httpContext.Request.ReadFormAsync();
                if (form.TryGetValue("id", out var formValue) && int.TryParse(formValue, out var formId)) return formId;
            }

            return null;
        }

        private static int ResolveEntityAnalysisModelDictionaryId(HttpContext httpContext)
        {
            return httpContext.Request.Query.TryGetValue("entityAnalysisModelDictionaryId", out var queryValue) &&
                   int.TryParse(queryValue, out var queryId)
                ? queryId
                : 0;
        }

        private static async Task<IResult> GetAsync(
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"GET {Base}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelDictionaryKvpService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.GetAsync(token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"GET {Base}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByEntityAnalysisModelDictionaryIdAsync(
            HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            var entityAnalysisModelDictionaryId = ResolveEntityAnalysisModelDictionaryId(httpContext);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"GET {Base}/ByEntityAnalysisModelDictionaryId/{entityAnalysisModelDictionaryId}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelDictionaryKvpService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service
                    .GetByEntityAnalysisModelDictionaryIdAsync(entityAnalysisModelDictionaryId, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"GET {Base}/ByEntityAnalysisModelDictionaryId/{entityAnalysisModelDictionaryId}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"GET {Base}/ByEntityAnalysisModelDictionaryId/{entityAnalysisModelDictionaryId}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"GET {Base}/ByEntityAnalysisModelDictionaryId/{entityAnalysisModelDictionaryId}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error(
                    $"GET {Base}/ByEntityAnalysisModelDictionaryId/{entityAnalysisModelDictionaryId}: 500 user={user}",
                    e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByIdAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"GET {Base}/{id}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelDictionaryKvpService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.GetByIdAsync(id, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}/{id}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}/{id}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"GET {Base}/{id}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/{id}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> CreateAsync(
            [FromBody] EntityAnalysisModelDictionaryKvpDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"POST {Base}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelDictionaryKvpService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.InsertAsync(model, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}: 400 user={user} errors={ex.Result.Errors.Count}");

                return TypedResults.BadRequest(ex.Result);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"POST {Base}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> UpdateAsync(
            [FromBody] EntityAnalysisModelDictionaryKvpDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"PUT {Base}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelDictionaryKvpService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.UpdateAsync(model, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"PUT {Base}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"PUT {Base}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled) log.Warn($"PUT {Base}: 400 user={user} errors={ex.Result.Errors.Count}");

                return TypedResults.BadRequest(ex.Result);
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled) log.Warn($"PUT {Base}: 204 (not found) user={user}");

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"PUT {Base}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"PUT {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> DeleteAsync(
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            var id = await ResolveIdAsync(httpContext);
            if (id is null)
            {
                if (log.IsWarnEnabled) log.Warn($"DELETE {Base}: 400 (missing or invalid id) user={user}");

                return TypedResults.BadRequest();
            }

            if (log.IsDebugEnabled) log.Debug($"DELETE {Base}?id={id}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelDictionaryKvpService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                await service.DeleteAsync(id.Value, token);
                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"DELETE {Base}?id={id}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"DELETE {Base}?id={id}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled) log.Warn($"DELETE {Base}?id={id}: 204 (not found) user={user}");

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"DELETE {Base}?id={id}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"DELETE {Base}?id={id}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}