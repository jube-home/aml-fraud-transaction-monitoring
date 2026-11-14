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
using FluentValidation.Results;
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Dto.Repository.CaseResearch;
using Jube.Service.Exceptions.Repository.CaseResearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseResearch;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseResearchEndpoints
    {
        private const string Base = "/api/CaseResearch";
        private const long MaxJsonBodyBytes = 1024 * 1024;

        public static void MapCaseResearchEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseResearch");

            group.MapPost("", CreateAsync)
                .ValidateAntiforgeryForCookieSessions()
                .WithMetadata(new RequestSizeLimitAttribute(MaxJsonBodyBytes))
                .Produces<CaseResearchDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseResearchCreate");

            group.MapGet("ByCaseKeyValue", GetByCaseKeyValueAsync)
                .Produces<List<CaseResearchDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseResearchListByCaseKeyValue");
        }

        private static async Task<IResult> CreateAsync([FromBody] CaseResearchDto model, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseResearchService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);

                return TypedResults.Ok(await service.CreateAsync(model, token));
            }
            catch (Exception e) when (e is NotAuthenticatedException or ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                return TypedResults.BadRequest(ex.Result);
            }
            catch (CaseLockedException ex)
            {
                return TypedResults.Conflict(new { code = ex.Code, message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
            catch (ArgumentNullException)
            {
                return TypedResults.BadRequest();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByCaseKeyValueAsync(string key, string value, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CaseResearchService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);

                return TypedResults.Ok(await service.GetByCaseKeyValueAsync(key, value, token));
            }
            catch (Exception e) when (e is NotAuthenticatedException or ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByCaseKeyValue: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
