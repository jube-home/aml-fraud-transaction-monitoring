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
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Dto.Invoke;
using Jube.Dto.Repository.Archive;
using Jube.Resources;
using Jube.Service.Exceptions.Invoke;
using Jube.Service.Invoke;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jube.App.Endpoints
{
    public static class InvokeEndpoints
    {
        private const string Base = "/api/Invoke";

        private static readonly JsonSerializerSettings fallbackSettings = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
            MaxDepth = 32
        };

        public static void MapInvokeEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("Invoke");

            group.MapGet("EntityAnalysisModel/Callback/{guid:guid}", CallbackAsync)
                .WithName("InvokeEntityAnalysisModelCallback");

            group.MapGet("Sanction", Sanction)
                .WithName("InvokeSanction");

            group.MapPut("Archive/Tag", (Func<HttpContext, Task<IResult>>)TagAsync)
                .WithName("InvokeArchiveTag");

            group.MapPost("EntityAnalysisModel/{guid}", (Func<HttpContext, Task<IResult>>)EntityAnalysisModelAsync)
                .WithName("InvokeEntityAnalysisModel");

            group.MapPost("EntityAnalysisModel/{guid}/{async}",
                    (Func<HttpContext, Task<IResult>>)EntityAnalysisModelAsync)
                .WithName("InvokeEntityAnalysisModelAsync");

            group.MapPost("ExhaustiveSearchInstance/{guid}",
                    (Func<HttpContext, Task<IResult>>)ExhaustiveSearchInstanceAsync)
                .WithName("InvokeExhaustiveSearchInstance");
        }

        private static InvokeService CreateService(HttpContext httpContext)
        {
            var services = httpContext.RequestServices;
            var dynamicEnvironment = services.GetRequiredService<global::Jube.DynamicEnvironment.DynamicEnvironment>();
            var log = services.GetRequiredService<ILog>();
            return new InvokeService(services.GetService<global::Jube.Engine.Engine>(),
                dynamicEnvironment,
                log,
                httpContext.User.Identity?.Name,
                services.GetRequiredService<IStringLocalizerFactory>().Create(typeof(InvokeResources)),
                () => DataConnectionDbContext.GetResilientDbContextDataConnection(
                    dynamicEnvironment.AppSettings("ConnectionString"), log));
        }

        private static JsonSerializerSettings SettingsFor(HttpContext httpContext)
        {
            return httpContext.RequestServices.GetService<IOptions<MvcNewtonsoftJsonOptions>>()?.Value
                .SerializerSettings ?? fallbackSettings;
        }

        private static IResult Write(HttpContext httpContext, InvokeResult result)
        {
            if (result.IsForbid)
            {
                return Results.Forbid();
            }

            if (result.Payload != null)
            {
                return Results.Bytes(result.Payload, result.ContentType);
            }

            if (result.HasJsonValue)
            {
                return Results.Text(JsonConvert.SerializeObject(result.JsonValue, SettingsFor(httpContext)),
                    "application/json; charset=utf-8", statusCode: result.StatusCode);
            }

            return Results.StatusCode(result.StatusCode);
        }

        private static async Task<IResult> CallbackAsync(HttpContext httpContext, Guid guid,
            CancellationToken token)
        {
            var timeout = ParseInt(First(httpContext.Request.Query["timeout"]));
            var result = await CreateService(httpContext).CallbackAsync(guid, timeout, token).ConfigureAwait(false);
            return Write(httpContext, result);
        }

        private static IResult Sanction(HttpContext httpContext)
        {
            var query = httpContext.Request.Query;
            var request = new SanctionSearchRequestDto
            {
                MultiPartString = FirstOrNull(First(query["multiPartString"])),
                Distance = FirstOrNull(First(query["distance"])),
                MaxDistanceRatio = FirstOrNull(First(query["maxDistanceRatio"])),
                MaxCoverageRatio = FirstOrNull(First(query["maxCoverageRatio"]))
            };

            try
            {
                return Write(httpContext, CreateService(httpContext).Sanction(request));
            }
            catch (DtoValidationException ex)
            {
                return TypedResults.BadRequest(ex.Result);
            }
        }

        private static async Task<IResult> TagAsync(HttpContext httpContext)
        {
            if (!IsJsonMediaType(httpContext.Request.ContentType))
            {
                return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            }

            var model = await ReadBodyAsModelAsync<ArchiveTagDto>(httpContext).ConfigureAwait(false);
            return Write(httpContext, CreateService(httpContext).Tag(model));
        }

        private static async Task<IResult> EntityAnalysisModelAsync(HttpContext httpContext)
        {
            var routeValues = httpContext.Request.RouteValues;
            var guid = routeValues["guid"]?.ToString();
            var async = routeValues.TryGetValue("async", out var value) ? value?.ToString() ?? string.Empty : null;

            var result = await CreateService(httpContext)
                .InvokeModelAsync(guid, async, () => ReadBodyAsync(httpContext),
                    httpContext.Request.ContentLength != null)
                .ConfigureAwait(false);
            return Write(httpContext, result);
        }

        private static async Task<IResult> ExhaustiveSearchInstanceAsync(HttpContext httpContext)
        {
            var guid = httpContext.Request.RouteValues["guid"]?.ToString();
            var result = await CreateService(httpContext)
                .ExhaustiveSearchInstanceAsync(guid, () => ReadBodyAsync(httpContext))
                .ConfigureAwait(false);
            return Write(httpContext, result);
        }

        private static async Task<MemoryStream> ReadBodyAsync(HttpContext httpContext)
        {
            var ms = new MemoryStream();
            try
            {
                await httpContext.Request.Body.CopyToAsync(ms).ConfigureAwait(false);
            }
            catch (BadHttpRequestException ex)
            {
                throw new ClientRequestException(ex.StatusCode, ex.Message);
            }

            return ms;
        }

        private static async Task<T> ReadBodyAsModelAsync<T>(HttpContext httpContext) where T : class
        {
            using var ms = new MemoryStream();
            await httpContext.Request.Body.CopyToAsync(ms).ConfigureAwait(false);

            try
            {
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(ms.ToArray()),
                    SettingsFor(httpContext));
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool IsJsonMediaType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }

            var mediaType = contentType.Split(';')[0].Trim();
            return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                   || mediaType.Equals("text/json", StringComparison.OrdinalIgnoreCase)
                   || (mediaType.StartsWith("application/", StringComparison.OrdinalIgnoreCase)
                       && mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase));
        }

        private static string First(StringValues values)
        {
            return values.Count == 0 ? string.Empty : values[0] ?? string.Empty;
        }

        private static string FirstOrNull(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static int? ParseInt(string value)
        {
            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }
    }
}