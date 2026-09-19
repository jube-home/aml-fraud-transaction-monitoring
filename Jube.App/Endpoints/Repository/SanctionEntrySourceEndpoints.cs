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
using Jube.Data.Context;
using Jube.Dto.Repository.SanctionEntrySource;
using Jube.Service.Exceptions.Repository.SanctionEntrySource;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.SanctionEntrySource;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class SanctionEntrySourceEndpoints
    {
        private const string Base = "/api/SanctionEntrySource";

        public static void MapSanctionEntrySourceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("SanctionEntrySource");

            group.MapGet("", GetAsync)
                .Produces<List<SanctionEntrySourceDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("SanctionEntrySourceList");

            group.MapPost("Import", ImportAsync)
                .DisableAntiforgery()
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("SanctionEntrySourceImport");
        }

        private static async Task<IResult> GetAsync(
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await SanctionEntrySourceService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.GetAsync(token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> ImportAsync(
            IFormFile files, [FromForm] int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/Import: entry sanctionEntrySourceId={id} user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await SanctionEntrySourceService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);

                await using var stream = files?.OpenReadStream();
                await service.ImportAsync(stream, files?.Length ?? 0, id, token);

                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Import: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Import: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Import: 400 sanctionEntrySourceId={id} user={user} " +
                             $"errors={ex.Result.Errors.Count}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/Import: client cancelled sanctionEntrySourceId={id} user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/Import: 500 sanctionEntrySourceId={id} user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}