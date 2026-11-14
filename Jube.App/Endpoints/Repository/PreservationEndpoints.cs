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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Dto.Repository.Preservation;
using Jube.Service.Exceptions.Repository.Preservation;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.Preservation;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class PreservationEndpoints
    {
        private const string Base = "/api/Preservation";
        private const long EnvelopeAllowance = 1_048_576;

        public static void MapPreservationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var environment = endpoints.ServiceProvider.GetService<DynamicEnvironment.DynamicEnvironment>();

            var packageLimit = (environment == null
                ? PreservationLimits.Default
                : PreservationLimits.FromEnvironment(environment)).MaximumPackageBytes / 3 * 4 + EnvelopeAllowance;

            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("Preservation");

            group.MapPost("Import", ImportAsync)
                .ValidateAntiforgeryForCookieSessions()
                .WithMetadata(new RequestSizeLimitAttribute(packageLimit))
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.RequestEntityTooLarge)
                .WithName("PreservationImport");

            group.MapPost("ImportPreview", ImportPreviewAsync)
                .ValidateAntiforgeryForCookieSessions()
                .WithMetadata(new RequestSizeLimitAttribute(packageLimit))
                .Produces<PreservationImportPreviewDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationImportPreview");

            group.MapGet("Summary", SummaryAsync)
                .Produces<PreservationSummaryDto>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationSummary");

            group.MapPost("Export", ExportAsync)
                .Produces<FileResult>(contentType: "application/octet-stream")
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationExport");
        }

        private static async Task<IResult> ImportAsync(
            [FromBody] PreservationImportRequestDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/Import: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            var streams = new List<Stream>();

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                var dtos = new List<PreservationImportFileDto>();

                if (model.Package is { Length: > 0 })
                {
                    var stream = new MemoryStream(model.Package, false);
                    streams.Add(stream);
                    dtos.Add(new PreservationImportFileDto { FileName = model.FileName ?? string.Empty, Content = stream });
                }

                await service.ImportAsync(dtos, model.Password, token);

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
            catch (NoFileException)
            {
                return TypedResults.BadRequest();
            }
            catch (InvalidPreservationFileException)
            {
                return TypedResults.BadRequest();
            }
            catch (PackageTooLargeException)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.RequestEntityTooLarge);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/Import: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/Import: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
            finally
            {
                foreach (var stream in streams)
                {
                    await stream.DisposeAsync();
                }
            }
        }

        private static async Task<IResult> ImportPreviewAsync(
            [FromBody] PreservationImportRequestDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/ImportPreview: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            var streams = new List<Stream>();

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                var dtos = new List<PreservationImportFileDto>();

                if (model.Package is { Length: > 0 })
                {
                    var stream = new MemoryStream(model.Package, false);
                    streams.Add(stream);
                    dtos.Add(new PreservationImportFileDto { FileName = model.FileName ?? string.Empty, Content = stream });
                }

                return TypedResults.Ok(await service.ImportPreviewAsync(dtos, model.Password, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/ImportPreview: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/ImportPreview: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NoFileException)
            {
                return TypedResults.BadRequest();
            }
            catch (InvalidPreservationFileException)
            {
                return TypedResults.BadRequest();
            }
            catch (PackageTooLargeException)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.RequestEntityTooLarge);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/ImportPreview: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/ImportPreview: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
            finally
            {
                foreach (var stream in streams)
                {
                    await stream.DisposeAsync();
                }
            }
        }

        private static async Task<IResult> SummaryAsync(
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
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

                return TypedResults.Ok(await service.SummaryAsync(token));
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
                log.Error($"GET {Base}/Summary: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> ExportAsync(
            [FromBody] ImportExportOptionsDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;

            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/Export: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);

                var export = await service.ExportAsync(model, token);

                return TypedResults.File(export.EncryptedBytes, "application/octet-stream", export.FileName);
            }
            catch (WeakPasswordException)
            {
                return TypedResults.BadRequest();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Export: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Export: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/Export: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/Export: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<PreservationService> CreateServiceAsync(DbContext dbContext,
            string user, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var service = await PreservationService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                serviceChangeBus, token);

            return service.WithLimits(PreservationLimits.FromEnvironment(dynamicEnvironment));
        }
    }
}