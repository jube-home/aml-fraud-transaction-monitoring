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
using Jube.Data.Context;
using Jube.Dto.Repository.CaseWorkflowFormEntry;
using Jube.Service.Repository.CaseWorkflowFormEntry;
using Jube.Service.Exceptions.Repository.CaseWorkflowFormEntry;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseWorkflowFormEntryEndpoints
    {
        private const string Base = "/api/CaseWorkflowFormEntry";

        public static void MapCaseWorkflowFormEntryEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseWorkflowFormEntry");

            group.MapPost("", InsertAsync)
                .Produces<CaseWorkflowFormEntryDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseWorkflowFormEntryCreate");

            group.MapGet("ByCaseKeyValue", GetByCaseKeyValueAsync)
                .Produces<System.Collections.Generic.List<CaseWorkflowFormEntryDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseWorkflowFormEntryListByCaseKeyValue");
        }

        private static async Task<IResult> InsertAsync([FromBody] CaseWorkflowFormEntryDto model,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            Engine.Helpers.JsonSerializationHelper jsonSerializationHelper, CancellationToken token)
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
                var service = await CaseWorkflowFormEntryService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, jsonSerializationHelper, token);
                var saved = await service.InsertAsync(model, token);
                return TypedResults.Ok(saved);
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
                    log.Warn($"POST {Base}: 400 (validation) user={user}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
            catch (NotFoundException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 400 (not found) user={user}");
                }

                return TypedResults.BadRequest(ex.Message);
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

        private static async Task<IResult> GetByCaseKeyValueAsync(string key, string value, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            Engine.Helpers.JsonSerializationHelper jsonSerializationHelper, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ByCaseKeyValue: entry key={key} value={value} user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseWorkflowFormEntryService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, jsonSerializationHelper, token);
                return TypedResults.Ok(await service.GetByCaseKeyValueAsync(key, value, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCaseKeyValue: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCaseKeyValue: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/ByCaseKeyValue: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByCaseKeyValue: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}