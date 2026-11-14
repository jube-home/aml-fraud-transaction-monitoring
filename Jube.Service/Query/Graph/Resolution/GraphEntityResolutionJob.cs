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
using Jube.Data.Repository;
using Jube.Dictionary.Fuzzy;
using Jube.Dto.Options;
using Jube.Engine.Sanctions;
using LinqToDB.Async;
using LinqToDB.Data;
using log4net;

namespace Jube.Service.Query.Graph.Resolution
{
    public sealed record GraphEntityResolutionOptions(
        int MaximumCluster = 10,
        bool AutoConfirmHard = false,
        IReadOnlyList<string>? FuzzyKeys = null,
        double MinimumPosterior = 0.2,
        double ClosenessThreshold = 0.92,
        int MaximumTokenFrequency = 50,
        int MaximumNodesPerKey = 20000);

    public sealed record GraphEntityResolutionResult(int Tenants, int HardCandidates, int FuzzyCandidates,
        int AutoConfirmed, int ResolvedNodes);

    public sealed class GraphEntityResolutionJob(string connectionString, ILog log,
        GraphEntityResolutionOptions options)
    {
        public const double HardPosterior = 0.999;
        public const double ExactAgreementM = 0.95;
        public const double CloseAgreementM = 0.6;
        public const double CloseAgreementU = 0.001;

        private const string HardSql = """
            INSERT INTO "GraphEntityMatch" ("TenantRegistryId","NodeIdA","NodeIdB","SourceId","Posterior","Evidence",
                "DecisionId","CreatedDate")
            SELECT @tenant, LEAST(a."FromNodeId", b."FromNodeId"), GREATEST(a."FromNodeId", b."FromNodeId"), 1, @posterior,
                jsonb_build_object('identifierKey', n."Key", 'identifierValue', n."Value", 'edge', d."Name",
                    'records', k."K"),
                1, (now() at time zone 'utc')
            FROM "GraphEdge" a
            JOIN "GraphEdge" b ON b."TenantRegistryId" = a."TenantRegistryId"
                AND b."GraphRegistryEdgeGuid" = a."GraphRegistryEdgeGuid" AND b."ToNodeId" = a."ToNodeId"
                AND b."FromNodeId" > a."FromNodeId"
            JOIN "GraphRegistryEdge" d ON d."Guid" = a."GraphRegistryEdgeGuid" AND COALESCE(d."Deleted", 0) = 0
                AND d."Hard" = 1
            JOIN "GraphNode" n ON n."Id" = a."ToNodeId" AND n."Hub" = 0
            JOIN "GraphNode" fa ON fa."Id" = a."FromNodeId"
            JOIN "GraphNode" fb ON fb."Id" = b."FromNodeId" AND fb."Key" = fa."Key"
            JOIN (SELECT "GraphRegistryEdgeGuid" AS "Def", "ToNodeId" AS "To", COUNT(DISTINCT "FromNodeId") AS "K"
                  FROM "GraphEdge" WHERE "TenantRegistryId" = @tenant AND "Restricted" = 0
                  GROUP BY "GraphRegistryEdgeGuid", "ToNodeId" HAVING COUNT(DISTINCT "FromNodeId") <= @maximum) k
                ON k."Def" = a."GraphRegistryEdgeGuid" AND k."To" = a."ToNodeId"
            WHERE a."TenantRegistryId" = @tenant AND a."Restricted" = 0 AND b."Restricted" = 0
            ON CONFLICT ("TenantRegistryId","NodeIdA","NodeIdB") DO NOTHING
            """;

        private const string AutoConfirmSql = """
            UPDATE "GraphEntityMatch" SET "DecisionId" = 2, "DecidedUser" = 'System',
                "DecidedDate" = (now() at time zone 'utc'), "DecisionNote" = 'Shared hard identifier (automatic).'
            WHERE "TenantRegistryId" = @tenant AND "SourceId" = 1 AND "DecisionId" = 1
            """;

        public async Task<GraphEntityResolutionResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString,
                log);

            var tenants = await dbContext.GraphRegistry
                .Where(r => r.Active == 1 && (r.Deleted == null || r.Deleted == 0) && r.TenantRegistryId != null)
                .Select(r => r.TenantRegistryId!.Value).Distinct().ToListAsync(token).ConfigureAwait(false);

            int hard = 0, fuzzy = 0, auto = 0, resolved = 0;

            foreach (var tenant in tenants.OrderBy(t => t))
            {
                token.ThrowIfCancellationRequested();

                hard += await dbContext.ExecuteAsync(HardSql, token, new DataParameter("tenant", tenant),
                    new DataParameter("posterior", HardPosterior),
                    new DataParameter("maximum", options.MaximumCluster)).ConfigureAwait(false);

                fuzzy += await FuzzyAsync(dbContext, tenant, token).ConfigureAwait(false);

                if (options.AutoConfirmHard)
                {
                    auto += await dbContext.ExecuteAsync(AutoConfirmSql, token, new DataParameter("tenant", tenant))
                        .ConfigureAwait(false);
                }

                resolved += await new GraphEntityMatchRepository(dbContext)
                    .RecomputeResolutionAsync(tenant, token).ConfigureAwait(false);
            }

            return new GraphEntityResolutionResult(tenants.Count, hard, fuzzy, auto, resolved);
        }

        public static string Fold(string value)
        {
            return SanctionTextFolding.Spaced(SanctionTextFolding.Fold(value)).Trim();
        }

        public static (string Level, double Weight, double Similarity)? Compare(string foldedA, string foldedB,
            double valueFrequency, double closeness)
        {
            if (foldedA.Length == 0 || foldedB.Length == 0)
            {
                return null;
            }

            if (string.Equals(foldedA, foldedB, StringComparison.Ordinal))
            {
                return ("Exact", FellegiSunter.AgreementWeight(ExactAgreementM,
                    FellegiSunter.TermFrequencyU(valueFrequency)), 1);
            }

            var similarity = Math.Max(FuzzyScorers.JaroWinkler(foldedA, foldedB),
                FuzzyScorers.TokenSort(foldedA, foldedB));

            return similarity >= closeness
                ? ("Close", FellegiSunter.AgreementWeight(CloseAgreementM, CloseAgreementU), similarity)
                : null;
        }

        private async Task<int> FuzzyAsync(DbContext dbContext, int tenant, CancellationToken token)
        {
            var inserted = 0;

            foreach (var key in options.FuzzyKeys ?? [])
            {
                var nodes = await dbContext.GraphNode
                    .Where(n => n.TenantRegistryId == tenant && n.Key == key)
                    .OrderBy(n => n.Id).Take(options.MaximumNodesPerKey)
                    .Select(n => new { n.Id, n.Value }).ToListAsync(token).ConfigureAwait(false);

                var folded = nodes.ToDictionary(n => n.Id, n => Fold(n.Value));
                var byValue = folded.Values.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
                var postings = new Dictionary<string, List<long>>(StringComparer.Ordinal);

                foreach (var (id, text) in folded)
                {
                    foreach (var tokenText in SanctionTextFolding.Tokens(text).Distinct())
                    {
                        if (!postings.TryGetValue(tokenText, out var list))
                        {
                            postings[tokenText] = list = [];
                        }

                        list.Add(id);
                    }
                }

                var seen = new HashSet<(long, long)>();
                var repository = new GraphEntityMatchRepository(dbContext);

                foreach (var list in postings.Values.Where(l => l.Count >= 2 && l.Count <= options.MaximumTokenFrequency))
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        for (var j = i + 1; j < list.Count; j++)
                        {
                            var low = Math.Min(list[i], list[j]);
                            var high = Math.Max(list[i], list[j]);

                            if (!seen.Add((low, high)))
                            {
                                continue;
                            }

                            var a = folded[low];
                            var b = folded[high];
                            var others = Math.Max(1, folded.Count - 1);
                            var frequency = (double)Math.Max(0, byValue[a] - 1) / others;
                            var comparison = Compare(a, b, frequency, options.ClosenessThreshold);

                            if (comparison is null)
                            {
                                continue;
                            }

                            var posterior = FellegiSunter.Posterior(1d / Math.Max(2, folded.Count), [comparison.Value.Weight]);

                            if (posterior < options.MinimumPosterior)
                            {
                                continue;
                            }

                            var evidence = JsonSerializer.Serialize(new
                            {
                                field = key,
                                agreement = comparison.Value.Level,
                                similarity = Math.Round(comparison.Value.Similarity, 4),
                                weight = Math.Round(comparison.Value.Weight, 3),
                                a,
                                b
                            });

                            if (await repository.InsertIfNewAsync(new GraphEntityMatch
                                {
                                    TenantRegistryId = tenant,
                                    NodeIdA = low,
                                    NodeIdB = high,
                                    SourceId = (short)GraphEntityMatchSource.Fuzzy,
                                    Posterior = posterior,
                                    Evidence = evidence,
                                    DecisionId = (short)GraphEntityMatchDecision.Pending
                                }, token).ConfigureAwait(false))
                            {
                                inserted++;
                            }
                        }
                    }
                }
            }

            return inserted;
        }
    }
}
