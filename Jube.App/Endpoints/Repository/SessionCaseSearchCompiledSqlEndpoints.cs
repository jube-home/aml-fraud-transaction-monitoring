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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Query.DynamicResultsSchema;
using Jube.Dto.Repository.SessionCaseSearchCompiledSql;
using Jube.Service.Exceptions.Repository.SessionCaseSearchCompiledSql;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.SessionCaseSearchCompiledSql;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Jube.App.Endpoints.Repository
{
    public static class SessionCaseSearchCompiledSqlEndpoints
    {
        private const string Base = "/api/SessionCaseSearchCompiledSql";

        public static void MapSessionCaseSearchCompiledSqlEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("SessionCaseSearchCompiledSql");

            group.MapGet("ByGuid/{guid:Guid}", ExecuteByGuidAsync)
                .Produces<DynamicResultSchemaDto>()
                .Produces((int)HttpStatusCode.NotFound)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("SessionCaseSearchCompiledSqlExecuteByGuid");

            group.MapGet("ByLast", GetLastAsync)
                .Produces<SessionCaseSearchCompiledSqlDto>()
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("SessionCaseSearchCompiledSqlGetLast");

            group.MapPost("", InsertAsync)
                .Produces<SessionCaseSearchCompiledSqlDto>()
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.Forbidden)
                .Produces((int)HttpStatusCode.InternalServerError)
                .WithName("SessionCaseSearchCompiledSqlCreate");
        }

        private static async Task<IResult> ExecuteByGuidAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ByGuid/{guid}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await SessionCaseSearchCompiledSqlService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, token);
                return TypedResults.Ok(await service.ExecuteByGuidAsync(guid, token));
            }
            catch (Npgsql.PostgresException e) when (e.SqlState is "22025" or "22P02" or "22007" or "22008" or "22003")
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByGuid/{guid}: 400 (unusable filter value) user={user}", e);
                }

                return TypedResults.BadRequest();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByGuid/{guid}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByGuid/{guid}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByGuid/{guid}: 404 user={user}");
                }

                return TypedResults.NotFound();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/ByGuid/{guid}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByGuid/{guid}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetLastAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/ByLast: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await SessionCaseSearchCompiledSqlService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, token);
                return TypedResults.Ok(await service.GetLastAsync(token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByLast: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/ByLast: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/ByLast: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByLast: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static bool ContainsNul(string value)
        {
            return value != null &&
                   (value.Contains('\0') || value.Contains("\\u0000", StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<IResult> InsertAsync([FromBody] SessionCaseSearchCompiledSqlDto model,
            HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"POST {Base}: entry user={user}");
            }

            if (ContainsNul(model?.SelectJson) || ContainsNul(model?.FilterJson))
            {
                return TypedResults.BadRequest();
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await SessionCaseSearchCompiledSqlService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, token);
                return TypedResults.Ok(await service.InsertAsync(model, token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 400 user={user} errors={ex.Result.Errors.Count}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
            catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException
                                          or OverflowException or NullReferenceException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"POST {Base}: 400 (rule could not be compiled) user={user}", e);
                }

                return TypedResults.BadRequest();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"POST {Base}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}