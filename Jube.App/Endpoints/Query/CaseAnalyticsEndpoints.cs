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
using Jube.Dto.Options;
using Jube.Dto.Query.CaseAnalytics;
using Jube.Service.Charts;
using Jube.Service.Exceptions.Query.CaseAnalytics;
using Jube.Service.Query.CaseAnalytics;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseAnalyticsEndpoints
    {
        private const string Base = "/api/CaseAnalytics";

        public static void MapCaseAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseAnalytics");

            group.MapGet("", GetAsync)
                .Produces<CaseAnalyticsDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseAnalyticsGet");

            group.MapGet("Chart/{kind}", ChartAsync)
                .Produces<string>(contentType: "image/svg+xml")
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseAnalyticsChart");
        }

        private static Task<IResult> GetAsync([AsParameters] CaseAnalyticsRequestDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("GET", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetAsync(request, token)), token);
        }

        private static Task<IResult> ChartAsync(CaseAnalyticsChart kind, [AsParameters] CaseAnalyticsRequestDto request,
            [FromQuery] bool? dark, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Chart/{kind}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, async service =>
                {
                    var report = await service.GetAsync(request, token);

                    return Results.Content(CaseAnalyticsChartRenderer.Render(kind, report, dark ?? false),
                        "image/svg+xml");
                }, token);
        }

        private static async Task<IResult> ExecuteAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<CaseAnalyticsService, Task<IResult>> action,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseAnalyticsService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, dynamicEnvironment, token);

                return await action(service);
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {route}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                return TypedResults.BadRequest(ex.Result);
            }
            catch (NotFoundException)
            {
                return TypedResults.NotFound();
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
