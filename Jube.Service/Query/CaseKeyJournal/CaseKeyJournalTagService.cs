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
using Jube.Data.Repository;
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
using Newtonsoft.Json.Linq;
using SearchNotFound = Jube.Service.Exceptions.Query.CaseSearch.NotFoundException;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed class CaseKeyJournalTagService
    {
        public const int MaximumTransactions = 500;
        public const int MaximumTags = 20;
        private const int MaximumNameLength = 256;

        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseSearchService search;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseKeyJournalTagService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, CaseSearchService search)
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
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseKeyJournalTagService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseKeyJournalTagService> CreateAsync(DbContext dbContext, string? userName,
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

            return new CaseKeyJournalTagService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, search);
        }

        [Description("Lists the Tags the Case Workflow's model allows, which are the only names that can be added " +
                     "to a journal transaction. Read-only.")]
        [ServiceOperation("CaseKeyJournalTagOptionsGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalTagOptionsDto> GetOptionsAsync(
            [Description("Guid of the Case Workflow.")]
            Guid caseWorkflowGuid, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TagOptions", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted();

                var modelId = await ResolveModelAsync(caseWorkflowGuid, token).ConfigureAwait(false);
                var names = await ActiveTagNamesAsync(modelId, token).ConfigureAwait(false);

                op.Rows(names.Count);

                return new CaseKeyJournalTagOptionsDto { Tag = [.. names.OrderBy(n => n, StringComparer.Ordinal)] };
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
                log.Error($"CaseKeyJournal.TagOptions: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Adds Tags to and removes Tags from up to 500 journal transactions in one step, so a whole " +
                     "selection can be labelled together. Every name to add must be an active Tag of the " +
                     "workflow's model. Returns the Tag sets before and after so the step can be undone with " +
                     "CaseKeyJournalTagRestore. SIDE EFFECTS: rewrites the Tags stored on each archived " +
                     "transaction and records who tagged it. Transactions outside the workflow's model, " +
                     "and those whose case is locked to another user, are left alone and counted.")]
        [ServiceOperation("CaseKeyJournalTagUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseKeyJournalTagDto> UpdateAsync(CaseKeyJournalTagRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TagUpdate", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var add = Names(request.Add);
                var remove = Names(request.Remove);

                Validate(request.Transactions.Length, request.Transactions.Distinct().Count(), add, remove);

                if (add.Count == 0 && remove.Count == 0)
                {
                    throw Invalid(nameof(request.Add), CaseKeyJournalResources.TagChangeRequired,
                        "TagChangeRequired");
                }

                var modelId = await ResolveModelAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);

                if (add.Count > 0)
                {
                    var allowed = await ActiveTagNamesAsync(modelId, token).ConfigureAwait(false);

                    if (add.Any(name => !allowed.Contains(name)))
                    {
                        throw Invalid(nameof(request.Add), CaseKeyJournalResources.TagUnknown, "TagUnknown");
                    }
                }

                var result = await ApplyAsync(modelId, request.Transactions,
                    (_, current) => current.Where(name => !remove.Contains(name))
                        .Concat(add.Where(name => !current.Contains(name))).ToArray(), token)
                    .ConfigureAwait(false);

                op.Rows(result.Current.Length);

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
                log.Error($"CaseKeyJournal.TagUpdate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Puts journal transactions back to the Tag sets they had, which undoes a tagging. Send the " +
                     "Previous sets that CaseKeyJournalTagUpdate returned. SIDE EFFECTS: rewrites the Tags stored " +
                     "on each archived transaction and records who changed it. " +
                     "Transactions outside the workflow's model, and those whose case is locked to another " +
                     "user, are left alone and counted.")]
        [ServiceOperation("CaseKeyJournalTagRestore", OperationKind.Write, Idempotent = true)]
        public async Task<CaseKeyJournalTagDto> RestoreAsync(CaseKeyJournalTagRestoreRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "TagRestore", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var sets = new Dictionary<Guid, List<string>>();

                foreach (var set in request.Sets)
                {
                    sets.TryAdd(set.Transaction, Names(set.Tag));
                }

                Validate(request.Sets.Length, sets.Count, [], []);

                if (sets.Values.Any(names => names.Count > MaximumTags))
                {
                    throw Invalid(nameof(request.Sets), CaseKeyJournalResources.TagLimitInvalid, "TagLimitInvalid");
                }

                var modelId = await ResolveModelAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);
                var result = await ApplyAsync(modelId, [.. sets.Keys], (guid, _) => [.. sets[guid]], token)
                    .ConfigureAwait(false);

                op.Rows(result.Current.Length);

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
                log.Error($"CaseKeyJournal.TagRestore: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<CaseKeyJournalTagDto> ApplyAsync(int modelId, Guid[] transactions,
            Func<Guid, List<string>, string[]> change, CancellationToken token)
        {
            var distinct = transactions.Distinct().ToList();

            var found = await dbContext.Archive
                .Where(a => a.EntityAnalysisModelId == modelId
                            && distinct.Contains(a.EntityAnalysisModelInstanceEntryGuid))
                .Select(a => new { a.EntityAnalysisModelInstanceEntryGuid, a.Json })
                .ToListAsync(token).ConfigureAwait(false);

            var lockedByAnother = (await dbContext.Case
                .Where(c => distinct.Contains(c.EntityAnalysisModelInstanceEntryGuid) && c.Locked == 1
                                                                                      && c.LockedUser != null
                                                                                      && c.LockedUser != userName)
                .Select(c => c.EntityAnalysisModelInstanceEntryGuid)
                .ToListAsync(token).ConfigureAwait(false)).ToHashSet();

            var archives = found.Where(a => !lockedByAnother.Contains(a.EntityAnalysisModelInstanceEntryGuid))
                .ToList();

            var archiveRepository = new ArchiveRepository(dbContext);
            var caseRepository = new CaseRepository(dbContext, userName);
            var archiveTagRepository = new ArchiveTagRepository(dbContext, userName);

            var previous = new List<CaseKeyJournalTagSetDto>();
            var current = new List<CaseKeyJournalTagSetDto>();

            foreach (var archive in archives)
            {
                token.ThrowIfCancellationRequested();

                var before = (JObject.Parse(archive.Json)["tag"] as JArray)?.Select(tag => tag.ToString()).ToList()
                             ?? [];
                var after = change(archive.EntityAnalysisModelInstanceEntryGuid, before).Distinct().ToArray();

                previous.Add(new CaseKeyJournalTagSetDto
                {
                    Transaction = archive.EntityAnalysisModelInstanceEntryGuid, Tag = [.. before]
                });
                current.Add(new CaseKeyJournalTagSetDto
                {
                    Transaction = archive.EntityAnalysisModelInstanceEntryGuid, Tag = after
                });

                if (before.SequenceEqual(after))
                {
                    continue;
                }

                var updated = await archiveRepository.UpdateTagsByEntityAnalysisModelInstanceEntryGuidAsync(
                    archive.EntityAnalysisModelInstanceEntryGuid, after, token).ConfigureAwait(false);

                await caseRepository.UpdateArchiveJsonAsync(archive.EntityAnalysisModelInstanceEntryGuid,
                    updated.Json, token).ConfigureAwait(false);

                await archiveTagRepository.MergeTagsAsync(archive.EntityAnalysisModelInstanceEntryGuid, after, token)
                    .ConfigureAwait(false);
            }

            return new CaseKeyJournalTagDto
            {
                Previous = [.. previous],
                Current = [.. current],
                Skipped = distinct.Count - found.Count,
                Locked = found.Count - archives.Count
            };
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

            var modelId = await dbContext.CaseWorkflow.Where(w => w.Guid == caseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            return modelId ?? throw new NotFoundException(strings[CaseKeyJournalResources.WorkflowNotFound]);
        }

        private async Task<HashSet<string>> ActiveTagNamesAsync(int modelId, CancellationToken token)
        {
            var names = await dbContext.EntityAnalysisModelTag
                .Where(t => t.EntityAnalysisModelId == modelId && t.Active == 1
                                                               && (t.Deleted == null || t.Deleted == 0))
                .Select(t => t.Name).ToListAsync(token).ConfigureAwait(false);

            return names.Where(name => !string.IsNullOrEmpty(name)).Select(name => name!)
                .ToHashSet(StringComparer.Ordinal);
        }

        private List<string> Names(string[]? names)
        {
            var cleaned = (names ?? []).Select(name => name.Trim()).ToList();

            if (cleaned.Any(name => name.Length is 0 or > MaximumNameLength))
            {
                throw Invalid("Tag", CaseKeyJournalResources.TagNameInvalid, "TagNameInvalid");
            }

            return cleaned.Distinct(StringComparer.Ordinal).ToList();
        }

        private void Validate(int requested, int distinct, List<string> add, List<string> remove)
        {
            if (requested is < 1 or > MaximumTransactions || distinct != requested)
            {
                throw Invalid("Transactions", CaseKeyJournalResources.TagTransactionsInvalid,
                    "TagTransactionsInvalid");
            }

            if (add.Count > MaximumTags || remove.Count > MaximumTags)
            {
                throw Invalid("Tag", CaseKeyJournalResources.TagLimitInvalid, "TagLimitInvalid");
            }
        }

        private DtoValidationException Invalid(string property, string resource, string code)
        {
            return new DtoValidationException(new FluentValidation.Results.ValidationResult(
                [new FluentValidation.Results.ValidationFailure(property, strings[resource]) { ErrorCode = code }]));
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
