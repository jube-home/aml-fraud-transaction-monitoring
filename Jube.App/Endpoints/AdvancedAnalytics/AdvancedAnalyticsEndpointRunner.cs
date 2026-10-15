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
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Service.Exceptions.AdvancedAnalytics;
using log4net;
using Microsoft.AspNetCore.Http;

namespace Jube.App.Endpoints.AdvancedAnalytics
{
    internal static class AdvancedAnalyticsEndpointRunner
    {
        public static async Task<IResult> RunAsync(string verb, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, Func<DbContext, Task<IResult>> action)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"{verb} {route}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(dbContext);
            }
            catch (NotAuthenticatedException)
            {
                Warn(log, $"{verb} {route}: 401 user={user}");

                return TypedResults.Unauthorized();
            }
            catch (ForbiddenException)
            {
                Warn(log, $"{verb} {route}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                Warn(log, $"{verb} {route}: 404 user={user}");

                return TypedResults.NotFound();
            }
            catch (DtoValidationException ex)
            {
                Warn(log, $"{verb} {route}: 400 user={user} errors={ex.Result.Errors.Count}");

                return TypedResults.BadRequest(ex.Result);
            }
            catch (ConflictException ex)
            {
                Warn(log, $"{verb} {route}: 409 user={user} reason={ex.Reason}");

                return TypedResults.Conflict(new ConflictBody(ex.Reason, ex.Message));
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{verb} {route}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"{verb} {route}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static void Warn(ILog log, string message)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn(message);
            }
        }

        public sealed record ConflictBody(string Reason, string Message);
    }
}