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
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class PreservationEndpoints
    {
        private const string Base = "/api/Preservation";

        public static void MapPreservationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("Preservation");

            group.MapPost("Import", ImportAsync)
                .DisableAntiforgery()
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationImport");

            group.MapGet("ExportPeek", ExportPeekAsync)
                .Produces<string>(contentType: "text/plain")
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationExportPeek");

            group.MapPost("Export", ExportAsync)
                .Produces<FileResult>(contentType: "application/octet-stream")
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("PreservationExport");
        }

        private static async Task<IResult> ImportAsync(
            IFormFileCollection files, [FromForm] string password, HttpContext httpContext, ILog log,
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
                foreach (var file in files)
                {
                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    dtos.Add(new PreservationImportFileDto { FileName = file.FileName, Content = stream });
                }

                await service.ImportAsync(dtos, password, token);
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

        private static async Task<IResult> ExportPeekAsync(
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token, bool exhaustive = false, bool suppressions = false, bool lists = false,
            bool dictionaries = false, bool visualisations = false)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ExportPeek: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CreateServiceAsync(dbContext, user, log, dynamicEnvironment,
                    stringLocalizerFactory, serviceChangeBus, token);
                var yaml = await service.ExportPeekAsync(exhaustive, suppressions, lists, dictionaries,
                    visualisations, token);
                return TypedResults.Text(yaml, "text/plain");
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ExportPeek: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ExportPeek: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/ExportPeek: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ExportPeek: 500 user={user}", e);
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

        private static Task<PreservationService> CreateServiceAsync(DbContext dbContext,
            string user, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return PreservationService.CreateAsync(dbContext, user, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment.AppSettings("PreservationSalt"),
                dynamicEnvironment.AppSettings("JempFileLegacyEncryptionFallback")
                    .Equals("True", StringComparison.CurrentCultureIgnoreCase), token);
        }
    }
}