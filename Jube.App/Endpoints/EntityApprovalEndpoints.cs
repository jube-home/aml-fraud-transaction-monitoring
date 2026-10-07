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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Dto.EntityApproval;
using Jube.Resources;
using Jube.Service.EntityApproval;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Validations.EntityApproval;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints
{
    public static class EntityApprovalEndpoints
    {
        private const string Base = "/api/EntityApproval";

        public static void MapEntityApprovalEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("EntityApproval");

            group.MapPost("Approve", ApproveAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.BadRequest)
                .WithName("EntityApprovalApprove");

            group.MapPost("Reject", RejectAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.BadRequest)
                .WithName("EntityApprovalReject");

            group.MapGet("History", HistoryAsync)
                .Produces<List<EntityApprovalDto>>()
                .WithName("EntityApprovalHistory");

            group.MapGet("Pending", PendingAsync)
                .Produces<List<PendingApprovalDto>>()
                .WithName("EntityApprovalPending");

            group.MapGet("Changes", ChangesAsync)
                .Produces<EntityApprovalChangesDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NotFound)
                .WithName("EntityApprovalChanges");

            group.MapPost("ApproveValues", ApproveValuesAsync)
                .Produces<EntityApprovalBulkResultDto>()
                .Produces((int)HttpStatusCode.BadRequest)
                .WithName("EntityApprovalApproveValues");

            group.MapGet("Status", StatusAsync)
                .Produces<EntityApprovalStatusDto>()
                .WithName("EntityApprovalStatus");

            group.MapGet("PendingByKind", PendingByKindAsync)
                .Produces<List<PendingApprovalDto>>()
                .WithName("EntityApprovalPendingByKind");

            group.MapPost("ValueStatus", ValueStatusAsync)
                .Produces<List<EntityApprovalStatusDto>>()
                .Produces((int)HttpStatusCode.BadRequest)
                .WithName("EntityApprovalValueStatus");
        }

        private static async Task<IResult> ValueStatusAsync(HttpContext httpContext, EntityApprovalBulkDto body,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name ?? string.Empty;

            if (!Enum.IsDefined(typeof(EntityApprovalKind), body.Kind))
            {
                return TypedResults.BadRequest(new { message = "The approval kind is not recognised." });
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await EntityApprovalService.CreateAsync(dbContext, user, log,
                    ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")), token);

                var statuses = await service.ValueStatusAsync((EntityApprovalKind)body.Kind, body.EntityId, token);
                return TypedResults.Ok(statuses.Select(ToStatusDto).ToList());
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (ApprovalRefusedException ex)
            {
                return TypedResults.BadRequest(new { message = ex.Message, reasons = ex.Reasons });
            }
            catch (KeyNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/ValueStatus: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static EntityApprovalStatusDto ToStatusDto(EntityApprovalStatus status)
        {
            return new EntityApprovalStatusDto(
                (int)status.Kind, status.EntityId, status.Name, status.CurrentVersion, status.EffectiveVersion,
                status.Deleted, status.Pending, status.Rejected, status.MakerUser, status.ApprovalsRecorded,
                status.ApprovalsRequired, status.CanApprove);
        }

        private static async Task<IResult> ApproveValuesAsync(HttpContext httpContext, EntityApprovalBulkDto body,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name ?? string.Empty;

            var strings = stringLocalizerFactory.Create(typeof(EntityApprovalResources));
            var validation = await new EntityApprovalBulkDtoValidator(strings).ValidateAsync(body, token);
            if (!validation.IsValid)
            {
                log.Warn($"POST {Base}/ApproveValues: 400 user={user} errors={validation.Errors.Count}");
                return TypedResults.BadRequest(validation);
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await EntityApprovalService.CreateAsync(dbContext, user, log,
                    ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")), token);

                var result = await service.ApproveValuesAsync((EntityApprovalKind)body.Kind, body.EntityId, token);
                return TypedResults.Ok(new EntityApprovalBulkResultDto(result.Approved, result.Refusals));
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (ApprovalRefusedException ex)
            {
                return TypedResults.BadRequest(new { message = ex.Message, reasons = ex.Reasons });
            }
            catch (KeyNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/ApproveValues: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> StatusAsync(HttpContext httpContext, int kind, int entityId, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name ?? string.Empty;

            if (!Enum.IsDefined(typeof(EntityApprovalKind), kind))
            {
                return TypedResults.BadRequest(new { message = "The approval kind is not recognised." });
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await EntityApprovalService.CreateAsync(dbContext, user, log,
                    ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")), token);

                var status = await service.StatusAsync((EntityApprovalKind)kind, entityId, token);
                return TypedResults.Ok(ToStatusDto(status));
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (KeyNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/Status: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> PendingByKindAsync(HttpContext httpContext, int kind, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name ?? string.Empty;

            if (!Enum.IsDefined(typeof(EntityApprovalKind), kind))
            {
                return TypedResults.BadRequest(new { message = "The approval kind is not recognised." });
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await EntityApprovalService.CreateAsync(dbContext, user, log,
                    ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")), token);

                var pending = await service.PendingForKindAsync((EntityApprovalKind)kind, token);
                return TypedResults.Ok(pending.Select(p => new PendingApprovalDto(
                        (int)p.Kind, p.EntityId, p.Name, p.Version, p.Deleted, p.MakerUser, p.ModelId, p.ParentId))
                    .ToList());
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/PendingByKind: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static Task<IResult> ApproveAsync(HttpContext httpContext, EntityApprovalDecisionDto body,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, CancellationToken token)
        {
            return DecideAsync(httpContext, body, log, dynamicEnvironment, stringLocalizerFactory, token,
                approve: true);
        }

        private static Task<IResult> RejectAsync(HttpContext httpContext, EntityApprovalDecisionDto body,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, CancellationToken token)
        {
            return DecideAsync(httpContext, body, log, dynamicEnvironment, stringLocalizerFactory, token,
                approve: false);
        }

        private static async Task<IResult> DecideAsync(HttpContext httpContext, EntityApprovalDecisionDto body,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, CancellationToken token, bool approve)
        {
            var user = httpContext.User.Identity?.Name ?? string.Empty;
            var verb = approve ? "Approve" : "Reject";

            var strings = stringLocalizerFactory.Create(typeof(EntityApprovalResources));
            var validation = await new EntityApprovalDecisionDtoValidator(strings).ValidateAsync(body, token);
            if (!validation.IsValid)
            {
                log.Warn($"POST {Base}/{verb}: 400 user={user} errors={validation.Errors.Count}");
                return TypedResults.BadRequest(validation);
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await EntityApprovalService.CreateAsync(dbContext, user, log,
                    ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")), token);

                var kind = (EntityApprovalKind)body.Kind;
                var approval = approve
                    ? await service.ApproveAsync(kind, body.EntityId, body.Version, body.Note, token)
                    : await service.RejectAsync(kind, body.EntityId, body.Version, body.Note, token);

                return TypedResults.Ok(ToDto(approval));
            }
            catch (NotAuthenticatedException)
            {
                log.Warn($"POST {Base}/{verb}: 403 (not authenticated) user={user}");
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                log.Warn($"POST {Base}/{verb}: 403 user={user}");
                return TypedResults.Forbid();
            }
            catch (ApprovalRefusedException ex)
            {
                log.Warn($"POST {Base}/{verb}: 400 (refused) user={user}: {ex.Message}");
                return TypedResults.BadRequest(new { message = ex.Message, reasons = ex.Reasons });
            }
            catch (KeyNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (OperationCanceledException)
            {
                log.Debug($"POST {Base}/{verb}: client cancelled user={user}");
                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/{verb}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> HistoryAsync(HttpContext httpContext, int kind, int entityId, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name ?? string.Empty;

            if (!Enum.IsDefined(typeof(EntityApprovalKind), kind))
            {
                return TypedResults.BadRequest(new { message = "The approval kind is not recognised." });
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await EntityApprovalService.CreateAsync(dbContext, user, log,
                    ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")), token);

                var rows = await service.HistoryAsync((EntityApprovalKind)kind, entityId, token);
                return TypedResults.Ok(rows.Select(ToDto).ToList());
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/History: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> PendingAsync(HttpContext httpContext, int modelId, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name ?? string.Empty;

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await EntityApprovalService.CreateAsync(dbContext, user, log,
                    ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")), token);

                var pending = await service.PendingForModelAsync(modelId, token);
                return TypedResults.Ok(pending.Select(p => new PendingApprovalDto(
                        (int)p.Kind, p.EntityId, p.Name, p.Version, p.Deleted, p.MakerUser, p.ModelId, p.ParentId))
                    .ToList());
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/Pending: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> ChangesAsync(HttpContext httpContext, int kind, int entityId, int version,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name ?? string.Empty;

            if (!Enum.IsDefined(typeof(EntityApprovalKind), kind))
            {
                return TypedResults.BadRequest(new { message = "The approval kind is not recognised." });
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var service = await EntityApprovalService.CreateAsync(dbContext, user, log,
                    ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")), token);

                var changes = await service.ChangesAsync((EntityApprovalKind)kind, entityId, version, token);
                return TypedResults.Ok(new EntityApprovalChangesDto(changes.HasEarlierVersion,
                    changes.Changes.Select(c => new EntityApprovalChangeDto(c.PropertyName,
                        VersionText.Preview(c.FromValue), VersionText.Preview(c.ToValue),
                        VersionText.ChangeKind(c.FromValue, c.ToValue))).ToList()));
            }
            catch (NotAuthenticatedException)
            {
                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                return TypedResults.Forbid();
            }
            catch (ApprovalRefusedException ex)
            {
                return TypedResults.BadRequest(new { message = ex.Message, reasons = ex.Reasons });
            }
            catch (KeyNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/Changes: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static EntityApprovalDto ToDto(EntityApproval approval)
        {
            return new EntityApprovalDto(approval.Id, approval.EntityApprovalKindId ?? 0, approval.EntityId ?? 0,
                approval.EntityVersion ?? 0, approval.StateId == (int)EntityApprovalState.Rejected
                    ? "Rejected"
                    : "Approved", approval.Note, approval.CreatedUser, approval.CreatedDate);
        }
    }
}