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
using Jube.Dto.Repository.Preservation;
using Jube.Dto.Repository.PreservationSnapshot;
using Jube.Service.Exceptions.Repository.Preservation;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.PreservationSnapshot;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using NotFoundException = Jube.Service.Exceptions.Repository.PreservationSnapshot.NotFoundException;

namespace Jube.App.Endpoints.Repository
{
    public static class PreservationSnapshotEndpoints
    {
        private const string Base = "/api/PreservationSnapshot";

        public static void MapPreservationSnapshotEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("PreservationSnapshot");

            group.MapGet("", GetAsync)
                .Produces<List<PreservationSnapshotDto>>()
                .WithName("PreservationSnapshotList");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<PreservationSnapshotDto>()
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("PreservationSnapshotGet");

            group.MapPost("", CreateAsync)
                .Produces<PreservationSnapshotDto>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationSnapshotCreate");

            group.MapPost("{id:int}/Import", ImportAsync)
                .Produces<PreservationSnapshotDto>()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationSnapshotImport");

            group.MapPost("Diff", DiffAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<PreservationDiffDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationSnapshotDiff");

            group.MapPost("{id:int}/ImportPreview", ImportPreviewAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces<PreservationImportPreviewDto>()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationSnapshotImportPreview");

            group.MapGet("{id:int}/Body", BodyAsync)
                .Produces<string>(contentType: "application/json")
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationSnapshotBody");

            group.MapDelete("{id:int}", DeleteAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("PreservationSnapshotDelete");
        }

        private static async Task<IResult> GetAsync(
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            int? limit, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                return TypedResults.Ok(await service.GetAsync(limit ?? 250, token));
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByIdAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                return TypedResults.Ok(await service.GetByIdAsync(id, token));
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/{id}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> CreateAsync(
            [FromBody] PreservationSnapshotRequestDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                return TypedResults.Ok(await service.CreateAsync(model, token));
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
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

        private static async Task<IResult> DiffAsync(
            [FromBody] PreservationDiffRequestDto model, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                return TypedResults.Ok(await service.DiffAsync(model, token));
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (ArgumentException)
            {
                return TypedResults.BadRequest();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/Diff: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> ImportPreviewAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                return TypedResults.Ok(await service.ImportPreviewAsync(id, token));
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (ArgumentException)
            {
                return TypedResults.BadRequest();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/{id}/ImportPreview: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> BodyAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                return TypedResults.File(System.Text.Encoding.UTF8.GetBytes(await service.GetBodyAsync(id, token)),
                    "application/json", $"snapshot-{id}.json");
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (ArgumentException)
            {
                return TypedResults.BadRequest();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/{id}/Body: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> ImportAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                return TypedResults.Ok(await service.ImportAsync(id, token));
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/{id}/Import: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> DeleteAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                await service.DeleteAsync(id, token);

                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"DELETE {Base}/{id}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static Task<PreservationSnapshotService> CreateServiceAsync(DbContext dbContext,
            string user, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return PreservationSnapshotService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                serviceChangeBus, token);
        }
    }
}