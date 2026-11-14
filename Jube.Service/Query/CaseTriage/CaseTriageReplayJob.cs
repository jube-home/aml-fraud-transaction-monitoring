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
using Jube.Service.Query.ArchiveKeyFields;
using Jube.Service.Query.CaseNarrative;
using Jube.Service.Query.CaseTriage.Interfaces;
using log4net;

namespace Jube.Service.Query.CaseTriage
{
    public sealed class CaseTriageReplayJob(
        string connectionString,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ICaseTriageReplayAgent agent,
        ILog log)
    {
        private const int ArchiveRows = 10;
        private const int NoteRows = 20;
        private const string FailureMessage = "The replay could not be completed; see the server log.";

        public async Task<int> RunPendingAsync(CancellationToken token = default)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                connectionString, log);

            var repository = new CaseAgentReplayRepository(dbContext);
            var ran = 0;

            foreach (var replay in await repository.ListPendingAsync(5, token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();

                if (!await repository.ClaimAsync(replay.Guid, token).ConfigureAwait(false))
                {
                    continue;
                }

                try
                {
                    await RunAsync(dbContext, repository, replay, token).ConfigureAwait(false);
                    ran++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error($"CaseTriageReplayJob: replay {replay.Guid} failed.", ex);

                    await repository.CompleteAsync(replay.Guid, CaseAgentReplayStatus.Failed, 0, 0, null,
                        FailureMessage, token).ConfigureAwait(false);
                }
            }

            return ran;
        }

        private async Task RunAsync(DbContext dbContext, CaseAgentReplayRepository repository, CaseAgentReplay replay,
            CancellationToken token)
        {
            var prompt = string.IsNullOrWhiteSpace(replay.PromptText) ? CaseTriagePrompt.Replay : replay.PromptText;

            var accessible = (await new CaseSearchFilterRepository(dbContext, replay.CreatedUser,
                    replay.TenantRegistryId).ListAccessibleWorkflowsAsync(token).ConfigureAwait(false))
                .Select(w => w.Guid)
                .Where(g => replay.CaseWorkflowGuid == null || g == replay.CaseWorkflowGuid)
                .ToArray();

            var candidates = accessible.Length == 0
                ? []
                : await repository.ListClosedCasesAsync(replay.TenantRegistryId, replay.FromDate,
                    replay.ToDate, accessible, replay.MaximumCases, token).ConfigureAwait(false);

            var resolver = new ArchiveKeyFieldResolver(dbContext,
                ArchiveKeyFieldResolver.DeterministicEncryptor(dynamicEnvironment));

            var scored = 0;
            string? modelName = null;

            foreach (var candidate in candidates)
            {
                token.ThrowIfCancellationRequested();

                if (!CaseNarrativePrompt.IsSafe(candidate.CaseKey, candidate.CaseKeyValue))
                {
                    continue;
                }

                var asOf = candidate.CreatedDate.AddMinutes(replay.AsOfMinutes);

                var key = await resolver.ResolveAsync(candidate.EntityAnalysisModelId, candidate.CaseKey,
                    candidate.CaseKeyValue, token).ConfigureAwait(false);

                var archive = key.Predicate is null
                    ? []
                    : await repository.ArchiveBeforeAsync(candidate.EntityAnalysisModelId, key.Predicate, asOf,
                        ArchiveRows, token).ConfigureAwait(false);

                var notes = await repository.NotesBeforeAsync(replay.TenantRegistryId, candidate.CaseKey, candidate.CaseKeyValue, asOf,
                    NoteRows, token).ConfigureAwait(false);

                var prior = await repository.PriorOutcomesBeforeAsync(replay.TenantRegistryId, candidate.CaseKey,
                    candidate.CaseKeyValue, candidate.CaseId, asOf, token).ConfigureAwait(false);

                var answer = await agent.AskAsync(prompt,
                    CaseTriageReplayBundle.Build(candidate, asOf, archive, notes, prior), token)
                    .ConfigureAwait(false);

                modelName ??= answer?.ModelName;
                var parsed = answer is null ? null : CaseTriageAnswer.TryParse(answer.Text);

                await repository.InsertResultAsync(new CaseAgentReplayResult
                {
                    ReplayGuid = replay.Guid,
                    CaseId = candidate.CaseId,
                    HumanOutcomeId = (short)candidate.HumanOutcomeId,
                    RecommendedOutcomeId = parsed is null ? null : (short)parsed.OutcomeId,
                    AsOfDate = asOf
                }, token).ConfigureAwait(false);

                if (parsed is not null)
                {
                    scored++;
                }
            }

            await repository.CompleteAsync(replay.Guid, CaseAgentReplayStatus.Completed, candidates.Count, scored,
                modelName is { Length: > 200 } ? modelName[..200] : modelName, null, token).ConfigureAwait(false);
        }
    }
}
