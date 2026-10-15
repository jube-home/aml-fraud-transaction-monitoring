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

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Dto.EntityApproval;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsInstance;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Jube.Service.Repository.AdvancedAnalyticsInstance.Models;

namespace Jube.App.Endpoints.AdvancedAnalytics
{
    public static class AdvancedAnalyticsInstanceEndpoints
    {
        private const string Base = "/api/AdvancedAnalyticsInstance";

        public static void MapAdvancedAnalyticsInstanceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("AdvancedAnalyticsInstance");

            group.MapGet("", ListAsync)
                .Produces<List<AdvancedAnalyticsInstanceDto>>()
                .WithName("AdvancedAnalyticsInstanceList");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<AdvancedAnalyticsInstanceDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("AdvancedAnalyticsInstanceGetById");

            group.MapDelete("{id:int}", DeleteAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("AdvancedAnalyticsInstanceDelete");

            group.MapPut("{id:int}/active", SetActiveAsync)
                .Produces<AdvancedAnalyticsInstanceDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("AdvancedAnalyticsInstanceSetActive");

            group.MapPost("{id:int}/Resubmit", ResubmitAsync)
                .Produces<AdvancedAnalyticsInstanceDto>()
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("AdvancedAnalyticsInstanceResubmit");

            group.MapPost("{id:int}/Retract", RetractAsync)
                .Produces<AdvancedAnalyticsInstanceDto>()
                .Produces((int)HttpStatusCode.Conflict)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("AdvancedAnalyticsInstanceRetract");
        }

        private static Task<IResult> ListAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token, int take = 200, int? afterId = null,
            int? entityAnalysisModelId = null, bool includeDeleted = false)
        {
            return AdvancedAnalyticsEndpointRunner.RunAsync("GET", Base, httpContext, log, dynamicEnvironment,
                async dbContext =>
                {
                    var service = await AdvancedAnalyticsInstanceService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token);

                    var result = await service.ListAsync(take, afterId, entityAnalysisModelId, includeDeleted,
                        token);

                    return TypedResults.Ok(result.Items);
                });
        }

        private static Task<IResult> GetByIdAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return AdvancedAnalyticsEndpointRunner.RunAsync("GET", $"{Base}/{id}", httpContext, log,
                dynamicEnvironment, async dbContext =>
                {
                    var service = await AdvancedAnalyticsInstanceService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token);

                    var result = await service.GetByIdAsync(id, token);

                    return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
                });
        }

        private static Task<IResult> DeleteAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return AdvancedAnalyticsEndpointRunner.RunAsync("DELETE", $"{Base}/{id}", httpContext, log,
                dynamicEnvironment, async dbContext =>
                {
                    var service = await AdvancedAnalyticsInstanceService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token);

                    await service.DeleteAsync(id, token);

                    return TypedResults.Ok();
                });
        }

        private static Task<IResult> SetActiveAsync(int id, SetActiveRequest request, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return AdvancedAnalyticsEndpointRunner.RunAsync("PUT", $"{Base}/{id}/active", httpContext, log,
                dynamicEnvironment, async dbContext =>
                {
                    if (request?.Active == null)
                    {
                        return TypedResults.BadRequest();
                    }

                    var service = await AdvancedAnalyticsInstanceService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token);

                    var result = await service.SetActiveAsync(id, request.Active.Value, token);

                    return TypedResults.Ok(result);
                });
        }

        private static Task<IResult> ResubmitAsync(int id, EntityApprovalActionRequestDto body,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return AdvancedAnalyticsEndpointRunner.RunAsync("POST", $"{Base}/{id}/Resubmit", httpContext, log,
                dynamicEnvironment, async dbContext =>
                {
                    var service = await AdvancedAnalyticsInstanceService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token);

                    var result = await service.ResubmitAsync(id, body?.Note, token);

                    return TypedResults.Ok(result);
                });
        }

        private static Task<IResult> RetractAsync(int id, EntityApprovalActionRequestDto body,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return AdvancedAnalyticsEndpointRunner.RunAsync("POST", $"{Base}/{id}/Retract", httpContext, log,
                dynamicEnvironment, async dbContext =>
                {
                    var service = await AdvancedAnalyticsInstanceService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token);

                    var result = await service.RetractAsync(id, body?.Note, token);

                    return TypedResults.Ok(result);
                });
        }

        public sealed record SetActiveRequest(bool? Active);
    }
}