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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Observability;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using SearchNotFound = Jube.Service.Exceptions.Query.CaseSearch.NotFoundException;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed class CaseKeyJournalTagJobService
    {
        public const int MaximumMatches = 100000;
        public const int ListLength = 10;

        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string reportConnectionString;
        private readonly CaseSearchService search;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseKeyJournalTagJobService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, CaseSearchService search, string reportConnectionString)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.search = search;
            this.reportConnectionString = reportConnectionString;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseKeyJournalTagJobService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseKeyJournalTagJobService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseKeyJournalResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseKeyJournalResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseKeyJournalResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            var search = await CaseSearchService.CreateAsync(dbContext, userName, log, stringLocalizerFactory,
                serviceChangeBus, dynamicEnvironment, auditLog, token).ConfigureAwait(false);

            return new CaseKeyJournalTagJobService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, search,
                dynamicEnvironment.AppSettings("ReportConnectionString") ?? dbContext.Connection.ConnectionString);
        }

        [Description("Counts how many transactions a journal view matches, the view being a case key (or any " +
                     "identifier pivoted to), a window, optional merged keys and the activations and elevation " +
                     "filters, so that a bulk tagging of all of them can be confirmed first. One job tags up to " +
                     "100,000 transactions. Read-only.")]
        [ServiceOperation("CaseKeyJournalTagJobPreview", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalTagJobPreviewDto> PreviewAsync(
            CaseKeyJournalTagJobPreviewRequestDto? request, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TagJobPreview", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var filter = await FilterAsync(request, token).ConfigureAwait(false);
                var modelId = await ResolveModelAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);
                var found = await MatchesAsync(request.CaseWorkflowGuid, modelId, filter, token).ConfigureAwait(false);

                op.Rows(found.Count);

                return new CaseKeyJournalTagJobPreviewDto
                {
                    Matching = Math.Min(found.Count, MaximumMatches),
                    Limit = MaximumMatches,
                    TooMany = found.Count > MaximumMatches
                };
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Npgsql.PostgresException ex) when (ArchiveFilterFailure.Is(ex, request?.Filter))
            {
                op.Outcome("invalid");
                throw ArchiveFilterFailure.Unreadable(strings);
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.TagJobPreview: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Starts a background job that adds Tags to and removes Tags from every transaction a journal " +
                     "view matches, which is more than the journal shows at once. Up to 100,000 transactions. The " +
                     "job runs on the jobs server as the caller, so the same checks apply as for tagging a " +
                     "selection: each name to add must be an active Tag of the workflow's model, and transactions " +
                     "whose case is locked to another user are left alone. Follow it with " +
                     "CaseKeyJournalTagJobGet. SIDE EFFECTS: queues the job, which rewrites the Tags stored on the " +
                     "matching archived transactions and records who tagged them.")]
        [ServiceOperation("CaseKeyJournalTagJobSubmit", OperationKind.Write, Idempotent = false)]
        public async Task<CaseKeyJournalTagJobDto> SubmitAsync(CaseKeyJournalTagJobRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TagJobSubmit", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var filter = await FilterAsync(request, token).ConfigureAwait(false);
                var add = Names(request.Add);
                var remove = Names(request.Remove);

                if (add.Count == 0 && remove.Count == 0)
                {
                    throw Invalid(nameof(request.Add), CaseKeyJournalResources.TagChangeRequired,
                        "TagChangeRequired");
                }

                if (add.Count > CaseKeyJournalTagService.MaximumTags || remove.Count > CaseKeyJournalTagService.MaximumTags)
                {
                    throw Invalid("Tag", CaseKeyJournalResources.TagLimitInvalid, "TagLimitInvalid");
                }

                var modelId = await ResolveModelAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);

                if (add.Count > 0)
                {
                    var allowed = (await dbContext.EntityAnalysisModelTag
                        .Where(t => t.EntityAnalysisModelId == modelId && t.Active == 1
                                                                       && (t.Deleted == null || t.Deleted == 0))
                        .Select(t => t.Name).ToListAsync(token).ConfigureAwait(false)).ToHashSet(StringComparer.Ordinal);

                    if (add.Any(name => !allowed.Contains(name)))
                    {
                        throw Invalid(nameof(request.Add), CaseKeyJournalResources.TagUnknown, "TagUnknown");
                    }
                }

                var found = await MatchesAsync(request.CaseWorkflowGuid, modelId, filter, token).ConfigureAwait(false);

                if (found.Count == 0)
                {
                    throw Invalid(nameof(request.Key), CaseKeyJournalResources.TagJobNoMatches, "TagJobNoMatches");
                }

                if (found.Count > MaximumMatches)
                {
                    throw Invalid(nameof(request.Key), CaseKeyJournalResources.TagJobTooManyMatches,
                        "TagJobTooManyMatches");
                }

                var job = Create(request.CaseWorkflowGuid, CaseKeyJournalTagJobKind.Tag, filter);
                job.AddJson = JsonConvert.SerializeObject(add);
                job.RemoveJson = JsonConvert.SerializeObject(remove);
                job.Total = found.Count;
                await new CaseKeyJournalTagJobRepository(dbContext).InsertAsync(job, token).ConfigureAwait(false);

                op.Rows(found.Count);

                return ToDto(job, false);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Npgsql.PostgresException ex) when (ArchiveFilterFailure.Is(ex, request?.Filter))
            {
                op.Outcome("invalid");
                throw ArchiveFilterFailure.Unreadable(strings);
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.TagJobSubmit: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a bulk tagging job of the caller, with how far it has got: its state, how many " +
                     "transactions matched, how many have been handled and changed, how many were left alone and " +
                     "whether it can be undone. Read-only.")]
        [ServiceOperation("CaseKeyJournalTagJobGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalTagJobDto> GetAsync(
            [Description("Guid of the job, as returned when it was started.")] Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TagJobGet", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted();

                var repository = new CaseKeyJournalTagJobRepository(dbContext);
                var job = await OwnAsync(repository, guid, token).ConfigureAwait(false);

                return ToDto(job, await CanUndoAsync(repository, job, token).ConfigureAwait(false));
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
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
                log.Error($"CaseKeyJournal.TagJobGet: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the caller's most recent bulk tagging jobs on a Case Workflow, newest first, so that " +
                     "one in progress can be found again. Read-only.")]
        [ServiceOperation("CaseKeyJournalTagJobList", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseKeyJournalTagJobDto>> ListAsync(
            [Description("Guid of the Case Workflow.")] Guid caseWorkflowGuid, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TagJobList", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted();

                await ResolveModelAsync(caseWorkflowGuid, token).ConfigureAwait(false);

                var repository = new CaseKeyJournalTagJobRepository(dbContext);
                var jobs = await repository.ListAsync(userName, caseWorkflowGuid, ListLength, token)
                    .ConfigureAwait(false);
                var result = new List<CaseKeyJournalTagJobDto>();

                foreach (var job in jobs)
                {
                    result.Add(ToDto(job, await CanUndoAsync(repository, job, token).ConfigureAwait(false)));
                }

                op.Rows(result.Count);

                return result;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
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
                log.Error($"CaseKeyJournal.TagJobList: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Asks a queued or running bulk tagging job of the caller to stop. A queued job is cancelled " +
                     "at once; a running one stops at the end of the batch in hand, leaving what it has already " +
                     "tagged in place (it can still be undone). SIDE EFFECTS: marks the job for cancellation.")]
        [ServiceOperation("CaseKeyJournalTagJobCancel", OperationKind.Write, Idempotent = true)]
        public async Task<CaseKeyJournalTagJobDto> CancelAsync(
            [Description("Guid of the job.")] Guid guid, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TagJobCancel", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted();

                var repository = new CaseKeyJournalTagJobRepository(dbContext);

                await OwnAsync(repository, guid, token).ConfigureAwait(false);
                await repository.RequestCancelAsync(guid, userName, token).ConfigureAwait(false);

                var job = await OwnAsync(repository, guid, token).ConfigureAwait(false);

                return ToDto(job, await CanUndoAsync(repository, job, token).ConfigureAwait(false));
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
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
                log.Error($"CaseKeyJournal.TagJobCancel: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Starts a background job that puts every transaction a finished bulk tagging changed back to " +
                     "the Tags it had before. Only the caller's own job that is Done, changed something and has " +
                     "not been undone can be undone. SIDE EFFECTS: queues the undo job, which rewrites the Tags " +
                     "stored on those archived transactions.")]
        [ServiceOperation("CaseKeyJournalTagJobUndo", OperationKind.Write, Idempotent = false)]
        public async Task<CaseKeyJournalTagJobDto> UndoAsync(
            [Description("Guid of the finished job to undo.")] Guid guid, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TagJobUndo", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted();

                var repository = new CaseKeyJournalTagJobRepository(dbContext);
                var original = await OwnAsync(repository, guid, token).ConfigureAwait(false);

                if (!await CanUndoAsync(repository, original, token).ConfigureAwait(false))
                {
                    throw Invalid(nameof(guid), CaseKeyJournalResources.TagJobNotUndoable, "TagJobNotUndoable");
                }

                var job = new CaseKeyJournalTagJob
                {
                    Guid = Guid.NewGuid(),
                    CaseWorkflowGuid = original.CaseWorkflowGuid,
                    RequestedUser = userName,
                    KindId = (byte)CaseKeyJournalTagJobKind.Undo,
                    StateId = (byte)CaseKeyJournalTagJobState.Queued,
                    CaseKey = original.CaseKey,
                    KeyValue = original.KeyValue,
                    FromDate = original.FromDate,
                    ToDate = original.ToDate,
                    UndoOfGuid = original.Guid,
                    Total = original.Changed,
                    CreatedDate = DateTime.UtcNow
                };

                await repository.InsertAsync(job, token).ConfigureAwait(false);

                op.Rows(original.Changed);

                return ToDto(job, false);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
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
                log.Error($"CaseKeyJournal.TagJobUndo: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<CaseKeyJournalTagJob> OwnAsync(CaseKeyJournalTagJobRepository repository, Guid guid,
            CancellationToken token)
        {
            var job = await repository.GetAsync(guid, token).ConfigureAwait(false);

            if (job is null || !string.Equals(job.RequestedUser, userName, StringComparison.Ordinal))
            {
                throw new NotFoundException(strings[CaseKeyJournalResources.TagJobNotFound]);
            }

            return job;
        }

        private static async Task<bool> CanUndoAsync(CaseKeyJournalTagJobRepository repository,
            CaseKeyJournalTagJob job, CancellationToken token)
        {
            return job.KindId == (byte)CaseKeyJournalTagJobKind.Tag
                   && job.StateId == (byte)CaseKeyJournalTagJobState.Done && job.Changed > 0
                   && !await repository.HasLiveUndoAsync(job.Guid, token).ConfigureAwait(false);
        }

        private CaseKeyJournalTagJob Create(Guid workflow, CaseKeyJournalTagJobKind kind, JournalMatchFilter filter)
        {
            return new CaseKeyJournalTagJob
            {
                Guid = Guid.NewGuid(),
                CaseWorkflowGuid = workflow,
                RequestedUser = userName,
                KindId = (byte)kind,
                StateId = (byte)CaseKeyJournalTagJobState.Queued,
                CaseKey = filter.Key,
                KeyValue = filter.Value,
                FromDate = filter.From,
                ToDate = filter.To,
                ActivationsOnly = (byte)(filter.ActivationsOnly ? 1 : 0),
                ResponseElevation = filter.ResponseElevation,
                MergeJson = filter.Merge.Count == 0
                    ? null
                    : JsonConvert.SerializeObject(filter.Merge.Select(lane => new { key = lane.Key, value = lane.Value })),
                FilterJson = filter.Filter?.Text,
                CreatedDate = DateTime.UtcNow
            };
        }

        public static CaseKeyJournalTagJobDto ToDto(CaseKeyJournalTagJob job, bool canUndo)
        {
            static string[] Names(string? json) =>
                string.IsNullOrEmpty(json) ? [] : JsonConvert.DeserializeObject<string[]>(json) ?? [];

            return new CaseKeyJournalTagJobDto
            {
                Guid = job.Guid,
                CaseWorkflowGuid = job.CaseWorkflowGuid,
                Kind = ((CaseKeyJournalTagJobKind)job.KindId).ToString(),
                State = ((CaseKeyJournalTagJobState)job.StateId).ToString(),
                Key = job.CaseKey,
                Value = job.KeyValue,
                Add = Names(job.AddJson),
                Remove = Names(job.RemoveJson),
                UndoOf = job.UndoOfGuid,
                Total = job.Total,
                Processed = job.Processed,
                Changed = job.Changed,
                Skipped = job.Skipped,
                Locked = job.Locked,
                Error = job.Error,
                CanUndo = canUndo,
                CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(job.CreatedDate, DateTimeKind.Utc)),
                StartedDate = job.StartedDate.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(job.StartedDate.Value, DateTimeKind.Utc))
                    : null,
                FinishedDate = job.FinishedDate.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(job.FinishedDate.Value, DateTimeKind.Utc))
                    : null
            };
        }

        private async Task<List<Guid>> MatchesAsync(Guid workflow, int modelId, JournalMatchFilter filter,
            CancellationToken token)
        {
            var indexed = await new CaseKeyJournalKeyIndexRepository(dbContext).GetReadyKeyNamesAsync(token)
                .ConfigureAwait(false);

            return await JournalMatcher.GuidsAsync(reportConnectionString, log, modelId, userName, filter, indexed,
                MaximumMatches, token).ConfigureAwait(false);
        }

        private Task<JournalMatchFilter> FilterAsync(CaseKeyJournalTagJobPreviewRequestDto request,
            CancellationToken token)
        {
            return JournalViewFilter.BuildAsync(dbContext, userName, request, strings, token);
        }

        private List<string> Names(string[]? names)
        {
            var cleaned = (names ?? []).Select(name => name.Trim()).ToList();

            if (cleaned.Any(name => name.Length is 0 or > 256))
            {
                throw Invalid("Tag", CaseKeyJournalResources.TagNameInvalid, "TagNameInvalid");
            }

            return cleaned.Distinct(StringComparer.Ordinal).ToList();
        }

        private async Task<int> ResolveModelAsync(Guid caseWorkflowGuid, CancellationToken token)
        {
            try
            {
                await search.EverythingAsync(caseWorkflowGuid, token).ConfigureAwait(false);
            }
            catch (SearchNotFound)
            {
                throw new NotFoundException(strings[CaseKeyJournalResources.WorkflowNotFound]);
            }

            var modelId = await dbContext.CaseWorkflow
                .Where(w => w.Guid == caseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            return modelId ?? throw new NotFoundException(strings[CaseKeyJournalResources.WorkflowNotFound]);
        }

        private FluentValidation.Results.ValidationFailure Failure(string property, string resource, string code)
        {
            return new FluentValidation.Results.ValidationFailure(property, strings[resource]) { ErrorCode = code };
        }

        private DtoValidationException Invalid(string property, string resource, string code)
        {
            return new DtoValidationException(new FluentValidation.Results.ValidationResult(
                [Failure(property, resource, code)]));
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            throw new ForbiddenException(strings[CaseKeyJournalResources.PermissionDenied], permissions);
        }
    }
}
