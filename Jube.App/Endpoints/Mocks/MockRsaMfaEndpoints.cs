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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jube.App.Dto.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jube.App.Endpoints.Mocks
{
    public static class MockRsaMfaEndpoints
    {
        private static readonly JsonSerializerSettings apiSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
        };

        public static void MapMockRsaMfaEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/Mfa").WithTags("Mfa").AllowAnonymous()
                .WithMetadata(new RequestSizeLimitAttribute(64 * 1024));

            group.MapPost("", PostAsync)
                .Produces<object>()
                .WithName("MockRsaMfaPost");
        }

        private static async Task<IResult> PostAsync(HttpRequest request)
        {
            if (!AcceptedByJsonFormatter(request.ContentType))
            {
                return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            }

            var requestDto = await BindAsync(request);

            if (!request.Headers.TryGetValue("client-key", out var clientKeyValues))
            {
                return Json(StatusCodes.Status401Unauthorized, new { error = "Missing client-key header." });
            }

            if (clientKeyValues.Count != 1)
            {
                return Json(StatusCodes.Status401Unauthorized, new
                {
                    error = $"Expected exactly one client-key value, got {clientKeyValues.Count}."
                });
            }

            if (clientKeyValues[0] != "SomethingSecretForTheClientKeyHeader")
            {
                return Json(StatusCodes.Status401Unauthorized, new { error = "Invalid client-key." });
            }

            if (String.IsNullOrEmpty(request.ContentType) ||
                !request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return Json(StatusCodes.Status415UnsupportedMediaType, new
                {
                    error = $"Expected application/json, got '{request.ContentType}'."
                });
            }

            var otp = requestDto?.SubjectCredentials.FirstOrDefault()
                ?.CollectedInputs
                .FirstOrDefault()
                ?.Value;

            var success = otp == "12345678";

            var response = new
            {
                context = new
                {
                    authnAttemptId = Guid.NewGuid().ToString(),
                    messageId = Guid.NewGuid().ToString(),
                    inResponseTo = "test5213021196242"
                },
                credentialValidationResults = new[]
                {
                    new
                    {
                        methodId = "SECURID",
                        methodResponseCode = "SUCCESS",
                        methodReasonCode = (string)null,
                        authnAttributes = Array.Empty<object>()
                    }
                },
                attemptResponseCode = success ? "SUCCESS" : "NOPE",
                attemptReasonCode = "CREDENTIAL_VERIFIED",
                challengeMethods = new
                {
                    challenges = new[]
                    {
                        new
                        {
                            methodSetId = (string)null,
                            requiredMethods = Array.Empty<object>()
                        }
                    }
                }
            };

            return Json(StatusCodes.Status200OK, response);
        }

        private static IResult Json(int statusCode, object value)
        {
            return Results.Content(JsonConvert.SerializeObject(value, apiSettings),
                "application/json; charset=utf-8", null, statusCode);
        }

        private static bool AcceptedByJsonFormatter(string contentType)
        {
            if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            {
                return false;
            }

            return mediaType.MatchesMediaType("application/json") ||
                   mediaType.MatchesMediaType("text/json") ||
                   mediaType.Type.Equals("application", StringComparison.OrdinalIgnoreCase) &&
                   mediaType.SubType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<MockRsaMfaRequestDto> BindAsync(HttpRequest request)
        {
            var encoding = MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType) &&
                           mediaType.Encoding != null
                ? mediaType.Encoding
                : Encoding.UTF8;

            using var reader = new StreamReader(request.Body, encoding, true, 1024, true);
            var text = await reader.ReadToEndAsync();

            var successful = true;
            var serializer = JsonSerializer.Create(apiSettings);
            serializer.Error += (_, args) =>
            {
                successful = false;
                args.ErrorContext.Handled = true;
            };

            try
            {
                await using var jsonReader = new JsonTextReader(new StringReader(text));
                jsonReader.MaxDepth = 32;
                var dto = serializer.Deserialize<MockRsaMfaRequestDto>(jsonReader);
                return successful ? dto : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}