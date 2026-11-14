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
using Jube.CaseRisk.Similarity;
using Jube.Data.Context;
using Jube.Data.Query.GetSimilarCaseComponentsQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSimilarity;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseSimilarity;
using Jube.Service.Observability;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.CaseSimilarity
{
    public sealed class CaseSimilarityService
    {
        public const int MinimumClosed = 5;
        public const int Seeds = 8;
        public const int SeedCap = 500;
        public const int Candidates = 300;
        public const int NeighbourTake = 10;
        public const int ReasonTake = 6;
        private const byte Closed = 3;
        private const byte Confirmed = 1;
        private const byte NotConfirmed = 2;
        private static readonly int[] permissions = [1];
        private static readonly SimilaritySettings settings = new();
        private readonly ILog auditLog;
        private readonly CaseRepository caseRepository;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseSimilarityService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            caseRepository = new CaseRepository(dbContext, userName);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseSimilarityService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseSimilarityService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseSimilarityResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseSimilarityResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseSimilarityResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseSimilarityService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Finds the cases of the same case workflow that resemble a case, and says how the closed ones " +
                     "were decided. Two cases are similar when they share facts: the same rules or models fired, " +
                     "the same tags, the same device or counterparty, the same kind and size of activity. Rare " +
                     "shared facts count for more than common ones. Returns how many similar cases there are, how " +
                     "many of the closed ones were confirmed, how many are still open, and the ten most similar " +
                     "with the facts each shares (field names only, never field values). The score is a " +
                     "similarity from 0 to 1, not a probability. Informational only: it never changes a case's " +
                     "ranking. Only cases the caller may see are counted. Read-only.")]
        [ServiceOperation("CaseSimilarityGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseSimilarityDto> GetAsync(
            [Description("Identifier of the case.")]
            int caseId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSimilarity", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();

                var existing = await caseRepository.GetByIdActiveOnlyAsync(caseId, token).ConfigureAwait(false)
                               ?? throw new NotFoundException(strings[CaseSimilarityResources.CaseNotFound]);

                var result = await BuildAsync(existing, token).ConfigureAwait(false);

                op.Entity(caseId);
                op.Rows(result.Neighbours.Count);

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
                log.Error($"CaseSimilarity.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<CaseSimilarityDto> BuildAsync(Data.Poco.Case existing, CancellationToken token)
        {
            var result = new CaseSimilarityDto
            {
                Built = existing.SimilarityCheckedDate.HasValue,
                CheckedDate = CaseSearchMapper.Utc(existing.SimilarityCheckedDate)
            };

            var rows = await new GetSimilarCaseComponentsQuery(dbContext)
                .ExecuteAsync(existing.Id, existing.CaseWorkflowGuid, Seeds, SeedCap, Candidates, token)
                .ConfigureAwait(false);

            if (rows.Count == 0)
            {
                return result;
            }

            var total = rows[0].TotalCases;
            var weights = new Dictionary<SimilarityComponent, double>();
            var vectors = new Dictionary<int, List<SimilarityComponent>>();

            foreach (var row in rows)
            {
                var component = new SimilarityComponent((CaseSimilarityKind)row.KindId, row.Component);

                weights[component] = SimilarityWeight.Of(row.CaseCount, total, settings);

                if (!vectors.TryGetValue(row.CaseId, out var vector))
                {
                    vector = [];
                    vectors[row.CaseId] = vector;
                }

                vector.Add(component);
            }

            if (!vectors.TryGetValue(existing.Id, out var mine))
            {
                return result;
            }

            var scored = vectors
                .Where(pair => pair.Key != existing.Id)
                .Select(pair => (CaseId: pair.Key, Result: SimilarityCalculator.Compare(mine, pair.Value, Weight,
                    settings)))
                .Where(pair => pair.Result.Band >= CaseSimilarityBand.Moderate)
                .ToDictionary(pair => pair.CaseId, pair => pair.Result);

            var visible = await caseRepository.GetVisibleIdsActiveOnlyAsync([.. scored.Keys], token)
                .ConfigureAwait(false);

            var cases = await (from c in dbContext.Case
                    from s in dbContext.CaseWorkflowStatus.Where(s =>
                        s.Guid == c.CaseWorkflowStatusGuid && (s.Deleted == null || s.Deleted == 0))
                    where visible.Contains(c.Id) && c.CaseWorkflowGuid == existing.CaseWorkflowGuid
                    select new
                    {
                        c.Id, c.CaseKey, c.CaseKeyValue, c.ClosedStatusId, c.ClosedDate, c.CreatedDate,
                        Status = s.Name, s.OutcomeId
                    })
                .ToListAsync(token).ConfigureAwait(false);

            var community = await CommunityAsync(existing.CaseKey, existing.CaseKeyValue, token)
                .ConfigureAwait(false);

            var ranked = cases
                .GroupBy(c => c.Id).Select(g => g.First())
                .OrderByDescending(c => scored[c.Id].Score)
                .ThenByDescending(c => scored[c.Id].SharedWeight)
                .ThenByDescending(c => c.Id)
                .ToList();

            result.Similar = ranked.Count;
            result.Closed = ranked.Count(c => c.ClosedStatusId == Closed && c.OutcomeId is Confirmed or NotConfirmed);
            result.Confirmed = ranked.Count(c => c.ClosedStatusId == Closed && c.OutcomeId == Confirmed);
            result.Open = ranked.Count(c => CaseSimilarityJob.IsOpen(c.ClosedStatusId));
            result.EnoughToJudge = result.Closed >= MinimumClosed;

            foreach (var c in ranked.Take(NeighbourTake))
            {
                var similarity = scored[c.Id];

                var linked = similarity.Kinds.Any(kind => kind is
                                 { Kind: CaseSimilarityKind.Entity, Shared.Count: > 0 })
                             || (community.HasValue && community == await CommunityAsync(c.CaseKey,
                                 c.CaseKeyValue, token).ConfigureAwait(false));

                result.Neighbours.Add(new CaseSimilarNeighbourDto
                {
                    CaseId = c.Id,
                    CaseKey = c.CaseKey,
                    CaseKeyValue = c.CaseKeyValue,
                    Status = c.Status,
                    ClosedStatusId = c.ClosedStatusId.GetValueOrDefault(),
                    OutcomeId = c.OutcomeId,
                    CreatedDate = CaseSearchMapper.Utc(c.CreatedDate),
                    ClosedDate = c.ClosedStatusId == Closed ? CaseSearchMapper.Utc(c.ClosedDate) : null,
                    BandId = (byte)similarity.Band,
                    Score = Math.Round(similarity.Score, 4),
                    Linked = linked,
                    Reasons = CaseSimilarityReasons.From(similarity, Weight, ReasonTake)
                });
            }

            return result;

            double Weight(SimilarityComponent component) => weights.GetValueOrDefault(component);
        }

        private async Task<long?> CommunityAsync(string? caseKey, string? caseKeyValue, CancellationToken token)
        {
            if (string.IsNullOrEmpty(caseKey) || string.IsNullOrEmpty(caseKeyValue))
            {
                return null;
            }

            return await dbContext.GraphNode
                .Where(n => n.TenantRegistryId == tenantRegistryId && n.Key == caseKey && n.Value == caseKeyValue)
                .Select(n => n.CommunityId).FirstOrDefaultAsync(token).ConfigureAwait(false);
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"CaseSimilarity: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[CaseSimilarityResources.PermissionDenied], permissions);
        }
    }
}
