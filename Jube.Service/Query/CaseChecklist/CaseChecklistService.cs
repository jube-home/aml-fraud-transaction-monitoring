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
using System.Text.Json;
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Query.GetCaseChecklistItemValidationQuery;
using Jube.Data.Poco;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseChecklist;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseChecklist;
using Jube.Service.Observability;
using Jube.Service.Query.CaseChecklist.Interfaces;
using Jube.Service.Query.CaseChecklist.Models;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using CaseRow = Jube.Data.Poco.Case;

namespace Jube.Service.Query.CaseChecklist
{
    public sealed class CaseChecklistService
    {
        public const int ShownEventTypeId = 21;
        public const int HiddenEventTypeId = 22;
        public const int CheckedEventTypeId = 23;
        public const int UncheckedEventTypeId = 24;
        public const int GateBlockedEventTypeId = 25;
        public const int GateOverriddenEventTypeId = 26;
        public const int GateBypassedEventTypeId = 27;
        public const int SlaBreachedEventTypeId = 28;
        public const string SystemActor = "System";
        public const int OverridePermission = 71;
        public const int DefaultReconcileMaxAgeSeconds = 60;
        public const int MaxReasonLength = 1000;
        private const int AdvisoryLockNamespace = 20263108;
        private const int ClosedStatusId = 3;
        private const int HistoryLimit = 200;
        private static readonly TimeSpan sqlTimeout = TimeSpan.FromSeconds(3);
        private static readonly int[] permissions = [1];
        private static readonly int[] overridePermissions = [OverridePermission];
        private readonly ILog auditLog;
        private readonly TimeProvider clock;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ICaseChecklistReconciler reconciler;
        private readonly IFormSqlRunner sqlRunner;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly TimeSpan reconcileMaxAge;

        private CaseChecklistService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, ICaseChecklistReconciler reconciler, IFormSqlRunner sqlRunner,
            TimeProvider clock, TimeSpan reconcileMaxAge)
        {
            this.reconcileMaxAge = reconcileMaxAge;
            this.sqlRunner = sqlRunner;
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.reconciler = reconciler;
            this.clock = clock;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseChecklistService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), TimeProvider.System, token);
        }

        internal static async Task<CaseChecklistService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog, TimeProvider clock,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseChecklistResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseChecklistResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseChecklistResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            var sqlRunner = FormValidatorRuntime.SqlRunner(dynamicEnvironment, userName, log);
            var reconciler = new CaseChecklistReconciler(dbContext, sqlRunner, clock, log);

            return new CaseChecklistService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, reconciler, sqlRunner, clock,
                ReconcileMaxAge(dynamicEnvironment));
        }

        public static TimeSpan ReconcileMaxAge(DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            return int.TryParse(dynamicEnvironment.AppSettings("CaseChecklistReconcileMaxAgeSeconds"), out var seconds)
                   && seconds >= 0
                ? TimeSpan.FromSeconds(seconds)
                : TimeSpan.FromSeconds(DefaultReconcileMaxAgeSeconds);
        }

        [Description("Gets the checklists of the status a case is in. The items are first brought up to date: " +
                     "an item appears when its show condition passes and is hidden again when it stops passing " +
                     "unless it has been ticked. Returns each checklist with its items, whether each is shown or " +
                     "done, its SLA position (on track, breached, completed on time or late) and the minutes left, " +
                     "and whether everything required is done. SIDE EFFECTS: may record that items were shown or " +
                     "hidden on the case timeline.")]
        [ServiceOperation("CaseChecklistGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseChecklistDto> GetAsync(
            [Description("Identifier of the case.")]
            int caseId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseChecklist", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();

                var existing = await new CaseRepository(dbContext, userName).GetByIdActiveOnlyAsync(caseId, token)
                    .ConfigureAwait(false);

                if (existing == null)
                {
                    throw new NotFoundException(strings[CaseChecklistResources.NotFound]);
                }

                var changes = await reconciler.ReconcileIfNeededAsync(caseId, SystemActor, "Read",
                    reconcileMaxAge, token).ConfigureAwait(false);

                var result = await SnapshotAsync(caseId, token).ConfigureAwait(false);

                op.Entity(caseId);
                op.Rows(result.Required);

                if (changes > 0)
                {
                    op.Updated();
                }

                return result;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseChecklist.Get: unexpected failure caseId={caseId} user={userName}", ex);
                throw;
            }
        }

        [Description("Gets the audit of the checklist of a case, newest first and at most 200 entries: items shown " +
                     "and hidden, ticks and unticks, refused ticks, and the gate blocked, overridden or bypassed, " +
                     "each with who did it and when. Does not bring the checklist up to date.")]
        public async Task<List<CaseChecklistHistoryEntryDto>> GetHistoryAsync(
            [Description("Identifier of the case.")]
            int caseId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseChecklist", "History", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();

                var existing = await new CaseRepository(dbContext, userName).GetByIdActiveOnlyAsync(caseId, token)
                    .ConfigureAwait(false);

                if (existing == null)
                {
                    throw new NotFoundException(strings[CaseChecklistResources.NotFound]);
                }

                var events = await new CaseChecklistEventRepository(dbContext, tenantRegistryId)
                    .ListRecentForCaseAsync(caseId, HistoryLimit, token).ConfigureAwait(false);

                var names = (await new CaseChecklistItemRepository(dbContext, tenantRegistryId)
                        .ListForCaseAsync(caseId, token).ConfigureAwait(false))
                    .ToDictionary(k => k.Id, v => v.ItemName);

                op.Entity(caseId);
                op.Rows(events.Count);

                return events.Select(e => new CaseChecklistHistoryEntryDto
                {
                    Id = e.Id,
                    ActionId = e.ActionId,
                    ItemName = e.CaseChecklistItemId.HasValue
                        ? names.GetValueOrDefault(e.CaseChecklistItemId.Value)
                        : null,
                    Actor = e.Actor,
                    CreatedDate = CaseSearchMapper.Utc(e.CreatedDate),
                    Detail = e.Detail
                }).ToList();
            }
            catch (Exception ex)
            {
                Classify(op, ex, "History", caseId);
                throw;
            }
        }

        [Description("Ticks one item of the checklist of the status a case is in. Only the user who holds the case can " +
                     "tick an item, and only an item that is currently shown. If the item has a validation " +
                     "condition it must pass first, otherwise the item stays unticked and the refusal is recorded. " +
                     "Returns the checklists as they now stand. SIDE EFFECTS: records the tick on the case timeline.")]
        [ServiceOperation("CaseChecklistCheck", OperationKind.Write)]
        public async Task<CaseChecklistDto> CheckAsync(
            [Description("Identifier of the case.")]
            int caseId,
            [Description("Identifier of the checklist item on the case, as returned by getting the checklist.")]
            long caseChecklistItemId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseChecklist", "Check", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();

                var caseRow = await LoadHeldCaseAsync(caseId, token).ConfigureAwait(false);
                string? failureMessage = null;

                await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

                try
                {
                    await LockCaseAsync(caseId, token).ConfigureAwait(false);
                    await reconciler.ReconcileAsync(caseId, userName, "Check", token).ConfigureAwait(false);

                    var items = new CaseChecklistItemRepository(dbContext, tenantRegistryId);
                    var item = await items.GetAsync(caseId, caseChecklistItemId, token)
                        .ConfigureAwait(false);

                    if (item == null)
                    {
                        throw new NotFoundException(strings[CaseChecklistResources.ItemNotFound]);
                    }

                    if (item.StateId == (byte)CaseChecklistItemState.Done)
                    {
                        throw new ConflictException(strings[CaseChecklistResources.ItemAlreadyDone]);
                    }

                    if (item.StateId != (byte)CaseChecklistItemState.Shown
                        || item.CaseWorkflowStatusGuid != caseRow.CaseWorkflowStatusGuid)
                    {
                        throw new ConflictException(strings[CaseChecklistResources.ItemNotShown]);
                    }

                    var now = clock.GetUtcNow().UtcDateTime;
                    var audit = new CaseChecklistAuditWriter(dbContext, tenantRegistryId);

                    var definition = await new GetCaseChecklistItemValidationQuery(dbContext)
                        .ExecuteAsync(item.CaseWorkflowChecklistItemGuid, token).ConfigureAwait(false);

                    var outcome = await ValidateAsync(definition, caseRow, token).ConfigureAwait(false);

                    if (outcome.Passed)
                    {
                        item.StateId = (byte)CaseChecklistItemState.Done;
                        item.CompletedDate = now;
                        item.CompletedUser = userName;
                        item.UpdatedDate = now;
                        await items.UpdateAsync(item, token).ConfigureAwait(false);

                        await audit.WriteAsync(caseRow, CaseChecklistAction.Checked, userName, item, item.ItemName,
                            outcome.Detail, CheckedEventTypeId, null, now, token).ConfigureAwait(false);

                        await new CaseChecklistSlaBreachRecorder(dbContext, clock, log).RecordCompletedLateAsync(
                            caseRow, tenantRegistryId, item, userName, now, token).ConfigureAwait(false);
                    }
                    else
                    {
                        failureMessage = string.IsNullOrWhiteSpace(definition?.ValidateMessage)
                            ? strings[CaseChecklistResources.ValidationFailedDefault].Value
                            : definition.ValidateMessage;

                        await audit.WriteAsync(caseRow, CaseChecklistAction.ValidationFailed, userName, item,
                            item.ItemName, outcome.Detail, null, null, now, token).ConfigureAwait(false);
                    }

                    await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                }
                catch
                {
                    await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                    throw;
                }

                if (failureMessage != null)
                {
                    throw new DtoValidationException(new ValidationResult(
                    [
                        new ValidationFailure("ValidateSql", failureMessage)
                        {
                            ErrorCode = "ChecklistItemValidationFailed"
                        }
                    ]));
                }

                op.Entity(caseId);
                op.Updated();

                return await SnapshotAsync(caseId, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Classify(op, ex, "Check", caseId);
                throw;
            }
        }

        [Description("Unticks one item of the checklist of the status a case is in, putting it back to shown. Only " +
                     "the user who holds the case can untick an item, and only an item that is ticked. The SLA clock " +
                     "of the item is not restarted. Returns the checklists as they now stand. SIDE EFFECTS: records " +
                     "the untick on the case timeline.")]
        [ServiceOperation("CaseChecklistUncheck", OperationKind.Write)]
        public async Task<CaseChecklistDto> UncheckAsync(
            [Description("Identifier of the case.")]
            int caseId,
            [Description("Identifier of the checklist item on the case, as returned by getting the checklist.")]
            long caseChecklistItemId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseChecklist", "Uncheck", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();

                var caseRow = await LoadHeldCaseAsync(caseId, token).ConfigureAwait(false);

                await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

                try
                {
                    await LockCaseAsync(caseId, token).ConfigureAwait(false);

                    var items = new CaseChecklistItemRepository(dbContext, tenantRegistryId);
                    var item = await items.GetAsync(caseId, caseChecklistItemId, token).ConfigureAwait(false);

                    if (item == null)
                    {
                        throw new NotFoundException(strings[CaseChecklistResources.ItemNotFound]);
                    }

                    if (item.StateId != (byte)CaseChecklistItemState.Done)
                    {
                        throw new ConflictException(strings[CaseChecklistResources.ItemNotDone]);
                    }

                    var now = clock.GetUtcNow().UtcDateTime;
                    var previousUser = item.CompletedUser;

                    item.StateId = (byte)CaseChecklistItemState.Shown;
                    item.CompletedDate = null;
                    item.CompletedUser = null;
                    item.UpdatedDate = now;
                    await items.UpdateAsync(item, token).ConfigureAwait(false);

                    await new CaseChecklistAuditWriter(dbContext, tenantRegistryId).WriteAsync(caseRow,
                        CaseChecklistAction.Unchecked, userName, item, item.ItemName,
                        $"Unticked by {userName}; it had been ticked by {previousUser}.", UncheckedEventTypeId,
                        null, now, token).ConfigureAwait(false);

                    await reconciler.ReconcileAsync(caseId, userName, "Uncheck", token).ConfigureAwait(false);

                    await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                }
                catch
                {
                    await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                    throw;
                }

                op.Entity(caseId);
                op.Updated();

                return await SnapshotAsync(caseId, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Classify(op, ex, "Uncheck", caseId);
                throw;
            }
        }

        public async Task<CaseChecklistOverrideDto> OverrideAsync(int caseId,
            CaseChecklistOverrideRequestDto? request, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseChecklist", "Override", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();

                if (!permissionValidation.Validate(overridePermissions))
                {
                    throw new ForbiddenException(strings[CaseChecklistResources.OverrideForbidden],
                        overridePermissions);
                }

                var reason = request?.Reason?.Trim();

                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw Invalid("Reason", strings[CaseChecklistResources.OverrideReasonRequired],
                        "OverrideReasonRequired");
                }

                if (reason.Length > MaxReasonLength)
                {
                    throw Invalid("Reason", strings[CaseChecklistResources.OverrideReasonTooLong],
                        "OverrideReasonTooLong");
                }

                var caseRow = await new CaseRepository(dbContext, userName).GetByIdActiveOnlyAsync(caseId, token)
                    .ConfigureAwait(false);

                if (caseRow == null)
                {
                    throw new NotFoundException(strings[CaseChecklistResources.NotFound]);
                }

                if (caseRow.ClosedStatusId == ClosedStatusId)
                {
                    throw new ConflictException(strings[CaseChecklistResources.CaseClosed]);
                }

                if (caseRow.Locked == 1 && string.Equals(caseRow.LockedUser, userName, StringComparison.Ordinal))
                {
                    throw new ConflictException(strings[CaseChecklistResources.OverrideHolderCannotOverride]);
                }

                CaseChecklistOverride granted;
                IReadOnlyList<OpenChecklistItem> open;

                await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

                try
                {
                    await LockCaseAsync(caseId, token).ConfigureAwait(false);

                    var gate = new CaseChecklistGate(dbContext, reconciler, () => dbContext, clock, log);
                    open = await gate.EvaluateAsync(caseId, userName, token).ConfigureAwait(false);

                    if (open.Count == 0)
                    {
                        throw new ConflictException(strings[CaseChecklistResources.OverrideNothingOpen]);
                    }

                    var overrides = new CaseChecklistOverrideRepository(dbContext, tenantRegistryId);

                    if (await overrides.GetOpenAsync(caseId, caseRow.CaseWorkflowStatusGuid, token)
                            .ConfigureAwait(false) != null)
                    {
                        throw new ConflictException(strings[CaseChecklistResources.OverrideAlreadyGranted]);
                    }

                    var now = clock.GetUtcNow().UtcDateTime;

                    granted = await overrides.InsertAsync(new CaseChecklistOverride
                    {
                        CaseId = caseId,
                        CaseWorkflowStatusGuid = caseRow.CaseWorkflowStatusGuid,
                        Reason = reason,
                        OpenItemsJson = JsonSerializer.Serialize(open.Select(s => new { s.Id, s.Name })),
                        GrantedUser = userName,
                        GrantedDate = now
                    }, token).ConfigureAwait(false);

                    await new CaseChecklistAuditWriter(dbContext, tenantRegistryId).WriteAsync(caseRow,
                        CaseChecklistAction.GateOverridden, userName, null, null,
                        $"{userName} overrode the gate for {open.Count} open item(s): " +
                        string.Join("; ", open.Select(s => s.Name)) + $". Reason: {reason}",
                        GateOverriddenEventTypeId, reason.Length > 200 ? reason[..200] : reason, now, token)
                        .ConfigureAwait(false);

                    await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                }
                catch
                {
                    await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                    throw;
                }

                op.Entity(caseId);
                op.Updated();

                return new CaseChecklistOverrideDto
                {
                    Id = granted.Id,
                    CaseId = caseId,
                    Reason = granted.Reason,
                    GrantedUser = granted.GrantedUser,
                    GrantedDate = CaseSearchMapper.Utc(granted.GrantedDate),
                    OpenItems = open.Select(s => s.Name).ToList()
                };
            }
            catch (Exception ex)
            {
                Classify(op, ex, "Override", caseId);
                throw;
            }
        }

        private async Task<CaseChecklistDto> SnapshotAsync(int caseId, CancellationToken token)
        {
            var rows = await new CaseChecklistItemRepository(dbContext, tenantRegistryId)
                .ListForCaseAsync(caseId, token).ConfigureAwait(false);

            var header = await dbContext.Case.Where(w => w.Id == caseId)
                .Select(s => new { s.CaseWorkflowStatusGuid, s.ChecklistReconciledDate })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var current = header?.CaseWorkflowStatusGuid ?? Guid.Empty;

            var checklistGuids = rows.Select(r => r.CaseWorkflowChecklistGuid).Distinct().ToList();

            var checklistPriorities = (await dbContext.CaseWorkflowChecklist
                .Where(w => checklistGuids.Contains(w.Guid) && (w.Deleted == 0 || w.Deleted == null))
                .Select(s => new { s.Guid, s.Priority }).ToListAsync(token).ConfigureAwait(false))
                .ToDictionary(k => k.Guid, v => v.Priority ?? 0);

            var snapshot = Build(caseId, current, rows, checklistPriorities, clock.GetUtcNow().UtcDateTime);

            snapshot.ReconciledDate = CaseSearchMapper.Utc(header?.ChecklistReconciledDate);

            var grant = current == Guid.Empty
                ? null
                : await new CaseChecklistOverrideRepository(dbContext, tenantRegistryId)
                    .GetOpenAsync(caseId, current, token).ConfigureAwait(false);

            snapshot.Override = grant == null ? null : OverrideView(caseId, grant);

            return snapshot;
        }

        private async Task<CaseRow> LoadHeldCaseAsync(int caseId, CancellationToken token)
        {
            var existing = await new CaseRepository(dbContext, userName).GetByIdActiveOnlyAsync(caseId, token)
                .ConfigureAwait(false);

            if (existing == null)
            {
                throw new NotFoundException(strings[CaseChecklistResources.NotFound]);
            }

            if (existing.ClosedStatusId == ClosedStatusId)
            {
                throw new ConflictException(strings[CaseChecklistResources.CaseClosed]);
            }

            if (existing.Locked != 1 || !string.Equals(existing.LockedUser, userName, StringComparison.Ordinal))
            {
                throw new ConflictException(strings[CaseChecklistResources.NotLockHolder]);
            }

            return existing;
        }

        private Task LockCaseAsync(int caseId, CancellationToken token)
        {
            return dbContext.ExecuteAsync("select pg_advisory_xact_lock(@lockNamespace, @lockCaseId)", token,
                new DataParameter("lockNamespace", AdvisoryLockNamespace, DataType.Int32),
                new DataParameter("lockCaseId", caseId, DataType.Int32));
        }

        private async Task<(bool Passed, string Detail)> ValidateAsync(
            GetCaseChecklistItemValidationQueryDto? definition, CaseRow caseRow, CancellationToken token)
        {
            if (definition == null || !definition.EnableValidation || string.IsNullOrWhiteSpace(definition.ValidateSql))
            {
                return (true, $"Ticked by {userName}; the item has no validation condition.");
            }

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = caseRow.Id, ["CaseKeyValue"] = caseRow.CaseKeyValue ?? string.Empty
            };

            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);

            limit.CancelAfter(sqlTimeout);

            try
            {
                return await sqlRunner.PassesAsync(definition.ValidateSql, parameters, limit.Token)
                    .ConfigureAwait(false)
                    ? (true, $"Ticked by {userName}; the validation condition passes.")
                    : (false, $"{userName} could not tick the item: the validation condition returned false.");
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return (false, $"{userName} could not tick the item: the validation condition timed out.");
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.Warn($"CaseChecklist.Check: validate SQL failed caseId={caseRow.Id}", e);

                return (false, $"{userName} could not tick the item: the validation condition failed to run.");
            }
        }

        private static DtoValidationException Invalid(string property, string message, string code)
        {
            return new DtoValidationException(new ValidationResult(
                [new ValidationFailure(property, message) { ErrorCode = code }]));
        }

        private void Classify(OperationScope op, Exception ex, string operation, int caseId)
        {
            switch (ex)
            {
                case ForbiddenException:
                    op.Outcome("forbidden");
                    break;
                case NotFoundException:
                    op.Outcome("not_found");
                    break;
                case ConflictException:
                    op.Outcome("conflict");
                    break;
                case DtoValidationException:
                    op.Outcome("invalid");
                    break;
                case OperationCanceledException:
                    op.Outcome("cancelled");
                    break;
                default:
                    op.Error(ex);
                    log.Error($"CaseChecklist.{operation}: unexpected failure caseId={caseId} user={userName}", ex);
                    break;
            }
        }

        internal static CaseChecklistDto Build(int caseId, Guid currentStatusGuid,
            IReadOnlyList<CaseChecklistItem> rows, IReadOnlyDictionary<Guid, int> checklistPriorities, DateTime now)
        {
            var visible = rows.Where(w => w.CaseWorkflowStatusGuid == currentStatusGuid
                                          && w.StateId != (byte)CaseChecklistItemState.Hidden).ToList();

            var groups = visible.GroupBy(g => g.CaseWorkflowChecklistGuid)
                .Select(g => new
                {
                    Guid = g.Key,
                    Name = g.First().ChecklistName,
                    Order = checklistPriorities.GetValueOrDefault(g.Key),
                    Items = g.OrderBy(o => o.Priority).ThenBy(o => o.ItemName, StringComparer.Ordinal)
                        .ThenBy(o => o.Id).Select(i => Item(i, now)).ToList()
                })
                .OrderBy(o => o.Order).ThenBy(o => o.Name, StringComparer.Ordinal).ToList();

            var open = visible.Count(w => w.StateId == (byte)CaseChecklistItemState.Shown);
            var done = visible.Count(w => w.StateId == (byte)CaseChecklistItemState.Done);

            return new CaseChecklistDto
            {
                CaseId = caseId,
                Checklists = groups.Select(g => new CaseChecklistGroupDto
                {
                    CaseWorkflowChecklistGuid = g.Guid, Name = g.Name, Items = g.Items
                }).ToList(),
                Required = open + done,
                Done = done,
                Open = open,
                Breached = visible.Count(w => w.StateId == (byte)CaseChecklistItemState.Shown
                                              && w.DueDate.HasValue && now > w.DueDate.Value),
                Complete = open == 0
            };
        }

        internal static CaseChecklistItemDto Item(CaseChecklistItem row, DateTime now)
        {
            var done = row.StateId == (byte)CaseChecklistItemState.Done;
            var sla = CaseChecklistSlaState.None;
            int? minutes = null;

            if (row.DueDate.HasValue)
            {
                if (done)
                {
                    sla = row.CompletedDate.HasValue && row.CompletedDate.Value > row.DueDate.Value
                        ? CaseChecklistSlaState.CompletedLate
                        : CaseChecklistSlaState.CompletedOnTime;
                }
                else
                {
                    sla = now > row.DueDate.Value ? CaseChecklistSlaState.Breached : CaseChecklistSlaState.OnTrack;
                    minutes = (int)Math.Floor((row.DueDate.Value - now).TotalMinutes);
                }
            }

            return new CaseChecklistItemDto
            {
                Id = row.Id,
                CaseWorkflowChecklistItemGuid = row.CaseWorkflowChecklistItemGuid,
                Name = row.ItemName,
                Priority = row.Priority,
                StateId = row.StateId,
                FirstShownDate = CaseSearchMapper.Utc(row.FirstShownDate),
                ShownDate = CaseSearchMapper.Utc(row.ShownDate),
                DueDate = CaseSearchMapper.Utc(row.DueDate),
                SlaBreachedDate = CaseSearchMapper.Utc(row.SlaBreachedDate),
                CompletedDate = CaseSearchMapper.Utc(row.CompletedDate),
                CompletedUser = row.CompletedUser,
                SlaStateId = (int)sla,
                MinutesRemaining = minutes
            };
        }

        internal static CaseChecklistOverrideDto OverrideView(int caseId, CaseChecklistOverride grant)
        {
            var names = new List<string>();

            if (!string.IsNullOrWhiteSpace(grant.OpenItemsJson))
            {
                using var document = JsonDocument.Parse(grant.OpenItemsJson);

                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("Name", out var name) && name.GetString() != null)
                    {
                        names.Add(name.GetString()!);
                    }
                }
            }

            return new CaseChecklistOverrideDto
            {
                Id = grant.Id,
                CaseId = caseId,
                Reason = grant.Reason,
                GrantedUser = grant.GrantedUser,
                GrantedDate = CaseSearchMapper.Utc(grant.GrantedDate),
                OpenItems = names
            };
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            throw new ForbiddenException(strings[CaseChecklistResources.PermissionDenied], permissions);
        }
    }
}
