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
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Dto.Query.CaseBulk;
using Jube.Service.Query.CaseBulk;
using Jube.Service.Query.CaseSupervision;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.Case;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;
using SupervisionExceptions = Jube.Service.Exceptions.Query.CaseSupervision;

namespace Jube.App.Endpoints.Query
{
    public static class CaseBulkEndpoints
    {
        private const string Base = "/api/CaseBulk";

        public static void MapCaseBulkEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseBulk");

            group.MapPost("Close", CloseAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseBulkResultDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseBulkClose");

            group.MapPost("Reassign", ReassignAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<CaseBulkResultDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseBulkReassign");
        }

        private static Task<IResult> CloseAsync([FromBody] CaseBulkCloseDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Engine.Helpers.JsonSerializationHelper jsonSerializationHelper,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Close", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, jsonSerializationHelper,
                async service => TypedResults.Ok(await service.CloseAsync(model, token)), token);
        }

        private static Task<IResult> ReassignAsync([FromBody] CaseBulkReassignDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            Engine.Helpers.JsonSerializationHelper jsonSerializationHelper, CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Reassign", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, jsonSerializationHelper,
                async service => TypedResults.Ok(await service.ReassignAsync(model, token)), token);
        }

        private static async Task<IResult> ExecuteAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Engine.Helpers.JsonSerializationHelper jsonSerializationHelper,
            Func<CaseBulkService, Task<IResult>> action, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var caseService = await CaseService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, jsonSerializationHelper, token);

                var supervisionService = await CaseSupervisionService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);

                return await action(new CaseBulkService(caseService, supervisionService, log));
            }
            catch (Exception ex) when (ex is CaseExceptions.NotAuthenticatedException
                                           or SupervisionExceptions.NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (Exception ex) when (ex is CaseExceptions.ForbiddenException
                                           or SupervisionExceptions.ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 403 user={user}");
                }

                return TypedResults.Forbid();
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
