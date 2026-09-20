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
using Jube.Data.Context;
using Jube.Dto.Query.Completions;
using Jube.Service.Exceptions.Query.Completions;
using Jube.Service.Query.Completions;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CompletionsEndpoints
    {
        private const string Base = "/api/Completions";

        public static void MapCompletionsEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("Completions");

            group.MapGet("ByCaseWorkflowId",
                    (int caseWorkflowId, HttpContext httpContext, ILog log,
                            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                            IStringLocalizerFactory localizers, IServiceChangeBus bus, CancellationToken token) =>
                        RunAsync("ByCaseWorkflowId", false, httpContext, log, dynamicEnvironment, localizers, bus,
                            token, (s, t) => s.GetByCaseWorkflowIdAsync(caseWorkflowId, t)))
                .Produces<List<CompletionDto>>()
                .WithName("CompletionsGetByCaseWorkflowId");

            group.MapGet("ByCaseWorkflowGuidIncludingDeleted",
                    (Guid caseWorkflowGuid, HttpContext httpContext, ILog log,
                            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                            IStringLocalizerFactory localizers, IServiceChangeBus bus, CancellationToken token) =>
                        RunAsync("ByCaseWorkflowGuidIncludingDeleted", true, httpContext, log, dynamicEnvironment,
                            localizers, bus, token,
                            (s, t) => s.GetByCaseWorkflowGuidIncludingDeletedAsync(caseWorkflowGuid, t)))
                .Produces<List<CompletionDto>>()
                .WithName("CompletionsGetByCaseWorkflowGuidIncludingDeleted");

            group.MapGet("ByCaseWorkflowIdIncludingDeleted",
                    (int caseWorkflowId, HttpContext httpContext, ILog log,
                            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                            IStringLocalizerFactory localizers, IServiceChangeBus bus, CancellationToken token) =>
                        RunAsync("ByCaseWorkflowIdIncludingDeleted", true, httpContext, log, dynamicEnvironment,
                            localizers, bus, token,
                            (s, t) => s.GetByCaseWorkflowIdIncludingDeletedAsync(caseWorkflowId, t)))
                .Produces<List<CompletionDto>>()
                .WithName("CompletionsGetByCaseWorkflowIdIncludingDeleted");

            group.MapGet("ByEntityAnalysisModelId",
                    (int entityAnalysisModelId, HttpContext httpContext, ILog log,
                            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                            IStringLocalizerFactory localizers, IServiceChangeBus bus, CancellationToken token) =>
                        RunAsync("ByEntityAnalysisModelId", false, httpContext, log, dynamicEnvironment, localizers,
                            bus, token, (s, t) => s.GetByEntityAnalysisModelIdAsync(entityAnalysisModelId, t)))
                .Produces<List<CompletionDto>>()
                .WithName("CompletionsGetByEntityAnalysisModelId");

            group.MapGet("ByEntityAnalysisModelIdParseTypeId",
                    (int entityAnalysisModelId, int parseTypeId, HttpContext httpContext, ILog log,
                            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                            IStringLocalizerFactory localizers, IServiceChangeBus bus, CancellationToken token) =>
                        RunAsync("ByEntityAnalysisModelIdParseTypeId", false, httpContext, log, dynamicEnvironment,
                            localizers, bus, token,
                            (s, t) => s.GetByEntityAnalysisModelIdParseTypeIdAsync(entityAnalysisModelId,
                                parseTypeId, t)))
                .Produces<List<CompletionDto>>()
                .WithName("CompletionsGetByEntityAnalysisModelIdParseTypeId");
        }

        private static async Task<IResult> RunAsync(string route, bool keyNotFoundIsNoContent,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory localizers, IServiceChangeBus bus, CancellationToken token,
            Func<CompletionsService, CancellationToken, Task<List<CompletionDto>>> call)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/{route}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CompletionsService.CreateAsync(dbContext, user, log, localizers, bus, token);
                return TypedResults.Ok(await call(service, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/{route}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/{route}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/{route}: 404 user={user}");
                }

                return TypedResults.NotFound();
            }
            catch (CaseWorkflowNotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/{route}: 404 (case workflow not found) user={user}");
                }

                return TypedResults.NotFound();
            }
            catch (KeyNotFoundException) when (keyNotFoundIsNoContent)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/{route}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/{route}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}