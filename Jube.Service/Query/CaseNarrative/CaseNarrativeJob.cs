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
using Jube.Data.Query.GetCaseNarrativeCandidatesQuery;
using Jube.Data.Query.GetCaseNarrativeFactsQuery;
using Jube.Data.Repository;
using Jube.Service.Query.CaseNarrative.Interfaces;
using Jube.Service.Query.ArchiveKeyFields;
using log4net;

namespace Jube.Service.Query.CaseNarrative
{
    public sealed class CaseNarrativeJob(
        string connectionString,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ICaseNarrativeAgent agent,
        ILog log)
    {
        private const int MaximumCandidates = 5000;
        private const int MaximumAnswerLength = 20000;

        public async Task<int> RunNarrativesAsync(CancellationToken token = default)
        {
            var users = (dynamicEnvironment.AppSettings("CaseNarrativeUsers") ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var maximum = Setting("CaseNarrativeMaximumCases", 25);
            var written = 0;

            foreach (var user in users)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    written += await NarrateForUserAsync(user, maximum, token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error($"CaseNarrativeJob: narrating as '{user}' failed.", ex);
                }
            }

            return written;
        }

        private async Task<int> NarrateForUserAsync(string user, int maximum, CancellationToken token)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                connectionString, log);

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, user, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                log.Warn($"CaseNarrativeJob: user '{user}' resolves to no tenant; skipped.");

                return 0;
            }

            var candidates = await new GetCaseNarrativeCandidatesQuery(dbContext)
                .ExecuteAsync(tenant.Value, 0, MaximumCandidates, token).ConfigureAwait(false);

            var repository = new CaseNoteRepository(dbContext, user);
            var written = 0;

            foreach (var candidate in candidates)
            {
                token.ThrowIfCancellationRequested();

                if (written >= maximum)
                {
                    break;
                }

                if (!CaseNarrativePrompt.IsSafe(candidate.CaseKey, candidate.CaseKeyValue))
                {
                    log.Warn($"CaseNarrativeJob: case {candidate.CaseId} has a key that is not plain text; skipped.");

                    continue;
                }

                await using var held = await CaseNarrativeAdvisoryLock
                    .TryAcquireAsync(connectionString, $"CaseNarrative|{candidate.CaseId}", token)
                    .ConfigureAwait(false);

                if (held is null)
                {
                    continue;
                }

                if (await NarrateAsync(dbContext, repository, user, candidate, token).ConfigureAwait(false))
                {
                    written++;
                }
            }

            return written;
        }

        private async Task<bool> NarrateAsync(DbContext dbContext, CaseNoteRepository repository, string user,
            GetCaseNarrativeCandidatesQueryDto candidate, CancellationToken token)
        {
            var caseKey = await new ArchiveKeyFieldResolver(dbContext,
                    ArchiveKeyFieldResolver.DeterministicEncryptor(dynamicEnvironment))
                .ResolveAsync(candidate.EntityAnalysisModelId, candidate.CaseKey, candidate.CaseKeyValue, token)
                .ConfigureAwait(false);

            var facts = await new GetCaseNarrativeFactsQuery(dbContext)
                .ExecuteAsync(candidate.EntityAnalysisModelId, caseKey.Predicate, candidate.CaseKey,
                    candidate.CaseKeyValue, token).ConfigureAwait(false);

            var hash = CaseNarrativeFacts.Hash(candidate, facts);
            var latest = await repository.GetLatestNarrativeAsync(candidate.CaseId, token).ConfigureAwait(false);

            if (latest?.FactsHash == hash)
            {
                return false;
            }

            var answer = await agent.AskAsync(user, CaseNarrativePrompt.Narrative,
                CaseNarrativePrompt.PageContext("narrative", candidate.CaseKey, candidate.CaseKeyValue,
                    candidate.CaseId), token).ConfigureAwait(false);

            if (answer is null || string.IsNullOrWhiteSpace(answer.Text))
            {
                log.Warn($"CaseNarrativeJob: AskJooby gave no narrative for case {candidate.CaseId}.");

                return false;
            }

            var saved = await repository.InsertNarrativeAsync(new CaseNote
            {
                Note = Clip(answer.Text),
                CaseKey = candidate.CaseKey,
                CaseKeyValue = candidate.CaseKeyValue,
                CaseId = candidate.CaseId,
                FactsHash = hash,
                AskJoobyConversationId = answer.ConversationId
            }, token).ConfigureAwait(false);

            return saved is not null;
        }

        private static string Clip(string text)
        {
            var trimmed = text.Trim();

            return trimmed.Length > MaximumAnswerLength ? trimmed[..MaximumAnswerLength] : trimmed;
        }

        private int Setting(string name, int fallback)
        {
            return int.TryParse(dynamicEnvironment.AppSettings(name), out var parsed) && parsed > 0
                ? parsed
                : fallback;
        }
    }
}
