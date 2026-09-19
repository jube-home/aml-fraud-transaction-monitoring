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
using Jube.Dto.Repository.UserInTenant;
using Jube.Service.Exceptions.Repository.UserInTenant;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.UserInTenant;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.Repository
{
    public static class UserInTenantEndpoints
    {
        private const string Base = "/api/UserInTenant";

        public static void MapUserInTenantEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("UserInTenant");

            group.MapGet("", GetAsync)
                .Produces<List<UserInTenantDto>>()
                .WithName("UserInTenantGet");

            group.MapGet("GetCurrentTenantRegistry", GetCurrentTenantRegistryAsync)
                .Produces<UserInTenantDto>()
                .WithName("UserInTenantGetCurrentTenantRegistry");

            group.MapPut("", UpdateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("UserInTenantUpdate");
        }

        private static async Task<IResult> GetAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
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
                var service = await UserInTenantService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);
                return TypedResults.Ok(await service.GetAsync(token));
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

        private static async Task<IResult> GetCurrentTenantRegistryAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"GET {Base}/GetCurrentTenantRegistry: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await UserInTenantService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);
                return TypedResults.Ok(await service.GetCurrentTenantRegistryAsync(token));
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/GetCurrentTenantRegistry: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GET {Base}/GetCurrentTenantRegistry: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"GET {Base}/GetCurrentTenantRegistry: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/GetCurrentTenantRegistry: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> UpdateAsync(int tenantRegistryId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
            {
                log.Debug($"PUT {Base}: entry tenantRegistryId={tenantRegistryId} user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await UserInTenantService.CreateAsync(dbContext, user, log, stringLocalizerFactory,
                    serviceChangeBus, token);
                await service.UpdateAsync(tenantRegistryId, token);
                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}: 403 (not authenticated) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}: 403 user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}: 400 user={user} errors={ex.Result.Errors.Count}");
                }

                return TypedResults.BadRequest(ex.Result);
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PUT {Base}: 204 (not found) user={user}");
                }

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"PUT {Base}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"PUT {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}