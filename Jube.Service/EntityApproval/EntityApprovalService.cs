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

namespace Jube.Service.EntityApproval
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Poco;
    using Data.Query;
    using Data.Query.GetEntityApprovalSubjectByEntityIdQuery;
    using Data.Query.GetEntityApprovalSubjectsByKindQuery;
    using Data.Query.GetEntityApprovalSubjectsByKindAndParentQuery;
    using Data.Query.GetEntityApprovalSubjectsByModelQuery;
    using Data.Query.Models;
    using Data.Repository;
    using Exceptions.EntityApproval;
    using log4net;
    using Parser.Dependency;
    using Security;

    public sealed class EntityApprovalService
    {
        public const int AllowApprovalPermission = 44;
        public const int ViewPendingApprovalsPermission = 45;
        private const int MaxNoteLength = 1000;

        private static readonly int[] approvePermissions = [AllowApprovalPermission];
        private static readonly int[] viewPermissions = [ViewPendingApprovalsPermission, AllowApprovalPermission];

        private readonly DbContext dbContext;
        private readonly int approvalsRequired;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityApprovalRepository repository;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public static async Task<EntityApprovalService> CreateAsync(DbContext dbContext, string userName,
            ILog log, int approvalsRequired, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException("Sign in to act on approvals.");
            }

            var tenantRegistryId = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false);

            if (!tenantRegistryId.HasValue)
            {
                throw new NotAuthenticatedException("This user does not belong to a tenant.");
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityApprovalService(dbContext, userName, tenantRegistryId.Value, permissionValidation, log,
                approvalsRequired);
        }

        public EntityApprovalService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, int approvalsRequired)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.approvalsRequired = approvalsRequired < 1 ? ApprovalsRequiredResolver.Default : approvalsRequired;
            repository = new EntityApprovalRepository(dbContext, tenantRegistryId);
        }

        public async Task<EntityApproval> ApproveAsync(EntityApprovalKind kind, int entityId, int version,
            string? note = null, CancellationToken token = default)
        {
            RequirePermission(approvePermissions);
            RefuseLongNote(note);

            var subject = await LoadCurrentAsync(kind, entityId, version, token).ConfigureAwait(false);

            RefuseOwnChange(subject, "approve");

            var rows = await repository.GetRowsByKindAndEntityIdsAsync(kind, [entityId], token)
                .ConfigureAwait(false);
            var entityRows = rows.Where(r => r.EntityId == entityId).ToList();

            if (EntityApprovalResolver.IsRejected(entityRows, version))
            {
                throw Refused("This version was rejected.",
                    $"{subject.Name} version {version} was rejected; an edit opens a new approval cycle.");
            }

            if (entityRows.Any(r => r.EntityVersion == version && r.State == EntityApprovalState.Approved
                                                               && string.Equals(r.CreatedUser, userName,
                                                                   StringComparison.OrdinalIgnoreCase)))
            {
                throw Refused("You have already approved this version.",
                    $"{subject.Name} version {version} already carries your approval.");
            }

            if (EntityApprovalResolver.EffectiveVersion(entityRows, version, approvalsRequired) == version)
            {
                throw Refused("This version is already in effect.", $"{subject.Name} version {version} is approved.");
            }

            await CheckPreflightAsync(kind, subject, approvalsRequired, token).ConfigureAwait(false);

            var approval = await RecordAsync(kind, entityId, version, EntityApprovalState.Approved, note, token)
                .ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"EntityApproval.Approve: kind={kind} id={entityId} version={version} user={userName}");
            }

            return approval;
        }

        public async Task<EntityApproval> RejectAsync(EntityApprovalKind kind, int entityId, int version,
            string? note = null, CancellationToken token = default)
        {
            RequirePermission(approvePermissions);
            RefuseLongNote(note);

            var subject = await LoadCurrentAsync(kind, entityId, version, token).ConfigureAwait(false);
            RefuseOwnChange(subject, "reject");

            var rows = await repository.GetRowsByKindAndEntityIdsAsync(kind, [entityId], token)
                .ConfigureAwait(false);
            var entityRows = rows.Where(r => r.EntityId == entityId).ToList();

            if (EntityApprovalResolver.IsRejected(entityRows, version))
            {
                throw Refused("This version is already rejected.",
                    $"{subject.Name} version {version} was rejected; an edit opens a new approval cycle.");
            }

            if (EntityApprovalResolver.EffectiveVersion(entityRows, version, approvalsRequired) == version)
            {
                throw Refused("This version is in effect, so it cannot be rejected. The live rule is left alone.",
                    $"{subject.Name} version {version} is approved and live; a rejection would withdraw it.");
            }

            var rejection = await RecordAsync(kind, entityId, version, EntityApprovalState.Rejected, note, token)
                .ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"EntityApproval.Reject: kind={kind} id={entityId} version={version} user={userName}");
            }

            return rejection;
        }

        private Task<EntityApproval> RecordAsync(EntityApprovalKind kind, int entityId, int version,
            EntityApprovalState state, string? note, CancellationToken token)
        {
            return repository.InsertAsync(new EntityApproval
            {
                EntityApprovalKindId = (int)kind,
                EntityId = entityId,
                EntityVersion = version,
                StateId = (int)state,
                Note = note,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            }, token);
        }

        private static void RefuseLongNote(string? note)
        {
            if (note is { Length: > MaxNoteLength })
            {
                throw Refused("The note is too long.", $"A note can be at most {MaxNoteLength} characters.");
            }
        }

        private void RefuseOwnChange(EntityApprovalSubject subject, string verb)
        {
            if (!permissionValidation.Landlord &&
                string.Equals(subject.MakerUser, userName, StringComparison.OrdinalIgnoreCase))
            {
                throw Refused($"A maker cannot {verb} their own change.", $"{subject.Name} was changed by you.");
            }
        }

        public async Task<EntityApprovalStatus> StatusAsync(EntityApprovalKind kind, int entityId,
            CancellationToken token = default)
        {
            RequirePermission(viewPermissions);

            var subject = await new GetEntityApprovalSubjectByEntityIdQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(kind, entityId, token).ConfigureAwait(false);

            if (subject is null)
            {
                throw new KeyNotFoundException($"{kind} {entityId} was not found in this tenant.");
            }

            var rows = await repository.GetRowsByKindAndEntityIdsAsync(kind, [entityId], token)
                .ConfigureAwait(false);

            return StatusOf(subject, rows.Where(r => r.EntityId == entityId).ToList());
        }

        public async Task<List<EntityApprovalStatus>> ValueStatusAsync(EntityApprovalKind parentKind, int parentId,
            CancellationToken token = default)
        {
            RequirePermission(viewPermissions);

            var valueKind = ValueKindOf(parentKind);

            var parent = await new GetEntityApprovalSubjectByEntityIdQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(parentKind, parentId, token).ConfigureAwait(false);

            if (parent is null)
            {
                throw new KeyNotFoundException($"{parentKind} {parentId} was not found in this tenant.");
            }

            var subjects = await new GetEntityApprovalSubjectsByKindAndParentQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(valueKind, parentId, token).ConfigureAwait(false);
            var rows = await repository.GetRowsByKindAndEntityIdsAsync(valueKind,
                subjects.Select(s => s.EntityId).Distinct().ToList(), token).ConfigureAwait(false);

            return subjects
                .Select(subject => StatusOf(subject, rows.Where(r => r.EntityId == subject.EntityId).ToList()))
                .OrderByDescending(status => status.Pending)
                .ThenBy(status => status.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private EntityApprovalStatus StatusOf(EntityApprovalSubject subject, List<EntityApprovalRow> entityRows)
        {
            var kind = subject.Kind;
            var entityId = subject.EntityId;

            var pending = EntityApprovalResolver.IsPending(entityRows, subject.Version, approvalsRequired);
            var rejected = EntityApprovalResolver.IsRejected(entityRows, subject.Version);
            var approvers = entityRows
                .Where(r => r.EntityVersion == subject.Version && r.State == EntityApprovalState.Approved)
                .Select(r => r.CreatedUser ?? string.Empty)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var alreadyApprovedByMe = approvers.Contains(userName, StringComparer.OrdinalIgnoreCase);
            var isMaker = string.Equals(subject.MakerUser, userName, StringComparison.OrdinalIgnoreCase);

            return new EntityApprovalStatus(
                kind,
                entityId,
                subject.Name,
                subject.Version,
                EntityApprovalResolver.EffectiveVersion(entityRows, subject.Version, approvalsRequired),
                subject.Deleted,
                pending,
                rejected,
                subject.MakerUser,
                approvers.Count,
                approvalsRequired,
                pending && !rejected && (!isMaker || permissionValidation.Landlord) && !alreadyApprovedByMe
                && permissionValidation.Validate(approvePermissions));
        }

        public Task<List<EntityApproval>> HistoryAsync(EntityApprovalKind kind, int entityId,
            CancellationToken token = default)
        {
            RequirePermission(viewPermissions);

            return repository.GetHistoryAsync(kind, entityId, token);
        }

        public async Task<List<EntityApprovalSubject>> PendingForModelAsync(int modelId,
            CancellationToken token = default)
        {
            RequirePermission(viewPermissions);

            var subjects = await new GetEntityApprovalSubjectsByModelQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(modelId, token).ConfigureAwait(false);

            return EntityApprovalPipelineOrder.Order(await PendingOfAsync(subjects, token).ConfigureAwait(false));
        }

        public async Task<List<EntityApprovalSubject>> PendingForKindAsync(EntityApprovalKind kind,
            CancellationToken token = default)
        {
            RequirePermission(viewPermissions);

            var subjects = await new GetEntityApprovalSubjectsByKindQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(kind, token).ConfigureAwait(false);

            return EntityApprovalPipelineOrder.Order(await PendingOfAsync(subjects, token).ConfigureAwait(false));
        }

        public async Task<EntityApprovalChanges> ChangesAsync(EntityApprovalKind kind, int entityId, int version,
            CancellationToken token = default)
        {
            RequirePermission(viewPermissions);

            await LoadCurrentAsync(kind, entityId, version, token).ConfigureAwait(false);

            var (current, earlier) = await EntityApprovalChangeSources.LoadAsync(dbContext, kind, entityId, version,
                token).ConfigureAwait(false);

            if (current is null)
            {
                throw new KeyNotFoundException($"{kind} {entityId} was not found in this tenant.");
            }

            if (earlier is null)
            {
                return new EntityApprovalChanges(false, []);
            }

            return new EntityApprovalChanges(true,
                VersionDiff.Compare(earlier, current, EntityApprovalChangeSources.ReviewIgnoredFields));
        }

        public async Task<BulkApprovalResult> ApproveValuesAsync(EntityApprovalKind parentKind, int parentId,
            CancellationToken token = default)
        {
            RequirePermission(approvePermissions);

            var valueKind = ValueKindOf(parentKind);

            var children = await new GetEntityApprovalSubjectsByKindAndParentQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(valueKind, parentId, token).ConfigureAwait(false);
            var pending = await PendingOfAsync(children, token).ConfigureAwait(false);

            var approved = 0;
            var refusals = new List<string>();

            await dbContext.InTransactionAsync(async () =>
            {
                approved = 0;
                refusals.Clear();

                foreach (var value in pending)
                {
                    try
                    {
                        await ApproveAsync(valueKind, value.EntityId, value.Version, null, token)
                            .ConfigureAwait(false);
                        approved++;
                    }
                    catch (ApprovalRefusedException ex)
                    {
                        refusals.Add($"{value.Name}: {string.Join(" ", ex.Reasons)}");
                    }
                }
            }, token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info($"EntityApproval.ApproveValues: parent={parentKind} id={parentId} approved={approved} " +
                         $"refused={refusals.Count} user={userName}");
            }

            return new BulkApprovalResult(approved, refusals);
        }

        private async Task<List<EntityApprovalSubject>> PendingOfAsync(IEnumerable<EntityApprovalSubject> subjects,
            CancellationToken token)
        {
            var pending = new List<EntityApprovalSubject>();

            foreach (var group in subjects.GroupBy(s => s.Kind))
            {
                var ids = group.Select(s => s.EntityId).Distinct().ToList();
                var rows = await repository.GetRowsByKindAndEntityIdsAsync(group.Key, ids, token)
                    .ConfigureAwait(false);

                foreach (var subject in group)
                {
                    var entityRows = rows.Where(r => r.EntityId == subject.EntityId).ToList();

                    if (EntityApprovalResolver.IsPending(entityRows, subject.Version, approvalsRequired))
                    {
                        pending.Add(subject);
                    }
                }
            }

            return pending;
        }

        private void RequirePermission(int[] specifications)
        {
            if (permissionValidation.Validate(specifications))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn(
                    $"EntityApproval: permission denied user={userName} specs=[{string.Join(",", specifications)}]");
            }

            throw new ForbiddenException("Approval requires permission.", specifications);
        }

        private async Task<EntityApprovalSubject> LoadCurrentAsync(EntityApprovalKind kind, int entityId,
            int version, CancellationToken token)
        {
            var subject = await new GetEntityApprovalSubjectByEntityIdQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(kind, entityId, token).ConfigureAwait(false);

            if (subject is null)
            {
                throw new KeyNotFoundException($"{kind} {entityId} was not found in this tenant.");
            }

            if (version != subject.Version)
            {
                throw Refused("This version is no longer current.",
                    $"{subject.Name} is at version {subject.Version}; review that version rather than version {version}.");
            }

            return subject;
        }

        private async Task CheckPreflightAsync(EntityApprovalKind kind, EntityApprovalSubject subject,
            int approvals, CancellationToken token)
        {
            var graphKind = EntityApprovalGraphKind.Of(kind);

            if (!graphKind.HasValue)
            {
                return;
            }

            var graph = await ModelDependencyGraphBuilder.BuildAsync(dbContext, tenantRegistryId, userName,
                subject.ModelId, log, token, approvals).ConfigureAwait(false);

            var candidate = new ModelEntity(graphKind.Value, subject.EntityId, subject.Name, subject.Active, false);

            if (subject.Deleted)
            {
                graph = graph.WithEntity(candidate);
            }
            else
            {
                candidate = graph.Entities.FirstOrDefault(e => e.Kind == graphKind.Value && e.Id == subject.EntityId)
                            ?? candidate;
            }

            var result = ApprovalPreflight.Evaluate(graph, candidate, subject.Deleted);

            if (result.Allowed)
            {
                return;
            }

            var reasons = result.Errors
                .Concat(result.Prerequisites.Select(p =>
                    $"Approve {p.Kind} {p.Name} first: it is not yet approved and {subject.Name} depends on it."))
                .ToList();

            throw new ApprovalRefusedException("The approval would leave the model inconsistent.", reasons);
        }

        private static EntityApprovalKind ValueKindOf(EntityApprovalKind parentKind)
        {
            return parentKind switch
            {
                EntityApprovalKind.EntityAnalysisModelList => EntityApprovalKind.EntityAnalysisModelListValue,
                EntityApprovalKind.EntityAnalysisModelDictionary => EntityApprovalKind.EntityAnalysisModelDictionaryKvp,
                _ => throw new ApprovalRefusedException("Only lists and dictionaries have values to approve.", [])
            };
        }

        private static ApprovalRefusedException Refused(string message, string reason)
        {
            return new ApprovalRefusedException(message, [reason]);
        }
    }
}