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

using System.Text.Json;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetCaseNarrativeCandidatesQuery;
using Jube.Data.Query.GetCaseNarrativeFactsQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Service.Query.ArchiveKeyFields;
using Jube.Service.Query.CaseNarrative;
using Jube.Service.Query.CaseNarrative.Interfaces;
using log4net;

namespace Jube.Service.Query.CaseTriage
{
    public sealed class CaseTriageJob(
        string connectionString,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ICaseNarrativeAgent agent,
        ILog log)
    {
        private const int MaximumCandidates = 5000;

        public async Task<int> RunAsync(CancellationToken token = default)
        {
            var users = Split(dynamicEnvironment.AppSettings("CaseTriageUsers"));

            var workflows = Split(dynamicEnvironment.AppSettings("CaseTriageWorkflows"))
                .Select(s => Guid.TryParse(s, out var guid) ? guid : (Guid?)null)
                .Where(w => w.HasValue).Select(s => s!.Value).ToList();

            if (workflows.Count == 0)
            {
                return 0;
            }

            var maximum = int.TryParse(dynamicEnvironment.AppSettings("CaseTriageMaximumCases"), out var parsed)
                          && parsed > 0
                ? parsed
                : 25;

            var written = 0;

            foreach (var user in users)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    written += await TriageForUserAsync(user, workflows, maximum, token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.Error($"CaseTriageJob: triaging as '{user}' failed.", ex);
                }
            }

            return written;
        }

        private async Task<int> TriageForUserAsync(string user, List<Guid> workflows, int maximum,
            CancellationToken token)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                connectionString, log);

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, user, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                log.Warn($"CaseTriageJob: user '{user}' resolves to no tenant; skipped.");

                return 0;
            }

            var repository = new CaseAgentRunRepository(dbContext);

            var candidates = await repository.ListCandidatesAsync(tenant.Value, workflows, MaximumCandidates, token)
                .ConfigureAwait(false);

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
                    log.Warn($"CaseTriageJob: case {candidate.CaseId} has a key that is not plain text; skipped.");

                    continue;
                }

                await using var held = await CaseNarrativeAdvisoryLock
                    .TryAcquireAsync(connectionString, $"CaseTriage|{candidate.CaseId}", token)
                    .ConfigureAwait(false);

                if (held is null)
                {
                    continue;
                }

                if (await TriageAsync(dbContext, repository, user, candidate, token).ConfigureAwait(false))
                {
                    written++;
                }
            }

            return written;
        }

        private async Task<bool> TriageAsync(DbContext dbContext, CaseAgentRunRepository repository, string user,
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

            if (await repository.LatestFactsHashAsync(candidate.CaseId, token).ConfigureAwait(false) == hash)
            {
                return false;
            }

            var answer = await agent.AskAsync(user, CaseTriagePrompt.Triage,
                CaseNarrativePrompt.PageContext("triage", candidate.CaseKey, candidate.CaseKeyValue,
                    candidate.CaseId), token).ConfigureAwait(false);

            var parsed = answer is null ? null : CaseTriageAnswer.TryParse(answer.Text);

            if (answer is null || parsed is null)
            {
                log.Warn($"CaseTriageJob: no usable recommendation for case {candidate.CaseId}.");

                return false;
            }

            await repository.InsertSupersedingAsync(new CaseAgentRun
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = candidate.TenantRegistryId,
                CaseId = candidate.CaseId,
                RecommendedOutcomeId = (short)parsed.OutcomeId,
                Rationale = parsed.Rationale,
                Evidence = parsed.Evidence.Count == 0 ? null : JsonSerializer.Serialize(parsed.Evidence),
                FactsHash = hash,
                AskJoobyConversationId = answer.ConversationId,
                ModelName = answer.ModelName is { Length: > 200 } name ? name[..200] : answer.ModelName,
                AgentDefinitionHash = CaseTriagePrompt.DefinitionHash(),
                StateId = (short)CaseAgentRunState.Pending,
                CreatedDate = DateTime.UtcNow
            }, token).ConfigureAwait(false);

            await new CaseEventRepository(dbContext, user).InsertAsync(new CaseEvent
            {
                CaseId = candidate.CaseId,
                CaseKey = candidate.CaseKey,
                CaseKeyValue = candidate.CaseKeyValue,
                CaseEventTypeId = CaseEventRepository.TriageRecommendedEventTypeId,
                After = parsed.OutcomeId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }, token).ConfigureAwait(false);

            return true;
        }

        private static string[] Split(string? value)
        {
            return (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries
                                                      | StringSplitOptions.TrimEntries);
        }
    }
}
