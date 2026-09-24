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
using Jube.Service.Exceptions.Query.EntityAnalysisModelIntegrity;
using Jube.Service.Query.EntityAnalysisModelIntegrity;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    internal static class IntegrityEndpoints
    {
        internal static async Task<IResult> ExecuteAsync<T>(string route, HttpContext httpContext,
            Func<EntityAnalysisModelIntegrityService, CancellationToken, Task<T>> body, CancellationToken token)
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
                var service = await EntityAnalysisModelIntegrityService.CreateAsync(dbContext,
                    new InProcessEngineStateSource(services.GetService<Engine.Engine>()), user, log,
                    services.GetRequiredService<IStringLocalizerFactory>(),
                    services.GetRequiredService<IServiceChangeBus>(), token);
                return TypedResults.Ok(await body(service, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {route}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {route}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotFoundException ex)
            {
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