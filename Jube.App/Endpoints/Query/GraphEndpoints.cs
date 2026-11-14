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
using Jube.Dto.Query.Graph;
using Jube.Service.Exceptions.Query.Graph;
using Jube.Service.Query.Graph;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class GraphEndpoints
    {
        private const string Base = "/api/Graph";

        public static void MapGraphEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("Graph");

            group.MapPost("Neighbourhood", NeighbourhoodAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphNeighbourhoodDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphNeighbourhoodGet");

            group.MapPost("Explode", ExplodeAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphExplodeDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphExplodeGet");

            group.MapGet("Edge/{edgeId:long}/Transactions", EdgeTransactionsAsync)
                .Produces<GraphEdgeTransactionsDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphEdgeTransactionsGet");

            group.MapGet("Explorations", ExplorationsAsync)
                .Produces<List<GraphExplorationDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphExplorationList");

            group.MapPut("Explorations", ExplorationSaveAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphExplorationDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphExplorationSave");

            group.MapDelete("Explorations/{id:int}", ExplorationDeleteAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphExplorationDelete");

            group.MapPost("Render", RenderAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphRenderResultDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphRenderSvg");

            group.MapPost("TimeStrip/Svg", TimeStripAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<string>(contentType: "image/svg+xml")
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphTimeStripSvg");

            group.MapPost("Capture", CaptureAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphCaptureResultDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphCaptureCreate");

            group.MapPost("Paths", PathsAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphPathsDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphPathsGet");

            group.MapPost("Flow", FlowAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphFlowDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphFlowGet");

            group.MapPost("Patterns/Search", PatternsAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<List<GraphPatternDto>>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphPatternList");

            group.MapGet("Patterns/{id:long}", PatternAsync)
                .Produces<GraphPatternDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphPatternGet");

            group.MapPost("Patterns/{id:long}/Decide", PatternDecideAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphPatternDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphPatternDecide");

            group.MapGet("Communities", CommunitiesAsync)
                .Produces<List<GraphCommunityDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphCommunityList");

            group.MapGet("Communities/{id:long}", CommunityAsync)
                .Produces<GraphCommunityDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphCommunityGet");

            group.MapGet("Hubs", HubsAsync)
                .Produces<List<GraphNodeDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphHubList");

            group.MapPost("Hubs/Override", HubOverrideAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphNodeDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphHubOverride");

            group.MapPost("Node", NodeAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<GraphNodeFactsDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("GraphNodeGet");
        }

        private static Task<IResult> NeighbourhoodAsync([FromBody] GraphNeighbourhoodRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Neighbourhood", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetNeighbourhoodAsync(request, token)), token);
        }

        private static Task<IResult> ExplodeAsync([FromBody] GraphExplodeRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Explode", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetExplodeAsync(request, token)),
                token);
        }

        private static Task<IResult> EdgeTransactionsAsync(long edgeId, [FromQuery] DateTime? from,
            [FromQuery] DateTime? to, [FromQuery] int? limit, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var request = new GraphEdgeTransactionsRequestDto { EdgeId = edgeId, From = from, To = to, Limit = limit };

            return ExecuteAsync("GET", $"{Base}/Edge/{edgeId}/Transactions", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetEdgeTransactionsAsync(request, token)), token);
        }

        private static Task<IResult> ExplorationsAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("GET", $"{Base}/Explorations", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetExplorationsAsync(token)), token);
        }

        private static Task<IResult> ExplorationSaveAsync([FromBody] GraphExplorationDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("PUT", $"{Base}/Explorations", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.SaveExplorationAsync(request, token)),
                token);
        }

        private static Task<IResult> ExplorationDeleteAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("DELETE", $"{Base}/Explorations/{id}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, async service =>
                {
                    await service.DeleteExplorationAsync(id, token);

                    return TypedResults.NoContent();
                }, token);
        }

        private static Task<IResult> RenderAsync([FromBody] GraphRenderRequestDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Render", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.RenderAsync(request, token)), token);
        }

        private static Task<IResult> TimeStripAsync([FromBody] GraphNeighbourhoodRequestDto request,
            [FromQuery] int? bins, [FromQuery] bool? dark, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/TimeStrip/Svg", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => Results.Content(await service.GetTimeStripSvgAsync(request, bins, dark ?? false, token),
                    "image/svg+xml"), token);
        }

        private static Task<IResult> CaptureAsync([FromBody] GraphCaptureRequestDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Capture", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.CaptureAsync(request, token)), token);
        }

        private static Task<IResult> PathsAsync([FromBody] GraphPathsRequestDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Paths", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetPathsAsync(request, token)), token);
        }

        private static Task<IResult> FlowAsync([FromBody] GraphFlowRequestDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Flow", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetFlowAsync(request, token)), token);
        }

        private static Task<IResult> PatternsAsync([FromBody] GraphPatternListRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAnalyticsAsync("POST", $"{Base}/Patterns/Search", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetPatternsAsync(request, token)), token);
        }

        private static Task<IResult> PatternAsync(long id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAnalyticsAsync("GET", $"{Base}/Patterns/{id}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetPatternAsync(id, token)), token);
        }

        private static Task<IResult> PatternDecideAsync(long id, [FromBody] GraphPatternDecideRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            request.Id = id;

            return ExecuteAnalyticsAsync("POST", $"{Base}/Patterns/{id}/Decide", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.DecidePatternAsync(request, token)), token);
        }

        private static Task<IResult> CommunitiesAsync([FromQuery] int? limit, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAnalyticsAsync("GET", $"{Base}/Communities", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetCommunitiesAsync(limit, token)), token);
        }

        private static Task<IResult> CommunityAsync(long id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAnalyticsAsync("GET", $"{Base}/Communities/{id}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetCommunityAsync(id, token)), token);
        }

        private static Task<IResult> HubsAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return ExecuteAnalyticsAsync("GET", $"{Base}/Hubs", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.GetHubsAsync(token)), token);
        }

        private static Task<IResult> HubOverrideAsync([FromBody] GraphHubOverrideRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAnalyticsAsync("POST", $"{Base}/Hubs/Override", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus,
                async service => TypedResults.Ok(await service.OverrideHubAsync(request, token)), token);
        }

        private static Task<IResult> NodeAsync([FromBody] GraphNodeRequestDto request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return ExecuteAsync("POST", $"{Base}/Node", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, async service => TypedResults.Ok(await service.GetNodeAsync(request, token)), token);
        }

        private static Task<IResult> ExecuteAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<GraphService, Task<IResult>> action, CancellationToken token)
        {
            return GuardAsync(method, route, httpContext, log, dynamicEnvironment, async (dbContext, user) =>
                await action(await GraphService.CreateAsync(dbContext, user, log, stringLocalizerFactory, serviceChangeBus,
                    token)));
        }

        private static Task<IResult> ExecuteAnalyticsAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, Func<GraphAnalyticsService, Task<IResult>> action,
            CancellationToken token)
        {
            return GuardAsync(method, route, httpContext, log, dynamicEnvironment, async (dbContext, user) =>
                await action(await GraphAnalyticsService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token)));
        }

        private static async Task<IResult> GuardAsync(string method, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, Func<DbContext, string, Task<IResult>> action)
        {
            var user = httpContext.User.Identity?.Name;
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await action(dbContext, user);
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
