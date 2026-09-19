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
using Jube.Dto.Repository.VisualisationRegistryDatasourceSeries;
using Jube.Service.Exceptions.Repository.VisualisationRegistryDatasourceSeries;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.VisualisationRegistryDatasourceSeries;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class VisualisationRegistryDatasourceSeriesEndpoints
    {
        private const string Base = "/api/VisualisationRegistryDatasourceSeries";

        public static void MapVisualisationRegistryDatasourceSeriesEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("VisualisationRegistryDatasourceSeries");

            group.MapGet("ByVisualisationRegistryDatasourceId/{id:int}", ByVisualisationRegistryDatasourceIdAsync)
                .Produces<List<VisualisationRegistryDatasourceSeriesDto>>()
                .WithName("VisualisationRegistryDatasourceSeriesListByVisualisationRegistryDatasourceId");
        }

        private static async Task<IResult> ByVisualisationRegistryDatasourceIdAsync(int id, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ByVisualisationRegistryDatasourceId/{id}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await VisualisationRegistryDatasourceSeriesService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                return TypedResults.Ok(
                    await service.GetByVisualisationRegistryDatasourceIdAsync(id, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByVisualisationRegistryDatasourceId/{id}: 403 (not authenticated) " +
                             $"user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByVisualisationRegistryDatasourceId/{id}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/ByVisualisationRegistryDatasourceId/{id}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByVisualisationRegistryDatasourceId/{id}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}