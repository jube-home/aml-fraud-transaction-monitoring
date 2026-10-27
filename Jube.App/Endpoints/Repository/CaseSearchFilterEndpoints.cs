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
using Jube.Dto.Repository.CaseSearchFilter;
using Jube.Service.Exceptions.Repository.CaseSearchFilter;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseSearchFilter;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseSearchFilterEndpoints
    {
        private const string Base = "/api/CaseSearchFilter";

        public static void MapCaseSearchFilterEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseSearchFilter");

            group.MapGet("", ListAsync)
                .Produces<List<CaseSearchFilterDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchFilterList");

            group.MapGet("ByGuid/{guid:Guid}", GetByGuidAsync)
                .Produces<CaseSearchFilterDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchFilterGet");

            group.MapPost("", InsertAsync)
                .Produces<CaseSearchFilterDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchFilterCreate");

            group.MapPut("", UpdateAsync)
                .Produces<CaseSearchFilterDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchFilterUpdate");

            group.MapDelete("{guid:Guid}", DeleteAsync)
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSearchFilterDelete");
        }

        private static Task<IResult> ListAsync([FromQuery] Guid? caseWorkflowGuid, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync("GET", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.ListAsync(caseWorkflowGuid, token)),
                token);
        }

        private static Task<IResult> GetByGuidAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync("GET", $"{Base}/ByGuid/{guid}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetByGuidAsync(guid, token)), token);
        }

        private static Task<IResult> InsertAsync([FromBody] CaseSearchFilterDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync("POST", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.InsertAsync(model, token)), token);
        }

        private static Task<IResult> UpdateAsync([FromBody] CaseSearchFilterDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunAsync("PUT", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.UpdateAsync(model, token)), token);
        }

        private static Task<IResult> DeleteAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync("DELETE", $"{Base}/{guid}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, async service =>
                {
                    await service.DeleteAsync(guid, token);

                    return TypedResults.NoContent();
                }, token);
        }

        private static async Task<IResult> RunAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<CaseSearchFilterService, Task<IResult>> action,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"{method} {route}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseSearchFilterService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);

                return await action(service);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 403 (not authenticated) user={user}");
                }

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
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 400 user={user} errors={ex.Result.Errors.Count}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 404 user={user}");
                }

                return TypedResults.NotFound();
            }
            catch (VersionConflictException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 409 user={user} supplied={ex.SuppliedVersion} current={ex.CurrentVersion}");
                }

                return TypedResults.Conflict(new
                {
                    code = "VersionConflict",
                    message = ex.Message,
                    suppliedVersion = ex.SuppliedVersion,
                    currentVersion = ex.CurrentVersion
                });
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{method} {route}: client cancelled user={user}");
                }

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
