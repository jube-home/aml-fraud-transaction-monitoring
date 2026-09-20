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
using Jube.Service.Exceptions.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Service.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Jube.App.Endpoints.Query
{
    public static class VisualisationRegistryDatasourceCommandExecutionEndpoints
    {
        private const string Base = "/api/GetByVisualisationRegistryDatasourceCommandExecutionQuery";

        private static readonly JsonSerializerSettings serializerSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        public static void MapVisualisationRegistryDatasourceCommandExecutionEndpoints(
            this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("VisualisationRegistryDatasourceCommandExecution");

            group.MapPost("{id:int}", ExecuteAsync)
                .WithName("VisualisationRegistryDatasourceCommandExecutionExecute");
        }

        private static async Task<IResult> ExecuteAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}/{id}: entry user={user}");
            }

            JArray parameters;
            try
            {
                using var reader = new StreamReader(httpContext.Request.Body);
                var body = await reader.ReadToEndAsync(token);
                if (string.IsNullOrWhiteSpace(body))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"POST {Base}/{id}: 400 (empty parameters) user={user}");
                    }

                    return TypedResults.BadRequest();
                }

                parameters = JArray.Parse(body);
            }
            catch (JsonException)
            {
                return TypedResults.BadRequest();
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await VisualisationRegistryDatasourceCommandExecutionService.CreateAsync(dbContext, user,
                    log,
                    stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, token);
                var result = await service.ExecuteAsync(id, parameters, token);
                return TypedResults.Content(JsonConvert.SerializeObject(result, serializerSettings),
                    "application/json");
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (Exception e) when (e is FormatException or OverflowException or InvalidCastException
                                          or ArgumentException or NullReferenceException or KeyNotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}/{id}: 400 (unreadable parameters) user={user}", e);
                }

                return TypedResults.BadRequest();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}/{id}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/{id}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}