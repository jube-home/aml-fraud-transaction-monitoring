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
using Jube.Service.Exceptions.Query.CaseJournal;
using Jube.Service.Query.CaseJournal;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseJournalEndpoints
    {
        private const int MaximumLimit = 1000;
        private const string Base = "/api/GetCaseJournalQuery";

        private static readonly JsonSerializerSettings serializerSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        public static void MapCaseJournalEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseJournal");

            group.MapGet("", GetAsync)
                .Produces<Jube.Dto.Query.CaseJournal.CaseJournalDto>()
                .WithName("CaseJournalGet");
        }

        private static async Task<IResult> GetAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token,
            string drillName = null, string drillValue = null, Guid? caseWorkflowGuid = null, int limit = 0,
            bool activationsOnly = false, double responseElevation = 0)
        {
            var user = httpContext.User.Identity?.Name;
            if (string.IsNullOrEmpty(drillName) || drillValue == null)
            {
                return TypedResults.BadRequest();
            }

            if (limit < 0)
            {
                return TypedResults.BadRequest();
            }

            limit = Math.Min(limit, MaximumLimit);

            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseJournalService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus,
                    dynamicEnvironment.ParserAssertSelectOnly(),
                    dynamicEnvironment.AppSettings("ReportConnectionString"), token);
                var result = await service.GetAsync(drillName, drillValue, caseWorkflowGuid.GetValueOrDefault(), limit,
                    activationsOnly,
                    responseElevation, token);
                return TypedResults.Content(JsonConvert.SerializeObject(result, serializerSettings),
                    "application/json");
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
    }
}