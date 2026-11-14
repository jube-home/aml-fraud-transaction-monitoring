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
using Jube.App.Code;
using Jube.Data.Context;
using Jube.Dto.Options;
using Jube.Data.Repository;
using Jube.Dto.UserNotification;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Jube.App.Endpoints
{
    public static class UserNotificationEndpoints
    {
        private const int PageSize = 50;

        public static void MapUserNotificationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/UserNotification")
                .RequireAuthorization()
                .WithTags("UserNotification");

            group.MapGet(string.Empty, ListAsync)
                .Produces<List<UserNotificationDto>>()
                .Produces((int)HttpStatusCode.Unauthorized)
                .WithName("UserNotificationList");

            group.MapPut("Read/{guid:guid}", MarkReadAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Unauthorized)
                .WithName("UserNotificationMarkRead");

            group.MapPut("ReadAll", MarkAllReadAsync)
                .ValidateAntiforgeryForCookieSessions()
                .Produces((int)HttpStatusCode.NoContent)
                .Produces((int)HttpStatusCode.Unauthorized)
                .WithName("UserNotificationMarkAllRead");
        }

        private static async Task<(string UserName, int TenantRegistryId)?> ResolveAsync(HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            var userName = httpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return null;
            }

            var permissions = await new Data.Security.PermissionValidation()
                .GetPermissionsAsync(dynamicEnvironment.AppSettings("ConnectionString"), userName, log)
                .ConfigureAwait(false);

            return (userName, permissions.TenantRegistryId);
        }

        private static async Task<IResult> ListAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            var caller = await ResolveAsync(httpContext, log, dynamicEnvironment).ConfigureAwait(false);

            if (caller == null)
            {
                return TypedResults.Unauthorized();
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            var rows = await new UserNotificationRepository(dbContext)
                .ListActiveAsync(caller.Value.TenantRegistryId, caller.Value.UserName, PageSize, token)
                .ConfigureAwait(false);

            var caseIds = rows.Where(r => r.CaseId.HasValue).Select(r => r.CaseId!.Value).Distinct().ToList();

            var visible = (await new CaseRepository(dbContext, caller.Value.UserName)
                .GetVisibleIdsActiveOnlyAsync(caseIds, token).ConfigureAwait(false)).ToHashSet();

            var holdsReports = await Jube.Service.Repository.CaseReport.CaseReportService
                .HoldsAnyAsync(dbContext, caller.Value.UserName, log, token).ConfigureAwait(false);

            return TypedResults.Ok(rows
                .Where(r => !r.CaseId.HasValue || visible.Contains(r.CaseId.Value))
                .Where(r => holdsReports || r.KindId is not ((short)NotificationKind.CaseReportAwaitingApproval
                    or (short)NotificationKind.CaseReportDecided or (short)NotificationKind.CaseReportFeedback))
                .Select(r => new UserNotificationDto
                {
                    Guid = r.Guid,
                    KindId = r.KindId,
                    PriorityId = r.PriorityId,
                    CaseId = r.CaseId,
                    ArgumentsJson = r.ArgumentsJson,
                    CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(r.CreatedDate, DateTimeKind.Utc)),
                    Read = r.ReadDate.HasValue
                })
                .ToList());
        }

        private static async Task<IResult> MarkReadAsync(Guid guid, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            var caller = await ResolveAsync(httpContext, log, dynamicEnvironment).ConfigureAwait(false);

            if (caller == null)
            {
                return TypedResults.Unauthorized();
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            await new UserNotificationRepository(dbContext)
                .MarkReadAsync(caller.Value.TenantRegistryId, caller.Value.UserName, guid, token)
                .ConfigureAwait(false);

            return TypedResults.NoContent();
        }

        private static async Task<IResult> MarkAllReadAsync(HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            var caller = await ResolveAsync(httpContext, log, dynamicEnvironment).ConfigureAwait(false);

            if (caller == null)
            {
                return TypedResults.Unauthorized();
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            await new UserNotificationRepository(dbContext)
                .MarkAllReadAsync(caller.Value.TenantRegistryId, caller.Value.UserName, token)
                .ConfigureAwait(false);

            return TypedResults.NoContent();
        }
    }
}
