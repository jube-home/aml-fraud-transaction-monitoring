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
using Jube.Data.Concurrency;
using Jube.Data.Context;
using Jube.Dto.EntityApproval;
using Jube.Dto.Repository.EntityAnalysisModelSvirfd;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Service.Exceptions.Repository.EntityAnalysisModelSvirfd;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.EntityAnalysisModelSvirfd;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class EntityAnalysisModelSvirfdEndpoints
    {
        private const string Base = "/api/EntityAnalysisModelSvirfd";

        public static void MapEntityAnalysisModelSvirfdEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("EntityAnalysisModelSvirfd");

            group.MapGet("", GetAsync)
                .Produces<List<EntityAnalysisModelSvirfdDto>>()
                .WithName("EntityAnalysisModelSvirfdList");

            group.MapGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}", GetByEntityAnalysisModelIdAsync)
                .Produces<List<EntityAnalysisModelSvirfdDto>>()
                .WithName("EntityAnalysisModelSvirfdListByEntityAnalysisModelId");

            group.MapGet("Fields/{entityAnalysisModelId:int}", FieldsAsync)
                .Produces<List<EntityAnalysisModelSvirfdFieldDto>>()
                .WithName("EntityAnalysisModelSvirfdFields");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<EntityAnalysisModelSvirfdDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("EntityAnalysisModelSvirfdGetById");

            group.MapPost("", InsertAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<EntityAnalysisModelSvirfdDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("EntityAnalysisModelSvirfdCreate");

            group.MapPut("", UpdateAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<EntityAnalysisModelSvirfdDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("EntityAnalysisModelSvirfdUpdate");

            group.MapDelete("{id:int}", DeleteAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("EntityAnalysisModelSvirfdDelete");

            group.MapPost("{id:int}/Resubmit", ResubmitAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<EntityAnalysisModelSvirfdDto>()
                .Produces<object>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("EntityAnalysisModelSvirfdResubmit");

            group.MapPost("{id:int}/Retract", RetractAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<EntityAnalysisModelSvirfdDto>()
                .Produces<object>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("EntityAnalysisModelSvirfdRetract");
        }

        private static Task<IResult> GetAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetAsync(token)), token);
        }

        private static Task<IResult> GetByEntityAnalysisModelIdAsync(int entityAnalysisModelId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetByEntityAnalysisModelIdAsync(entityAnalysisModelId, token)), token);
        }

        private static Task<IResult> FieldsAsync(int entityAnalysisModelId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Fields/{entityAnalysisModelId}", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.FieldsAsync(entityAnalysisModelId, token)), token);
        }

        private static Task<IResult> GetByIdAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/{id}", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => await service.GetByIdAsync(id, token) is { } dto
                    ? TypedResults.Ok(dto)
                    : TypedResults.NotFound(), token);
        }

        private static Task<IResult> InsertAsync([FromBody] EntityAnalysisModelSvirfdDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("POST", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.InsertAsync(model, token)), token);
        }

        private static Task<IResult> UpdateAsync([FromBody] EntityAnalysisModelSvirfdDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("PUT", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.UpdateAsync(model, token)), token);
        }

        private static Task<IResult> DeleteAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("DELETE", $"{Base}/{id}", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service =>
                {
                    try
                    {
                        await service.DeleteAsync(id, token);
                    }
                    catch (NotFoundException)
                    {
                        return TypedResults.NoContent();
                    }

                    return TypedResults.NoContent();
                }, token);
        }

        private static Task<IResult> ResubmitAsync(int id, [FromBody] EntityApprovalActionRequestDto body,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/{id}/Resubmit", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => await MutatedOrGoneAsync(() => service.ResubmitAsync(id, body?.Note, token)), token);
        }

        private static Task<IResult> RetractAsync(int id, [FromBody] EntityApprovalActionRequestDto body,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/{id}/Retract", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => await MutatedOrGoneAsync(() => service.RetractAsync(id, body?.Note, token)), token);
        }

        private static async Task<IResult> MutatedOrGoneAsync(Func<Task<EntityAnalysisModelSvirfdDto>> action)
        {
            try
            {
                return TypedResults.Ok(await action());
            }
            catch (NotFoundException)
            {
                return TypedResults.NoContent();
            }
        }

        private static Task<IResult> ExecuteAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<EntityAnalysisModelSvirfdService, Task<IResult>> action, CancellationToken token)
        {
            return GuardAsync(method, route, httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await EntityAnalysisModelSvirfdService.CreateAsync(dbContext, httpContext.User.Identity?.Name, log,
                    stringLocalizerFactory, serviceChangeBus, token);

                return await action(service);
            });
        }

        private static async Task<IResult> GuardAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, Func<DbContext, Task<IResult>> action)
        {
            var user = httpContext.User.Identity?.Name;
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(dbContext);
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
            catch (ApprovalRefusedException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 400 user={user} reason={ex.Message}");
                }

                return TypedResults.BadRequest(new { message = ex.Message, reasons = ex.Reasons });
            }
            catch (NotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (VersionConflictException ex)
            {
                return TypedResults.Conflict(ex.Message);
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
