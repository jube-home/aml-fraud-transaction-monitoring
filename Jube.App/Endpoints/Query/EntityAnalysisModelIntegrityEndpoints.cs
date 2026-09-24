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
using System.Threading;
using System.Threading.Tasks;
using Jube.Dto.Query.EntityAnalysisModelIntegrity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Jube.App.Endpoints.Query
{
    public static class EntityAnalysisModelIntegrityEndpoints
    {
        private const string Base = "/api/EntityAnalysisModelIntegrity";

        public static void MapEntityAnalysisModelIntegrityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("EntityAnalysisModelIntegrity");

            group.MapGet("Models", ModelsAsync)
                .Produces<List<IntegrityModelDto>>()
                .WithName("EntityAnalysisModelIntegrityModels");

            group.MapGet("{entityAnalysisModelId:int}", CheckAsync)
                .Produces<ModelIntegrityReportDto>()
                .WithName("EntityAnalysisModelIntegrityCheck");
        }

        private static Task<IResult> ModelsAsync(HttpContext httpContext, CancellationToken token)
        {
            return IntegrityEndpoints.ExecuteAsync(Base + "/Models", httpContext,
                (service, t) => service.ModelsAsync(t), token);
        }

        private static Task<IResult> CheckAsync(int entityAnalysisModelId, HttpContext httpContext,
            CancellationToken token)
        {
            return IntegrityEndpoints.ExecuteAsync(Base, httpContext,
                (service, t) => service.CheckAsync(entityAnalysisModelId, t), token);
        }
    }
}