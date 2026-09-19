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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Dto.Repository.CaseFile;
using Jube.Service.Repository.CaseFile;
using Jube.Service.Exceptions.Repository.CaseFile;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class CaseFileEndpoints
    {
        private const string Base = "/api/CaseFile";

        private static readonly HashSet<string> inlineSafeMediaTypes =
        [
            "application/pdf", "image/png", "image/jpeg", "image/gif", "image/webp", "text/plain"
        ];

        public static void MapCaseFileEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("CaseFile");

            group.MapPost("Upload", UploadAsync)
                .DisableAntiforgery()
                .Produces<CaseFileDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseFileUpload");

            group.MapPost("Remove", RemoveAsync)
                .DisableAntiforgery()
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseFileRemove");

            group.MapGet("", GenerateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseFileGenerate");

            group.MapGet("ByCaseKeyValue", GetByCaseKeyValueAsync)
                .Produces<List<CaseFileDto>>()
                .Produces((int)HttpStatusCode.Forbidden)
                .WithName("CaseFileListByCaseKeyValue");
        }

        private static async Task<IResult> UploadAsync(
            IFormFileCollection files, [FromForm] string caseKey, [FromForm] string caseKeyValue,
            [FromForm] int caseId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/Upload: entry caseId={caseId} user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseFileService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);
                var uploaded = files.Select(file =>
                    new UploadedFileContent(file.OpenReadStream(), file.FileName, file.ContentType, file.Length));
                var result = await service.UploadAsync(uploaded, caseKey, caseKeyValue, caseId, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Upload: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Upload: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotVisibleException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Upload: 403 (case not visible) caseId={caseId} user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Upload: 400 user={user} errors={ex.Result.Errors.Count}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/Upload: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/Upload: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> RemoveAsync(
            [FromForm] int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/Remove: entry id={id} user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseFileService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);
                await service.RemoveAsync(id, token);
                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Remove: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Remove: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotVisibleException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/Remove: 403 (not visible) id={id} user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/Remove: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/Remove: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GenerateAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}: entry id={id} user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseFileService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);
                var content = await service.GenerateAsync(id, token);
                var mediaType = (content.ContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
                httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
                httpContext.Response.Headers["Content-Security-Policy"] = "sandbox; default-src 'none'";
                return inlineSafeMediaTypes.Contains(mediaType)
                    ? TypedResults.File(content.Content, mediaType)
                    : TypedResults.File(content.Content, "application/octet-stream",
                        string.IsNullOrWhiteSpace(content.Name) ? "download" : content.Name);
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
            catch (NotVisibleException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}: 403 (not visible) id={id} user={user}");
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

        private static async Task<IResult> GetByCaseKeyValueAsync(
            string key, string value, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ByCaseKeyValue: entry key={key} user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await CaseFileService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);
                var result = await service.GetByCaseKeyValueAsync(key, value, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCaseKeyValue: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByCaseKeyValue: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/ByCaseKeyValue: client cancelled user={user}");
                }

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