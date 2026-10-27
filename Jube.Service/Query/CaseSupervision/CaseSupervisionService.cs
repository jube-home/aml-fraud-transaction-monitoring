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

using System.ComponentModel;
using FluentValidation.Results;
using Jube.CaseRisk.Analytics;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseAnalytics;
using Jube.Dto.Query.CaseSupervision;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseSupervision;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Query.CaseSupervision;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.CaseSupervision
{
    public sealed class CaseSupervisionService
    {
        public const int BreakLockEventTypeId = 18;
        public const int AllocateLockEventTypeId = 14;
        public const int DefaultIdleMinutes = 120;
        public const int MaxStuckLocks = 200;
        private static readonly int[] permissions = [18];
        private static readonly int[] takenEventTypes = [2, 6];
        private readonly ILog auditLog;
        private readonly CaseSearchFilterRepository caseSearchFilterRepository;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseSupervisionService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            caseSearchFilterRepository = new CaseSearchFilterRepository(dbContext, userName, tenantRegistryId);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseSupervisionService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseSupervisionService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseSupervisionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseSupervisionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseSupervisionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseSupervisionService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Shows a supervisor what every analyst who can work a Case Workflow is holding and has done " +
                     "over a period: cases held now and how long the oldest has been held, cases taken, peeked " +
                     "at and closed, and the median time to close, plus how many open cases nobody holds. Needs " +
                     "the Read Write Case Workflow permission and a role granted the workflow.")]
        [ServiceOperation("CaseSupervisionWorkload", OperationKind.Read, Idempotent = true)]
        public async Task<CaseSupervisionDto> WorkloadAsync(CaseAnalyticsRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSupervision", "Workload", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();
                await EnsureWorkflowAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);

                var end = request.To?.UtcDateTime ?? DateTime.UtcNow;
                var start = request.From?.UtcDateTime ?? end.AddDays(-30);
                var workflowGuid = request.CaseWorkflowGuid;
                var now = DateTime.UtcNow;

                var eligible = await EligibleAsync(workflowGuid, null, token).ConfigureAwait(false);

                var open = dbContext.Case.Where(c => c.CaseWorkflowGuid == workflowGuid
                                                     && (c.ClosedStatusId == 0 || c.ClosedStatusId == 1));

                var holds = await open.Where(c => c.Locked == 1 && c.LockedUser != null)
                    .Select(c => new { User = c.LockedUser!, c.LockedDate }).ToListAsync(token).ConfigureAwait(false);

                var unassigned = await open.CountAsync(c => c.Locked != 1 || c.LockedUser == null, token)
                    .ConfigureAwait(false);

                var events = await (from e in dbContext.CaseEvent
                        join c in dbContext.Case on e.CaseId equals c.Id
                        where c.CaseWorkflowGuid == workflowGuid && e.CreatedDate >= start && e.CreatedDate <= end
                                                                 && (e.CaseEventTypeId == 2 || e.CaseEventTypeId == 6
                                                                     || e.CaseEventTypeId == 16)
                        select new { User = e.CreatedUser, Type = e.CaseEventTypeId ?? 0 })
                    .ToListAsync(token).ConfigureAwait(false);

                var closed = await dbContext.Case.Where(c => c.CaseWorkflowGuid == workflowGuid
                                                             && c.ClosedStatusId == 3 && c.ClosedUser != null
                                                             && c.ClosedDate >= start && c.ClosedDate <= end)
                    .Select(c => new { User = c.ClosedUser!, c.CreatedDate, c.ClosedDate })
                    .ToListAsync(token).ConfigureAwait(false);

                var names = eligible.Concat(holds.Select(h => h.User)).Concat(events.Select(e => e.User))
                    .Concat(closed.Select(c => c.User)).Where(n => !string.IsNullOrEmpty(n))
                    .Distinct(StringComparer.Ordinal).ToList();

                var rows = names.Select(name =>
                {
                    var mine = holds.Where(h => h.User == name).ToList();
                    var done = closed.Where(c => c.User == name && c.CreatedDate.HasValue && c.ClosedDate.HasValue)
                        .Select(c => Math.Max(0, (c.ClosedDate!.Value - c.CreatedDate!.Value).TotalMinutes)).ToList();

                    return new CaseAnalystWorkloadDto
                    {
                        User = name,
                        Holding = mine.Count,
                        OldestHoldMinutes = mine.Any(h => h.LockedDate.HasValue)
                            ? Math.Max(0, (now - mine.Where(h => h.LockedDate.HasValue).Min(h => h.LockedDate!.Value))
                                .TotalMinutes)
                            : null,
                        Taken = events.Count(e => e.User == name && takenEventTypes.Contains(e.Type)),
                        Peeks = events.Count(e => e.User == name && e.Type == 16),
                        Closed = closed.Count(c => c.User == name),
                        MedianResolutionMinutes = done.Count > 0 ? DistributionStats.Of(done).P50 : null
                    };
                }).OrderByDescending(r => r.Holding).ThenByDescending(r => r.Closed).ThenBy(r => r.User,
                    StringComparer.Ordinal).ToList();

                op.Rows(rows.Count);

                return new CaseSupervisionDto
                {
                    CaseWorkflowGuid = workflowGuid,
                    From = new DateTimeOffset(DateTime.SpecifyKind(start, DateTimeKind.Utc)),
                    To = new DateTimeOffset(DateTime.SpecifyKind(end, DateTimeKind.Utc)),
                    Unassigned = unassigned,
                    Held = holds.Count,
                    Analysts = rows
                };
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        [Description("Lists the open cases of a Case Workflow that have stayed locked to someone with no activity " +
                     "on them for at least the given minutes, longest idle first, capped at 200. Activity is " +
                     "any event on the case, or the lock itself when nothing has happened since.")]
        [ServiceOperation("CaseSupervisionStuckLocks", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseStuckLockDto>> StuckLocksAsync(
            [Description("Guid of the Case Workflow.")]
            Guid caseWorkflowGuid,
            [Description("The minutes a lock must have been idle to be listed. Defaults to 120.")]
            int idleMinutes = DefaultIdleMinutes,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSupervision", "StuckLocks", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted();
                await EnsureWorkflowAsync(caseWorkflowGuid, token).ConfigureAwait(false);

                var idle = Math.Clamp(idleMinutes, 1, 525_600);
                var now = DateTime.UtcNow;
                var cutoff = now.AddMinutes(-idle);

                var locked = await (from c in dbContext.Case
                        where c.CaseWorkflowGuid == caseWorkflowGuid && (c.ClosedStatusId == 0 || c.ClosedStatusId == 1)
                              && c.Locked == 1 && c.LockedUser != null
                        select new
                        {
                            c.Id, c.CaseKey, c.CaseKeyValue, c.LockedUser, c.LockedDate, c.CreatedDate,
                            StatusName = c.CaseWorkflowStatus.Name,
                            LastEvent = dbContext.CaseEvent.Where(e => e.CaseId == c.Id)
                                .Max(e => e.CreatedDate)
                        }).ToListAsync(token).ConfigureAwait(false);

                var stuck = locked.Select(c => new
                    {
                        Row = c,
                        Last = new[] { c.LastEvent, c.LockedDate }.Where(d => d.HasValue)
                            .Select(d => d!.Value).DefaultIfEmpty(c.CreatedDate ?? now).Max()
                    }).Where(c => c.Last <= cutoff).OrderBy(c => c.Last).Take(MaxStuckLocks).ToList();

                var ids = stuck.Select(s => s.Row.Id).ToList();

                var assessments = ids.Count == 0
                    ? []
                    : await dbContext.CaseRiskAssessment.Where(a => ids.Contains(a.CaseId)
                                                                    && a.Sequence == dbContext.CaseRiskAssessment
                                                                        .Where(x => x.CaseId == a.CaseId)
                                                                        .Max(x => x.Sequence))
                        .Select(a => new { a.CaseId, a.LaneId, a.Priority }).ToListAsync(token)
                        .ConfigureAwait(false);

                op.Rows(stuck.Count);

                return stuck.Select(s =>
                {
                    var assessment = assessments.FirstOrDefault(a => a.CaseId == s.Row.Id);

                    return new CaseStuckLockDto
                    {
                        CaseId = s.Row.Id,
                        CaseKey = s.Row.CaseKey,
                        CaseKeyValue = s.Row.CaseKeyValue,
                        StatusName = s.Row.StatusName,
                        LockedUser = s.Row.LockedUser,
                        LockedDate = s.Row.LockedDate.HasValue
                            ? new DateTimeOffset(DateTime.SpecifyKind(s.Row.LockedDate.Value, DateTimeKind.Utc))
                            : null,
                        LastActivityDate = new DateTimeOffset(DateTime.SpecifyKind(s.Last, DateTimeKind.Utc)),
                        IdleMinutes = Math.Max(0, (now - s.Last).TotalMinutes),
                        Priority = assessment?.Priority ?? 0,
                        Lane = assessment?.LaneId ?? 0
                    };
                }).ToList();
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        [Description("Releases the lock held on an open case so that anyone may take it, recording who broke it " +
                     "and why in the case journal. The case must be open and locked.")]
        [ServiceOperation("CaseSupervisionBreakLock", OperationKind.Write)]
        public async Task BreakLockAsync(CaseBreakLockDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSupervision", "BreakLock", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted();
                Validate(new CaseBreakLockDtoValidator(strings).Validate(model));

                var existing = await GetCaseAsync(model.CaseId, token).ConfigureAwait(false);

                if (existing.ClosedStatusId is not (0 or 1))
                {
                    throw new ConflictException(strings[CaseSupervisionResources.CaseNotOpen]);
                }

                if (existing.Locked != 1)
                {
                    throw new ConflictException(strings[CaseSupervisionResources.CaseNotLocked]);
                }

                var holder = existing.LockedUser;

                var changed = await dbContext.Case.Where(c => c.Id == existing.Id && c.Locked == 1
                                                              && c.LockedUser == holder)
                    .Set(s => s.Locked, (byte?)0)
                    .Set(s => s.LockedUser, (string?)null)
                    .Set(s => s.LockedDate, (DateTime?)null)
                    .UpdateAsync(token).ConfigureAwait(false);

                if (changed == 0)
                {
                    throw new ConflictException(strings[CaseSupervisionResources.CaseChanged]);
                }

                await new CaseEventRepository(dbContext, userName).InsertAsync(new CaseEvent
                {
                    CaseId = existing.Id,
                    CaseKey = existing.CaseKey,
                    CaseKeyValue = existing.CaseKeyValue,
                    CaseEventTypeId = BreakLockEventTypeId,
                    Before = holder,
                    After = string.IsNullOrWhiteSpace(model.Reason) ? null : model.Reason.Trim()
                }, token).ConfigureAwait(false);

                op.Entity(existing.Id);
                op.Updated();
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        [Description("Locks an open case to another analyst, whether it is held by someone now or free, recording " +
                     "the handover in the case journal. The analyst must be in the tenant, hold the Read Write " +
                     "Case permission and have a role granted both the workflow and the status the case is in, so " +
                     "that the case does not vanish from their view.")]
        [ServiceOperation("CaseSupervisionReassign", OperationKind.Write)]
        public async Task ReassignAsync(CaseReassignDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSupervision", "Reassign", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted();
                Validate(new CaseReassignDtoValidator(strings).Validate(model));

                var existing = await GetCaseAsync(model.CaseId, token).ConfigureAwait(false);

                if (existing.ClosedStatusId is not (0 or 1))
                {
                    throw new ConflictException(strings[CaseSupervisionResources.CaseNotOpen]);
                }

                var target = model.ToUser!.Trim();

                var eligible = await EligibleAsync(existing.CaseWorkflowGuid, existing.CaseWorkflowStatusGuid, token)
                    .ConfigureAwait(false);

                if (!eligible.Contains(target, StringComparer.Ordinal))
                {
                    throw new DtoValidationException(new ValidationResult(
                    [
                        new ValidationFailure(nameof(CaseReassignDto.ToUser),
                            strings[CaseSupervisionResources.AnalystNotEligible]) { ErrorCode = "AnalystNotEligible" }
                    ]));
                }

                var previous = existing.Locked == 1 ? existing.LockedUser : null;
                var wasLocked = existing.Locked;

                var changed = await dbContext.Case.Where(c => c.Id == existing.Id && c.Locked == wasLocked
                                                              && c.LockedUser == existing.LockedUser)
                    .Set(s => s.Locked, (byte?)1)
                    .Set(s => s.LockedUser, target)
                    .Set(s => s.LockedDate, DateTime.UtcNow)
                    .UpdateAsync(token).ConfigureAwait(false);

                if (changed == 0)
                {
                    throw new ConflictException(strings[CaseSupervisionResources.CaseChanged]);
                }

                await new CaseEventRepository(dbContext, userName).InsertAsync(new CaseEvent
                {
                    CaseId = existing.Id,
                    CaseKey = existing.CaseKey,
                    CaseKeyValue = existing.CaseKeyValue,
                    CaseEventTypeId = AllocateLockEventTypeId,
                    Before = previous,
                    After = target
                }, token).ConfigureAwait(false);

                op.Entity(existing.Id);
                op.Updated();
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        private async Task<Jube.Data.Poco.Case> GetCaseAsync(int caseId, CancellationToken token)
        {
            var existing = await new CaseRepository(dbContext, userName).GetByIdActiveOnlyAsync(caseId, token)
                .ConfigureAwait(false);

            if (existing == null)
            {
                throw new NotFoundException(strings[CaseSupervisionResources.NotFound]);
            }

            return existing;
        }

        private Task<List<string>> EligibleAsync(Guid caseWorkflowGuid, Guid? statusGuid,
            CancellationToken token)
        {
            return (from u in dbContext.UserRegistry
                    join t in dbContext.UserInTenant on u.Name equals t.User
                    join rr in dbContext.RoleRegistry on u.RoleRegistryGuid equals rr.Guid
                    where t.TenantRegistryId == tenantRegistryId
                          && u.Active == 1 && rr.Active == 1
                          && (u.PasswordLocked == 0 || u.PasswordLocked == null)
                          && (u.Deleted == 0 || u.Deleted == null)
                          && (rr.Deleted == 0 || rr.Deleted == null)
                          && dbContext.RoleRegistryPermission.Any(p => p.RoleRegistryId == rr.Id
                                                                       && p.PermissionSpecificationId == 1
                                                                       && p.Active == 1
                                                                       && (p.Deleted == 0 || p.Deleted == null))
                          && dbContext.CaseWorkflowRole.Any(r => r.CaseWorkflowGuid == caseWorkflowGuid
                                                                 && r.RoleRegistryGuid == u.RoleRegistryGuid
                                                                 && (r.Deleted == 0 || r.Deleted == null))
                          && (statusGuid == null
                              || dbContext.CaseWorkflowStatusRole.Any(r => r.CaseWorkflowStatusGuid == statusGuid
                                                                           && r.RoleRegistryGuid == u.RoleRegistryGuid
                                                                           && (r.Deleted == 0 || r.Deleted == null)))
                    select u.Name!).Distinct().ToListAsync(token);
        }

        private async Task EnsureWorkflowAsync(Guid caseWorkflowGuid, CancellationToken token)
        {
            if (await caseSearchFilterRepository.GetAccessibleCaseWorkflowIdAsync(caseWorkflowGuid, token)
                    .ConfigureAwait(false) == null)
            {
                throw new NotFoundException(strings[CaseSupervisionResources.WorkflowNotFound]);
            }
        }

        private static void Validate(ValidationResult result)
        {
            if (!result.IsValid)
            {
                throw new DtoValidationException(result);
            }
        }

        private bool Classify(OperationScope op, Exception ex)
        {
            switch (ex)
            {
                case ForbiddenException:
                    op.Outcome("forbidden");
                    break;
                case NotFoundException:
                    op.Outcome("not_found");
                    break;
                case DtoValidationException:
                    op.Outcome("invalid");
                    break;
                case ConflictException:
                    op.Outcome("conflict");
                    break;
                case OperationCanceledException:
                    op.Outcome("cancelled");
                    break;
                default:
                    op.Error(ex);
                    log.Error($"CaseSupervision: unexpected failure user={userName}", ex);
                    break;
            }

            return false;
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            throw new ForbiddenException(strings[CaseSupervisionResources.PermissionDenied], permissions);
        }
    }
}
