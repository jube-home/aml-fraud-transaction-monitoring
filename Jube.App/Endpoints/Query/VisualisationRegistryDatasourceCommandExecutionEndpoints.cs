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
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Service.Exceptions.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Service.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class VisualisationRegistryDatasourceCommandExecutionEndpoints
    {
        private const string CardBase = "/api/VisualisationRegistryDatasource";


        public static void MapVisualisationRegistryDatasourceCommandExecutionEndpoints(
            this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGroup(CardBase)
                .RequireAuthorization()
                .WithTags("VisualisationRegistryDatasourceCommandExecution")
                .MapPost("{id:int}/Card", CardAsync)
                .Produces<DatasourceCardDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("VisualisationRegistryDatasourceCommandExecutionCard");
        }

        private static async Task<IResult> CardAsync(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DatasourceCardRequestDto request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {CardBase}/{id}/Card: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await VisualisationRegistryDatasourceCommandExecutionService.CreateAsync(dbContext, user,
                    log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, token);

                return TypedResults.Ok(await service.CardAsync(id, request, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {CardBase}/{id}/Card: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {CardBase}/{id}/Card: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {CardBase}/{id}/Card: 404 user={user}");
                }

                return TypedResults.NotFound();
            }
            catch (InvalidParametersException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {CardBase}/{id}/Card: 400 user={user} message={ex.Message}");
                }

                return TypedResults.BadRequest(new { code = ex.Code, message = ex.Message });
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {CardBase}/{id}/Card: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {CardBase}/{id}/Card: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}