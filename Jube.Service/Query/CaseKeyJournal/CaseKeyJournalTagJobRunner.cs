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


using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Reactivity.Interfaces;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed class CaseKeyJournalTagJobRunner(
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        IStringLocalizerFactory localizers,
        IServiceChangeBus serviceChangeBus)
    {
        public const int BatchSize = 500;
        private const int MaximumErrorLength = 500;

        public async Task<bool> RunNextAsync(CancellationToken token = default)
        {
            var connectionString = dynamicEnvironment.AppSettings("ConnectionString");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                connectionString, log);

            var repository = new CaseKeyJournalTagJobRepository(dbContext);
            var job = await repository.ClaimNextAsync(token).ConfigureAwait(false);

            if (job is null)
            {
                return false;
            }

            try
            {
                if (await repository.CancelRequestedAsync(job.Id, token).ConfigureAwait(false))
                {
                    await repository.FinishAsync(job.Id, CaseKeyJournalTagJobState.Cancelled, null, token)
                        .ConfigureAwait(false);

                    return true;
                }

                var service = await CaseKeyJournalTagService.CreateAsync(dbContext, job.RequestedUser, log,
                    localizers, serviceChangeBus, dynamicEnvironment, token).ConfigureAwait(false);

                var state = job.KindId == (byte)CaseKeyJournalTagJobKind.Undo
                    ? await UndoAsync(repository, service, job, token).ConfigureAwait(false)
                    : await TagAsync(dbContext, repository, service, job, token).ConfigureAwait(false);

                await repository.FinishAsync(job.Id, state, null, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.Error($"CaseKeyJournalTagJobRunner: job {job.Guid} failed as {ex}.");

                await repository.FinishAsync(job.Id, CaseKeyJournalTagJobState.Failed, Describe(ex), token)
                    .ConfigureAwait(false);
            }

            return true;
        }

        private static string Describe(Exception ex)
        {
            var message = ex is DtoValidationException { Result.Errors.Count: > 0 } validation
                ? validation.Result.Errors[0].ErrorMessage
                : ex.Message;

            return message.Length > MaximumErrorLength ? message[..MaximumErrorLength] : message;
        }

        private async Task<CaseKeyJournalTagJobState> TagAsync(DbContext dbContext,
            CaseKeyJournalTagJobRepository repository, CaseKeyJournalTagService service, CaseKeyJournalTagJob job,
            CancellationToken token)
        {
            var modelId = await dbContext.CaseWorkflow
                .Where(w => w.Guid == job.CaseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false)
                          ?? throw new NotFoundException("The workflow was not found.");

            IReadOnlyDictionary<string, ArchiveFilterField> fields = string.IsNullOrWhiteSpace(job.FilterJson)
                ? new Dictionary<string, ArchiveFilterField>()
                : await ArchiveFilterFields.LoadAsync(dbContext, job.RequestedUser, modelId, token)
                    .ConfigureAwait(false);

            var filter = new JournalMatchFilter(job.CaseKey, job.KeyValue, job.FromDate, job.ToDate,
                job.ActivationsOnly == 1, job.ResponseElevation, JournalMerge.Parse(job.MergeJson) ?? [],
                ArchiveFilter.Parse(job.FilterJson, fields));

            var indexed = await new CaseKeyJournalKeyIndexRepository(dbContext).GetReadyKeyNamesAsync(token)
                .ConfigureAwait(false);

            var guids = await JournalMatcher.GuidsAsync(
                dynamicEnvironment.AppSettings("ReportConnectionString") ?? dynamicEnvironment.AppSettings("ConnectionString"),
                log, modelId, job.RequestedUser, filter, indexed, CaseKeyJournalTagJobService.MaximumMatches, token)
                .ConfigureAwait(false);

            if (guids.Count > CaseKeyJournalTagJobService.MaximumMatches)
            {
                throw new InvalidOperationException("Too many transactions now match to tag them in one job.");
            }

            await repository.SetTotalAsync(job.Id, guids.Count, token).ConfigureAwait(false);

            var add = JsonConvert.DeserializeObject<string[]>(job.AddJson ?? "[]") ?? [];
            var remove = JsonConvert.DeserializeObject<string[]>(job.RemoveJson ?? "[]") ?? [];
            var processed = 0;
            var changed = 0;
            var skipped = 0;
            var locked = 0;

            foreach (var batch in guids.Chunk(BatchSize))
            {
                if (await repository.CancelRequestedAsync(job.Id, token).ConfigureAwait(false))
                {
                    return CaseKeyJournalTagJobState.Cancelled;
                }

                var result = await service.UpdateAsync(new CaseKeyJournalTagRequestDto
                {
                    CaseWorkflowGuid = job.CaseWorkflowGuid, Transactions = batch, Add = add, Remove = remove
                }, token).ConfigureAwait(false);

                var now = result.Current.ToDictionary(set => set.Transaction, set => set.Tag);
                var before = result.Previous.Where(set => !now[set.Transaction].SequenceEqual(set.Tag)).ToList();

                await repository.AddRowsAsync(job.Id,
                    before.Select(set => (set.Transaction, JsonConvert.SerializeObject(set.Tag))).ToList(), token)
                    .ConfigureAwait(false);

                processed += batch.Length;
                changed += before.Count;
                skipped += result.Skipped;
                locked += result.Locked;

                await repository.ProgressAsync(job.Id, processed, changed, skipped, locked, token)
                    .ConfigureAwait(false);
            }

            return CaseKeyJournalTagJobState.Done;
        }

        private async Task<CaseKeyJournalTagJobState> UndoAsync(CaseKeyJournalTagJobRepository repository, CaseKeyJournalTagService service, CaseKeyJournalTagJob job,
            CancellationToken token)
        {
            var original = await repository.GetAsync(job.UndoOfGuid!.Value, token).ConfigureAwait(false)
                           ?? throw new NotFoundException("The tagging to undo was not found.");

            await repository.SetTotalAsync(job.Id, original.Changed, token).ConfigureAwait(false);

            var processed = 0;
            var changed = 0;
            var skipped = 0;
            var locked = 0;
            var after = 0L;

            while (true)
            {
                if (await repository.CancelRequestedAsync(job.Id, token).ConfigureAwait(false))
                {
                    return CaseKeyJournalTagJobState.Cancelled;
                }

                var rows = await repository.RowsAsync(original.Id, after, BatchSize, token).ConfigureAwait(false);

                if (rows.Count == 0)
                {
                    break;
                }

                var sets = rows.Select(row => new CaseKeyJournalTagSetDto
                {
                    Transaction = row.EntityAnalysisModelInstanceEntryGuid,
                    Tag = JsonConvert.DeserializeObject<string[]>(row.PreviousTagJson) ?? []
                }).ToArray();

                var result = await service.RestoreAsync(new CaseKeyJournalTagRestoreRequestDto
                {
                    CaseWorkflowGuid = job.CaseWorkflowGuid, Sets = sets
                }, token).ConfigureAwait(false);

                var now = result.Current.ToDictionary(set => set.Transaction, set => set.Tag);

                processed += rows.Count;
                changed += result.Previous.Count(set => !now[set.Transaction].SequenceEqual(set.Tag));
                skipped += result.Skipped;
                locked += result.Locked;
                after = rows[^1].Id;

                await repository.ProgressAsync(job.Id, processed, changed, skipped, locked, token)
                    .ConfigureAwait(false);
            }

            return CaseKeyJournalTagJobState.Done;
        }
    }
}
