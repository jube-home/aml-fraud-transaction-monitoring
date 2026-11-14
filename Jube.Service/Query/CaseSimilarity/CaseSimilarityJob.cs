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

using Jube.CaseRisk.Similarity;
using Jube.Data.Context;
using Jube.Data.Query.GetCaseSimilarityDueQuery;
using Jube.Data.Query.GetCaseSimilarityRowsQuery;
using Jube.Data.Query.ReplaceCaseSimilarityComponentsQuery;
using Jube.Dto.Options;
using Jube.Service.Query.ArchiveKeyFields;
using Jube.Service.Query.CaseNarrative;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseSimilarity
{
    public sealed class CaseSimilarityJob(
        string connectionString,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        CaseSimilarityJobSettings settings)
    {
        public const string LockName = "CaseSimilaritySweep";

        private readonly Func<string, string, string>? hash =
            CaseSimilarityHash.KeyOf(dynamicEnvironment) is { } secret ? CaseSimilarityHash.For(secret) : null;
        private readonly SimilaritySettings similarity = new();

        public static bool IsOpen(byte? closedStatusId) => closedStatusId is null or 0 or 1;

        public bool HasHashingKey => hash is not null;

        public async Task<CaseSimilaritySweepResult> RunOnceAsync(CancellationToken token = default)
        {
            if (hash is null)
            {
                return new CaseSimilaritySweepResult(0, 0, 0);
            }

            await using var held = await CaseNarrativeAdvisoryLock.TryAcquireAsync(connectionString, LockName, token)
                .ConfigureAwait(false);

            if (held is null)
            {
                return new CaseSimilaritySweepResult(0, 0, 0);
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                connectionString, log);

            var due = await new GetCaseSimilarityDueQuery(dbContext)
                .ExecuteAsync(DateTime.UtcNow - settings.RefreshAfter, settings.MaximumCases, token)
                .ConfigureAwait(false);

            var resolver = new ArchiveKeyFieldResolver(dbContext,
                ArchiveKeyFieldResolver.DeterministicEncryptor(dynamicEnvironment));

            var changed = 0;
            var failed = 0;

            foreach (var row in due)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    if (await RebuildAsync(dbContext, resolver, row, token).ConfigureAwait(false) > 0)
                    {
                        changed++;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    log.Error($"CaseSimilarityJob: rebuilding case {row.CaseId} failed.", ex);

                    await dbContext.Case.Where(c => c.Id == row.CaseId)
                        .Set(c => c.SimilarityCheckedDate, DateTime.UtcNow)
                        .UpdateAsync(token).ConfigureAwait(false);
                }
            }

            return new CaseSimilaritySweepResult(due.Count, changed, failed);
        }

        public async Task<int> RebuildAsync(DbContext dbContext, ArchiveKeyFieldResolver resolver,
            GetCaseSimilarityDueQueryDto row, CancellationToken token = default)
        {
            if (hash is null)
            {
                return 0;
            }

            var now = DateTime.UtcNow;
            var components = new List<SimilarityComponent>();
            var tags = new HashSet<string>(StringComparer.Ordinal);

            if (row.EntityAnalysisModelId is { } modelId)
            {
                components.AddRange(await ReadTransactionsAsync(dbContext, resolver, row, modelId, now, tags, hash,
                        token)
                    .ConfigureAwait(false));

                tags.UnionWith(await dbContext.CaseTag
                    .Where(t => t.CaseId == row.CaseId && (t.Deleted == null || t.Deleted == 0) && t.Name != null)
                    .Select(t => t.Name).ToListAsync(token).ConfigureAwait(false));

                components.AddRange(SimilarityVectorBuilder.Flags(CaseSimilarityKind.Tag, tags));
                components.AddRange(Bands(row));
            }

            var bounded = CaseSimilarityVectorReader.Bound(components, similarity);

            return await new ReplaceCaseSimilarityComponentsQuery(dbContext).ExecuteAsync(row.CaseId,
                row.CaseWorkflowGuid, [.. bounded.Select(c => (short)c.Kind)], [.. bounded.Select(c => c.Value)],
                now, token).ConfigureAwait(false);
        }

        private async Task<List<SimilarityComponent>> ReadTransactionsAsync(DbContext dbContext,
            ArchiveKeyFieldResolver resolver, GetCaseSimilarityDueQueryDto row, int modelId, DateTime now,
            HashSet<string> tags, Func<string, string, string> keyed, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(row.CaseKey) || string.IsNullOrWhiteSpace(row.CaseKeyValue))
            {
                return [];
            }

            var caseKey = await resolver.ResolveAsync(modelId, row.CaseKey, row.CaseKeyValue, token)
                .ConfigureAwait(false);

            if (!caseKey.Resolved)
            {
                return [];
            }

            var to = IsOpen(row.ClosedStatusId) ? now : row.ClosedDate ?? now;
            var from = (row.CreatedDate ?? now).AddDays(-settings.WindowDays);

            var rows = await new GetCaseSimilarityRowsQuery(dbContext)
                .ExecuteAsync(modelId, caseKey.Predicate!, from, to, settings.MaximumRows, token)
                .ConfigureAwait(false);

            var entries = rows.Select(r => r.EntityAnalysisModelInstanceEntryGuid).Distinct().ToList();

            if (entries.Count > 0)
            {
                tags.UnionWith(await dbContext.ArchiveTag
                    .Where(t => entries.Contains(t.EntityAnalysisModelInstanceEntryGuid)
                                && (t.Deleted == null || t.Deleted == 0) && t.Name != null)
                    .Select(t => t.Name).Distinct().ToListAsync(token).ConfigureAwait(false));
            }

            var payloadFields = (await resolver.FieldsAsync(modelId, token).ConfigureAwait(false))
                .Where(field => field.Facet == "payload")
                .GroupBy(field => field.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            return CaseSimilarityVectorReader.Read([.. rows.Select(r => Parse(r.Json)).OfType<JObject>()],
                payloadFields, keyed, similarity);
        }

        private static IEnumerable<SimilarityComponent> Bands(GetCaseSimilarityDueQueryDto row)
        {
            if (row.RiskValue is { } value && SimilarityBanding.Band(value) is { } band)
            {
                yield return new SimilarityComponent(CaseSimilarityKind.Band, "Value=" + band);
            }

            if (row.RiskLaneId is (byte)CaseRiskLane.LiveMoney or (byte)CaseRiskLane.Mandatory)
            {
                yield return new SimilarityComponent(CaseSimilarityKind.Band, "Lane=" + row.RiskLaneId);
            }
        }

        private static JObject? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JObject.Parse(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
