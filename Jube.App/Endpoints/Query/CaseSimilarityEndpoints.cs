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
using Jube.Dto.Query.CaseSimilarity;
using Jube.Service.Exceptions.Query.CaseSimilarity;
using Jube.Service.Query.CaseSimilarity;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Query
{
    public static class CaseSimilarityEndpoints
    {
        private const string Base = "/api/CaseSimilarity";

        public static void MapCaseSimilarityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseSimilarity");

            group.MapGet("{caseId:int}", GetAsync)
                .Produces<CaseSimilarityDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("CaseSimilarityGet");
        }

        private static async Task<IResult> GetAsync(int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseSimilarityService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);

                var similarity = await service.GetAsync(caseId, token);

                similarity.Enabled = CaseSimilarityHash.KeyOf(dynamicEnvironment) is not null;

                return TypedResults.Ok(similarity);
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/{caseId}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/{caseId}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
