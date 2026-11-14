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
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Dto.Repository.CaseAgentRun;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseAgentRun;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseAgentRunEndpoints
    {
        private const string Base = "/api/CaseAgentRun";

        public static void MapCaseAgentRunEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base).RequireAuthorization().WithTags("CaseAgentRun");

            group.MapGet("ByCaseId/{caseId:int}", ListAsync)
                .Produces<List<CaseAgentRunDto>>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseAgentRunByCaseId");

            group.MapPost("Decide", DecideAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseAgentRunDecide");
        }

        private static Task<IResult> ListAsync(int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus, CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus,
                async service => TypedResults.Ok(await service.ListAsync(caseId, token)), token);
        }

        private static Task<IResult> DecideAsync([FromBody] CaseAgentRunDecisionDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            CancellationToken token)
        {
            return ExecuteAsync(httpContext, log, dynamicEnvironment, bus, async service =>
            {
                await service.DecideAsync(model, token);

                return TypedResults.NoContent();
            }, token);
        }

        private static async Task<IResult> ExecuteAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IServiceChangeBus bus,
            Func<CaseAgentRunService, Task<IResult>> action, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(await CaseAgentRunService.CreateAsync(dbContext, user, log, bus, token));
            }
            catch (Exception ex) when (ex is CaseExceptions.NotAuthenticatedException
                                           or CaseExceptions.ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (CaseExceptions.NotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (CaseExceptions.ConflictException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
        }
    }
}
