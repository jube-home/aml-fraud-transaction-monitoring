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
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsModel;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Jube.Service.Repository.AdvancedAnalyticsModel.Models;

namespace Jube.App.Endpoints.AdvancedAnalytics
{
    public static class AdvancedAnalyticsModelEndpoints
    {
        private const string Base = "/api/AdvancedAnalyticsModel";

        public static void MapAdvancedAnalyticsModelEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("AdvancedAnalyticsModel");

            group.MapGet("", ListAsync)
                .Produces<List<AdvancedAnalyticsTrainedModelDto>>()
                .WithName("AdvancedAnalyticsModelList");

            group.MapGet("{id:int}", GetAsync)
                .Produces<AdvancedAnalyticsTrainedModelDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("AdvancedAnalyticsModelGet");

            group.MapGet("{id:int}/versions", ListVersionsAsync)
                .Produces<List<AdvancedAnalyticsModelVersionDto>>()
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("AdvancedAnalyticsModelListVersions");

            group.MapGet("Version/{versionId:int}", GetVersionAsync)
                .Produces<AdvancedAnalyticsModelVersionDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("AdvancedAnalyticsModelGetVersion");

            group.MapGet("Version/{versionId:int}/PromotionEvidence", GetPromotionEvidenceAsync)
                .Produces<AdvancedAnalyticsPromotionEvidenceDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("AdvancedAnalyticsModelGetPromotionEvidence");

            group.MapGet("Compare/{versionIdA:int}/{versionIdB:int}", CompareAsync)
                .Produces<AdvancedAnalyticsVersionComparisonDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("AdvancedAnalyticsModelCompareVersions");
        }

        private static Task<IResult> RunAsync(string verb, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token,
            Func<AdvancedAnalyticsModelService, Task<IResult>> action)
        {
            return AdvancedAnalyticsEndpointRunner.RunAsync(verb, route, httpContext, log, dynamicEnvironment,
                async dbContext => await action(await AdvancedAnalyticsModelService.CreateAsync(dbContext,
                    httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token)));
        }

        private static Task<IResult> ListAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token, int take = 200, int? afterId = null,
            int? advancedAnalyticsInstanceId = null)
        {
            return RunAsync("GET", Base, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, token, async service =>
                    TypedResults.Ok((await service.ListModelsAsync(take, afterId, advancedAnalyticsInstanceId,
                        token)).Items));
        }

        private static Task<IResult> GetAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync("GET", $"{Base}/{id}", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, token, async service =>
                {
                    var result = await service.GetModelAsync(id, token);

                    return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
                });
        }

        private static Task<IResult> ListVersionsAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync("GET", $"{Base}/{id}/versions", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, token, async service =>
                    TypedResults.Ok(await service.ListVersionsAsync(id, token)));
        }

        private static Task<IResult> GetVersionAsync(int versionId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync("GET", $"{Base}/Version/{versionId}", httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, token, async service =>
                {
                    var result = await service.GetVersionAsync(versionId, token);

                    return result == null ? TypedResults.NotFound() : TypedResults.Ok(result);
                });
        }

        private static Task<IResult> GetPromotionEvidenceAsync(int versionId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync("GET", $"{Base}/Version/{versionId}/PromotionEvidence", httpContext, log,
                dynamicEnvironment, stringLocalizerFactory, serviceChangeBus, token, async service =>
                    TypedResults.Ok(await service.GetPromotionEvidenceAsync(versionId, token)));
        }

        private static Task<IResult> CompareAsync(int versionIdA, int versionIdB, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync("GET", $"{Base}/Compare/{versionIdA}/{versionIdB}", httpContext, log,
                dynamicEnvironment, stringLocalizerFactory, serviceChangeBus, token, async service =>
                    TypedResults.Ok(await service.CompareVersionsAsync(versionIdA, versionIdB, token)));
        }
    }
}