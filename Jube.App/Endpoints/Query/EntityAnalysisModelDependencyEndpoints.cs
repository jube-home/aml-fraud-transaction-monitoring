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
using Jube.Dto.Query.EntityAnalysisModelDependency;
using Jube.Service.Exceptions;
using Jube.Service.Query.EntityAnalysisModelDependency;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class EntityAnalysisModelDependencyEndpoints
    {
        private const string Base = "/api/EntityAnalysisModelDependency";

        public static void MapEntityAnalysisModelDependencyEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("EntityAnalysisModelDependency");

            group.MapGet("Dependents/{entityAnalysisModelId:int}/{kind}/{id:int}", DependentsAsync)
                .Produces<DependencyImpactDto>()
                .WithName("EntityAnalysisModelDependencyDependents");
            group.MapGet("Dependencies/{entityAnalysisModelId:int}/{kind}/{id:int}", DependenciesAsync)
                .Produces<DependencyListDto>()
                .WithName("EntityAnalysisModelDependencyDependencies");
        }

        private static Task<IResult> DependentsAsync(int entityAnalysisModelId, string kind, int id,
            HttpContext httpContext, CancellationToken token)
        {
            return ExecuteAsync($"{Base}/Dependents", httpContext,
                (service, cancellation) => service.DependentsAsync(entityAnalysisModelId, kind, id, cancellation),
                token);
        }

        private static Task<IResult> DependenciesAsync(int entityAnalysisModelId, string kind, int id,
            HttpContext httpContext, CancellationToken token)
        {
            return ExecuteAsync($"{Base}/Dependencies", httpContext,
                (service, cancellation) => service.DependenciesAsync(entityAnalysisModelId, kind, id, cancellation),
                token);
        }

        private static async Task<IResult> ExecuteAsync<T>(string route, HttpContext httpContext,
            Func<EntityAnalysisModelDependencyService, CancellationToken, Task<T>> body, CancellationToken token)
        {
            var services = httpContext.RequestServices;
            var log = services.GetRequiredService<ILog>();
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {route}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                services.GetRequiredService<DynamicEnvironment.DynamicEnvironment>().AppSettings("ConnectionString"),
                log);
            try
            {
                var service = await EntityAnalysisModelDependencyService.CreateAsync(dbContext, user, log,
                    services.GetRequiredService<IStringLocalizerFactory>(),
                    services.GetRequiredService<IServiceChangeBus>(), token).ConfigureAwait(false);
                return TypedResults.Ok(await body(service, token).ConfigureAwait(false));
            }
            catch (ServiceException ex) when (ex.Code is "PermissionDenied" or "NotAuthenticated")
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {route}: 403 ({ex.Code}) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ServiceException ex) when (ex.Code == "NotFound")
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {route}: 404 user={user}");
                }

                return TypedResults.NotFound(new { ex.Message });
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {route}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {route}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}