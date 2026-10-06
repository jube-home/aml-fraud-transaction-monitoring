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
using Jube.Data.Context;
using Jube.Service.Exceptions;
using Jube.Service.Query.VersionHistory;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints.VersionHistory
{
    public static class VersionHistoryEndpointMapper
    {
        public static void MapVersionHistoryGroup<TService, TVersion, TDto>(this IEndpointRouteBuilder endpoints,
            string name)
            where TService : class, IVersionHistoryService<TService, TVersion>
            where TVersion : class
        {
            var fields = VersionHistoryProjection.ExposedFields(typeof(TDto), typeof(TVersion));

            var group = endpoints.MapGroup($"/api/{name}")
                .RequireAuthorization()
                .WithTags(name);

            group.MapGet("ById/{id:int}", (HttpContext httpContext, int id, ILog log,
                        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                        IStringLocalizerFactory localizers, CancellationToken token) =>
                    RunAsync<TService, TVersion>(httpContext, log, dynamicEnvironment, localizers, async service =>
                    {
                        var row = await service.GetByIdAsync(id, token).ConfigureAwait(false);
                        return row is null
                            ? TypedResults.NotFound()
                            : TypedResults.Ok(VersionHistoryProjection.Project(row, fields));
                    }, token))
                .WithName($"{name}ById");

            group.MapGet("ByParent/{parentId:int}", (HttpContext httpContext, int parentId, ILog log,
                        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                        IStringLocalizerFactory localizers, CancellationToken token) =>
                    RunAsync<TService, TVersion>(httpContext, log, dynamicEnvironment, localizers, async service =>
                    {
                        var rows = await service.GetByParentIdAsync(parentId, token).ConfigureAwait(false);
                        return TypedResults.Ok(ProjectAll(rows, fields));
                    }, token))
                .WithName($"{name}ByParent");

            group.MapGet("ByDateRange/{parentId:int}", (HttpContext httpContext, int parentId, DateTime from,
                        DateTime to, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                        IStringLocalizerFactory localizers, CancellationToken token) =>
                    RunAsync<TService, TVersion>(httpContext, log, dynamicEnvironment, localizers, async service =>
                    {
                        var rows = await service.GetByDateRangeAsync(parentId, from, to, token).ConfigureAwait(false);
                        return TypedResults.Ok(ProjectAll(rows, fields));
                    }, token))
                .WithName($"{name}ByDateRange");

            group.MapGet("ById/{id:int}/ByDateRange", (HttpContext httpContext, int id, DateTime from,
                        DateTime to, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                        IStringLocalizerFactory localizers, CancellationToken token) =>
                    RunAsync<TService, TVersion>(httpContext, log, dynamicEnvironment, localizers, async service =>
                    {
                        var row = await service.GetByIdAndDateRangeAsync(id, from, to, token).ConfigureAwait(false);
                        return row is null
                            ? TypedResults.NotFound()
                            : TypedResults.Ok(VersionHistoryProjection.Project(row, fields));
                    }, token))
                .WithName($"{name}ByIdAndDateRange");

            group.MapGet("Latest/{parentId:int}", (HttpContext httpContext, int parentId, ILog log,
                        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                        IStringLocalizerFactory localizers, CancellationToken token) =>
                    RunAsync<TService, TVersion>(httpContext, log, dynamicEnvironment, localizers, async service =>
                    {
                        var row = await service.GetLatestAsync(parentId, token).ConfigureAwait(false);
                        return row is null
                            ? TypedResults.NotFound()
                            : TypedResults.Ok(VersionHistoryProjection.Project(row, fields));
                    }, token))
                .WithName($"{name}Latest");

            group.MapGet("Compare", (HttpContext httpContext, int fromId, int toId, ILog log,
                        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                        IStringLocalizerFactory localizers, CancellationToken token) =>
                    RunAsync<TService, TVersion>(httpContext, log, dynamicEnvironment, localizers, async service =>
                    {
                        var changes = await service.CompareAsync(fromId, toId, token).ConfigureAwait(false);
                        return TypedResults.Ok(VersionHistoryProjection.Changes(changes, fields));
                    }, token))
                .WithName($"{name}Compare");
        }

        private static List<Dictionary<string, object>> ProjectAll<TVersion>(IEnumerable<TVersion> rows,
            IReadOnlySet<string> fields)
            where TVersion : class
        {
            var projected = new List<Dictionary<string, object>>();

            foreach (var row in rows)
            {
                projected.Add(VersionHistoryProjection.Project(row, fields));
            }

            return projected;
        }

        private static async Task<IResult> RunAsync<TService, TVersion>(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory localizers,
            Func<TService, Task<IResult>> action, CancellationToken token)
            where TService : IVersionHistoryService<TService, TVersion>
            where TVersion : class
        {
            var user = httpContext.User.Identity?.Name;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await TService.CreateAsync(dbContext, user, log, localizers, token)
                    .ConfigureAwait(false);
                return await action(service).ConfigureAwait(false);
            }
            catch (ServiceException ex) when (ex.Code is "NotAuthenticated" or "PermissionDenied")
            {
                log.Warn($"{httpContext.Request.Path}: 403 ({ex.Code}) user={user}");
                return TypedResults.Forbid();
            }
            catch (ServiceException ex)
            {
                return TypedResults.BadRequest(new { message = ex.Message });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"{httpContext.Request.Path}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}