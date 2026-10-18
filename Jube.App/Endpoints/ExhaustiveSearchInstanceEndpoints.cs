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
using Jube.Dto.ExhaustiveSearchInstance;
using Jube.Dto.EntityApproval;
using Jube.Service.Exceptions.ExhaustiveSearchInstance;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.ExhaustiveSearchInstance;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints
{
    public static class ExhaustiveSearchInstanceEndpoints
    {
        private const string Base = "/api/ExhaustiveSearchInstance";

        public static void MapExhaustiveSearchInstanceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("ExhaustiveSearchInstance");

            group.MapGet("", GetAsync)
                .Produces<List<ExhaustiveSearchInstanceDto>>()
                .WithName("ExhaustiveSearchInstanceGetAll");

            group.MapGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}", GetByEntityAnalysisModelIdAsync)
                .Produces<List<ExhaustiveSearchInstanceDto>>()
                .WithName("ExhaustiveSearchInstanceGetByEntityAnalysisModelId");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<ExhaustiveSearchInstanceDto>()
                .WithName("ExhaustiveSearchInstanceGetById");

            group.MapPost("", CreateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("ExhaustiveSearchInstanceCreate");

            group.MapPut("", UpdateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("ExhaustiveSearchInstanceUpdate");

            group.MapPut("stop/{guid:guid}", StopAsync)
                .Produces((int)HttpStatusCode.OK)
                .WithName("ExhaustiveSearchInstanceStop");

            group.MapPut("{id:int}/active", SetActiveAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("ExhaustiveSearchInstanceSetActive");

            group.MapDelete("{id:int}", DeleteAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("ExhaustiveSearchInstanceDelete");

            group.MapPost("{id:int}/Resubmit", ResubmitAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<object>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("ExhaustiveSearchInstanceResubmit");

            group.MapPost("{id:int}/Retract", RetractAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<object>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("ExhaustiveSearchInstanceRetract");
        }

        private static async Task<IResult> GetAsync(
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
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
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);

                var result = await service.GetAsync(token);

                return TypedResults.Ok(result);
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

        private static async Task<IResult> GetByEntityAnalysisModelIdAsync(
            int entityAnalysisModelId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);

                var result = await service.GetByEntityAnalysisModelIdAsync(entityAnalysisModelId, token);

                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByIdAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
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
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);

                var result = await service.GetByIdAsync(id, token);

                return TypedResults.Ok(result);
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

        private static async Task<IResult> CreateAsync(
            [FromBody] ExhaustiveSearchInstanceDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
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
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);

                var result = await service.InsertAsync(model, token);

                return TypedResults.Ok(result);
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

        private static async Task<IResult> UpdateAsync(
            [FromBody] ExhaustiveSearchInstanceDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"PUT {Base}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);

                var result = await service.UpdateAsync(model, token);

                return TypedResults.Ok(result);
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

        private static async Task<IResult> SetActiveAsync(
            int id, [FromBody] ExhaustiveSearchInstanceSetActiveRequest model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"PUT {Base}/{id}/active: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);

                var result = await service.SetActiveAsync(id, model.Active, token);

                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}/{id}/active: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}/{id}/active: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}/{id}/active: 204 (not found) user={user}");
                }

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"PUT {Base}/{id}/active: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"PUT {Base}/{id}/active: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> StopAsync(
            Guid guid, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"PUT {Base}/stop/{guid}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);

                await service.StopAsync(guid, token);

                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}/stop/{guid}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}/stop/{guid}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"PUT {Base}/stop/{guid}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"PUT {Base}/stop/{guid}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> DeleteAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
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
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);

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
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"DELETE {Base}/{id}: 400 user={user} errors={ex.Result.Errors.Count}");
                }

                return TypedResults.BadRequest(ex.Result);
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

        private static async Task<IResult> ResubmitAsync(
            int id, [FromBody] EntityApprovalActionRequestDto body, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/{id}/Resubmit: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.ResubmitAsync(id, body?.Note, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Resubmit: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Resubmit: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ApprovalRefusedException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Resubmit: 400 user={user} reason={ex.Message}");
                }

                return TypedResults.BadRequest(new { message = ex.Message, reasons = ex.Reasons });
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Resubmit: 204 (not found) user={user}");
                }

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/{id}/Resubmit: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/{id}/Resubmit: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> RetractAsync(
            int id, [FromBody] EntityApprovalActionRequestDto body, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/{id}/Retract: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await ExhaustiveSearchInstanceService.CreateAsync(
                    dbContext, user, log, stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.RetractAsync(id, body?.Note, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Retract: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Retract: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ApprovalRefusedException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Retract: 400 user={user} reason={ex.Message}");
                }

                return TypedResults.BadRequest(new { message = ex.Message, reasons = ex.Reasons });
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}/Retract: 204 (not found) user={user}");
                }

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/{id}/Retract: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/{id}/Retract: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }

    public sealed record ExhaustiveSearchInstanceSetActiveRequest(bool Active);
}