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
using Jube.Dto.Query.CaseWorkflowMacroExecution;
using Jube.Service.Exceptions.Query.CaseWorkflowMacroExecution;
using Jube.Service.Query.CaseWorkflowMacroExecution;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseWorkflowMacroExecutionEndpoints
    {
        private const string Base = "/api/CaseWorkflowMacroExecution";

        public static void MapCaseWorkflowMacroExecutionEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseWorkflowMacroExecution");

            group.MapPost("", ExecuteAsync)
                .Produces<CaseWorkflowMacroExecutionDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseWorkflowMacroExecutionExecute");
        }

        private static async Task<IResult> ExecuteAsync([FromBody] CaseWorkflowMacroExecutionDto model,
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
                var service = await CaseWorkflowMacroExecutionService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, jsonSerializationHelper, token);
                return TypedResults.Ok(await service.ExecuteAsync(model, token));
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
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 400 (not found) user={user}");
                }

                return TypedResults.BadRequest();
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
    }
}