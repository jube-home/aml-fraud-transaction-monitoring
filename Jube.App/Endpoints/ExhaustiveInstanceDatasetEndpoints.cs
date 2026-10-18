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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Dto.ExhaustiveInstanceDataset;
using Jube.Service.Exceptions.ExhaustiveInstanceDataset;
using Jube.Service.Exceptions.ExhaustiveSearchInstance;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.ExhaustiveInstanceDataset;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.Net.Http.Headers;

namespace Jube.App.Endpoints
{
    public static class ExhaustiveInstanceDatasetEndpoints
    {
        private const string CombinedBase = "/api/ExhaustiveInstanceDataset";
        private const long EnvelopeAllowanceBytes = 1024 * 1024;
        private const int MaxNameFieldBytes = 2048;
        private const int MaxBoundaryLength = 200;

        public static void MapExhaustiveInstanceDatasetEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGroup(CombinedBase)
                .RequireAuthorization()
                .WithTags("ExhaustiveInstanceDataset")
                .MapGet("", (HttpContext c, ILog l, DynamicEnvironment.DynamicEnvironment e,
                        IStringLocalizerFactory f, IServiceChangeBus b, CancellationToken t) =>
                    ListAsync(null, CombinedBase, c, l, e, f, b, t))
                .Produces<List<ExhaustiveInstanceDatasetDto>>()
                .WithName("ExhaustiveInstanceDatasetGetAll");

            MapKind(endpoints, DatasetKind.Csv);
            MapKind(endpoints, DatasetKind.Json);
        }

        private static void MapKind(IEndpointRouteBuilder endpoints, DatasetKind kind)
        {
            var name = kind.ToString();
            var baseRoute = "/api/ExhaustiveInstance" + name;

            var group = endpoints.MapGroup(baseRoute)
                .RequireAuthorization()
                .WithTags("ExhaustiveInstance" + name);

            group.MapGet("", (HttpContext c, ILog l, DynamicEnvironment.DynamicEnvironment e,
                        IStringLocalizerFactory f, IServiceChangeBus b, CancellationToken t) =>
                    ListAsync(kind, baseRoute, c, l, e, f, b, t))
                .Produces<List<ExhaustiveInstanceDatasetDto>>()
                .WithName("ExhaustiveInstance" + name + "GetAll");

            group.MapGet("{id:int}", (int id, HttpContext c, ILog l, DynamicEnvironment.DynamicEnvironment e,
                        IStringLocalizerFactory f, IServiceChangeBus b, CancellationToken t) =>
                    RunAsync($"GET {baseRoute}/{id}", c, l, e, f, b, t,
                        async (service, token) => TypedResults.Ok(await service.GetByIdAsync(kind, id, token))))
                .Produces<ExhaustiveInstanceDatasetDto>()
                .WithName("ExhaustiveInstance" + name + "GetById");

            group.MapPost("Upload", (HttpContext c, ILog l, DynamicEnvironment.DynamicEnvironment e,
                        IStringLocalizerFactory f, IServiceChangeBus b, CancellationToken t) =>
                    UploadAsync(kind, baseRoute, c, l, e, f, b, t))
                .ValidateAntiforgeryForCookieSessions()
                .Produces<ExhaustiveInstanceDatasetDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.RequestEntityTooLarge)
                .Produces((int)HttpStatusCode.UnsupportedMediaType)
                .WithName("ExhaustiveInstance" + name + "Upload");

            group.MapPut("Confirm", ([FromBody] ExhaustiveInstanceDatasetMappingDto model,
                        HttpContext c, ILog l,
                        DynamicEnvironment.DynamicEnvironment e, IStringLocalizerFactory f, IServiceChangeBus b,
                        CancellationToken t) =>
                    RunAsync($"PUT {baseRoute}/Confirm", c, l, e, f, b, t,
                        async (service, token) => TypedResults.Ok(await service.ConfirmAsync(kind, model, token))))
                .Produces<ExhaustiveInstanceDatasetDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("ExhaustiveInstance" + name + "Confirm");

            group.MapDelete("{id:int}", (int id, HttpContext c, ILog l, DynamicEnvironment.DynamicEnvironment e,
                        IStringLocalizerFactory f, IServiceChangeBus b, CancellationToken t) =>
                    RunAsync($"DELETE {baseRoute}/{id}", c, l, e, f, b, t, async (service, token) =>
                    {
                        await service.DeleteAsync(kind, id, token);

                        return TypedResults.Ok();
                    }))
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Conflict)
                .WithName("ExhaustiveInstance" + name + "Delete");
        }

        private static Task<IResult> ListAsync(DatasetKind? kind, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync($"GET {route}", httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, token, async (service, ct) => TypedResults.Ok(await service.GetAsync(kind, ct)));
        }

        private static async Task<IResult> RunAsync(string description, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token,
            Func<ExhaustiveInstanceDatasetService, CancellationToken, Task<IResult>> action)
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await ExhaustiveInstanceDatasetService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus,
                    DatasetLimitsFactory.FromEnvironment(dynamicEnvironment), token);

                return await action(service, token);
            }
            catch (Exception e) when (e is NotAuthenticatedException or ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{description}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DatasetRejectedException ex)
            {
                var status = ex.Report.Errors.Exists(e => e.Code == "TooLarge")
                    ? (int)HttpStatusCode.RequestEntityTooLarge
                    : (int)HttpStatusCode.BadRequest;

                return TypedResults.Json(
                    new { code = ex.Code, errors = ex.Report.Errors, warnings = ex.Report.Warnings },
                    statusCode: status);
            }
            catch (UploadRejectedException ex)
            {
                return TypedResults.BadRequest(new { code = ex.Code, message = ex.Message });
            }
            catch (DtoValidationException ex)
            {
                return TypedResults.BadRequest(ex.Result);
            }
            catch (NotFoundException)
            {
                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (DatasetInUseException ex)
            {
                return TypedResults.Conflict(new { code = ex.Code, message = ex.Message });
            }
            catch (BadHttpRequestException ex)
            {
                return TypedResults.StatusCode(ex.StatusCode);
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException)
            {
                return TypedResults.BadRequest(new
                    { code = "UploadRejected", message = "The request body is malformed." });
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{description}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"{description}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static Task<IResult> UploadAsync(DatasetKind kind, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var request = httpContext.Request;
            var limits = DatasetLimitsFactory.FromEnvironment(dynamicEnvironment);
            var description = $"POST {route}/Upload";

            if (request.Headers.ContentEncoding.Count > 0
                && !(request.Headers.ContentEncoding.Count == 1
                     && string.Equals(request.Headers.ContentEncoding[0], "identity",
                         StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult<IResult>(TypedResults.BadRequest(new
                {
                    code = "UploadRejected", message = "A Content-Encoding on the request body is not supported."
                }));
            }

            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType))
            {
                return Task.FromResult<IResult>(TypedResults.StatusCode((int)HttpStatusCode.UnsupportedMediaType));
            }

            var isMultipart = mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase);

            var isRawJson = kind == DatasetKind.Json
                            && mediaType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase);

            if (!isMultipart && !isRawJson)
            {
                return Task.FromResult<IResult>(TypedResults.StatusCode((int)HttpStatusCode.UnsupportedMediaType));
            }

            var bodyLimit = isRawJson ? limits.MaxBytes : limits.MaxBytes + EnvelopeAllowanceBytes;

            if (request.ContentLength > bodyLimit)
            {
                return Task.FromResult<IResult>(TypedResults.StatusCode((int)HttpStatusCode.RequestEntityTooLarge));
            }

            if (httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } sizeFeature)
            {
                sizeFeature.MaxRequestBodySize = bodyLimit;
            }

            if (isRawJson)
            {
                var name = request.Query["name"].ToString();
                var label = request.Query["sourceLabel"].ToString();

                return RunAsync(description, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                    serviceChangeBus, token, async (service, ct) => TypedResults.Ok(
                        await service.UploadJsonAsync(name, label, request.ContentType, request.Body, ct)));
            }

            var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;

            if (string.IsNullOrEmpty(boundary) || boundary.Length > MaxBoundaryLength)
            {
                return Task.FromResult<IResult>(TypedResults.BadRequest(new
                {
                    code = "UploadRejected", message = "Invalid multipart boundary."
                }));
            }

            return RunAsync(description, httpContext, log, dynamicEnvironment, stringLocalizerFactory,
                serviceChangeBus, token, async (service, ct) =>
                {
                    var reader = new MultipartReader(boundary, request.Body)
                    {
                        BodyLengthLimit = bodyLimit, HeadersLengthLimit = 16 * 1024
                    };

                    string name = null;

                    while (await reader.ReadNextSectionAsync(ct) is { } section)
                    {
                        if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition)
                            || !disposition.DispositionType.Equals("form-data"))
                        {
                            continue;
                        }

                        var fieldName = HeaderUtilities.RemoveQuotes(disposition.Name).Value;

                        var fileName = HeaderUtilities.RemoveQuotes(disposition.FileName).Value
                                       ?? HeaderUtilities.RemoveQuotes(disposition.FileNameStar).Value;

                        if (fileName == null)
                        {
                            if (fieldName == "name")
                            {
                                name = await ReadSmallTextAsync(section.Body, ct);
                            }

                            continue;
                        }

                        var result = kind == DatasetKind.Csv
                            ? await service.UploadCsvAsync(name, fileName, section.ContentType, section.Body, ct)
                            : await service.UploadJsonFileAsync(name, fileName, section.ContentType, section.Body, ct);

                        return TypedResults.Ok(result);
                    }

                    return TypedResults.BadRequest(new
                    {
                        code = "UploadRejected",
                        message = kind == DatasetKind.Csv ? "A CSV file is required." : "A JSON file is required."
                    });
                });
        }

        private static async Task<string> ReadSmallTextAsync(Stream body, CancellationToken token)
        {
            var buffer = new byte[MaxNameFieldBytes + 1];
            var total = 0;
            int read;

            while (total < buffer.Length && (read = await body.ReadAsync(buffer.AsMemory(total), token)) > 0)
            {
                total += read;
            }

            return total > MaxNameFieldBytes
                ? new string('x', MaxNameFieldBytes + 1)
                : Encoding.UTF8.GetString(buffer, 0, total);
        }
    }
}