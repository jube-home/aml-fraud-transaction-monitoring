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

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Dto.Query.Svirfd;
using Jube.Service.Exceptions.Query.Svirfd;
using Jube.Service.Query.Svirfd;
using Jube.Svirfd.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class SvirfdEndpoints
    {
        private const string Base = "/api/Svirfd";

        public static void MapSvirfdEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base).RequireAuthorization().WithTags("Svirfd");

            group.MapGet("Lookup/{ip}", LookupAsync)
                .Produces<SvirfdLookupDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.ServiceUnavailable)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("SvirfdLookup");

            group.MapGet("Status", StatusAsync)
                .Produces<SvirfdStatusDto>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("SvirfdStatus");
        }

        private static Task<IResult> LookupAsync(string ip, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            CancellationToken token)
        {
            return GuardAsync("GET", $"{Base}/Lookup", httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await SvirfdService.CreateAsync(dbContext, httpContext.User.Identity?.Name, log, Lookup(httpContext),
                    stringLocalizerFactory, token);

                return TypedResults.Ok(await service.LookupAsync(ip, token));
            });
        }

        private static Task<IResult> StatusAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            CancellationToken token)
        {
            return GuardAsync("GET", $"{Base}/Status", httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var service = await SvirfdService.CreateAsync(dbContext, httpContext.User.Identity?.Name, log, Lookup(httpContext),
                    stringLocalizerFactory, token);

                return TypedResults.Ok(service.Status());
            });
        }

        private static ISvirfdLookup Lookup(HttpContext httpContext)
        {
            return httpContext.RequestServices.GetService<ISvirfdLookup>() ?? new Jube.Svirfd.SvirfdNullLookup();
        }

        private static async Task<IResult> GuardAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            System.Func<DbContext, Task<IResult>> action)
        {
            var user = httpContext.User.Identity?.Name;
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(dbContext);
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (InvalidAddressException ex)
            {
                return TypedResults.BadRequest(new { error = ex.Message });
            }
            catch (IndexLoadingException ex)
            {
                return TypedResults.Json(new { error = ex.Message }, statusCode: (int)HttpStatusCode.ServiceUnavailable);
            }
            catch (NotFoundException ex)
            {
                return TypedResults.NotFound(new { error = ex.Message });
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                log.Error($"{method} {route}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
