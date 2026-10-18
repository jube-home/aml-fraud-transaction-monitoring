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

// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;

namespace Jube.Data.Query.GetExhaustiveSearchInstanceVariableStatisticsQuery
{
    public class GetExhaustiveSearchInstanceVariableStatisticsQuery
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public GetExhaustiveSearchInstanceVariableStatisticsQuery(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => (int?)s.TenantRegistryId).FirstOrDefault() ?? -1;
        }

        public async Task<List<GetExhaustiveSearchInstanceVariableStatisticsQueryDto>> ExecuteAsync(
            int exhaustiveSearchInstanceId,
            int? exhaustiveSearchInstanceVariableId,
            CancellationToken token = default)
        {
            var variables = await dbContext.ExhaustiveSearchInstanceVariable
                .Where(w =>
                    w.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId
                    && (exhaustiveSearchInstanceVariableId == null || w.Id == exhaustiveSearchInstanceVariableId)
                    && (w.Deleted == 0 || w.Deleted == null)
                    && (w.ExhaustiveSearchInstance.Deleted == 0 || w.ExhaustiveSearchInstance.Deleted == null))
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            var result = variables.Select(v => new GetExhaustiveSearchInstanceVariableStatisticsQueryDto
            {
                Id = v.Id,
                Name = v.Name,
                Mean = v.Mean.GetValueOrDefault(),
                StandardDeviation = v.StandardDeviation.GetValueOrDefault(),
                Minimum = v.Minimum.GetValueOrDefault(),
                Maximum = v.Maximum.GetValueOrDefault(),
                Iqr = v.Iqr.GetValueOrDefault()
            }).ToList();

            if (result.Count == 0)
            {
                return result;
            }

            var ids = result.Select(r => r.Id).ToList();
            var byId = result.ToDictionary(r => r.Id);

            var population = await dbContext.ExhaustiveSearchInstanceVariableHistogram
                .Where(w => ids.Contains(w.ExhaustiveSearchInstanceVariableId.GetValueOrDefault())
                            && w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstance.EntityAnalysisModel
                                .TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.BinSequence)
                .Select(s => new { s.ExhaustiveSearchInstanceVariableId, s.BinRangeStart, s.BinRangeEnd, s.Frequency })
                .ToListAsync(token).ConfigureAwait(false);

            foreach (var row in population)
            {
                byId[row.ExhaustiveSearchInstanceVariableId.GetValueOrDefault()].Population.Add(Bin(
                    row.BinRangeStart, row.BinRangeEnd, row.Frequency));
            }

            var classes = await dbContext.ExhaustiveSearchInstanceVariableHistogramClass
                .Where(w => ids.Contains(w.ExhaustiveSearchInstanceVariableId.GetValueOrDefault())
                            && w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstance.EntityAnalysisModel
                                .TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.BinSequence)
                .Select(s => new
                {
                    s.ExhaustiveSearchInstanceVariableId, s.ClassLabel, s.BinRangeStart, s.BinRangeEnd, s.Frequency
                })
                .ToListAsync(token).ConfigureAwait(false);

            foreach (var row in classes)
            {
                var target = byId[row.ExhaustiveSearchInstanceVariableId.GetValueOrDefault()];

                (row.ClassLabel == 1 ? target.AlignedPositive : target.AlignedNegative)
                    .Add(Bin(row.BinRangeStart, row.BinRangeEnd, row.Frequency));
            }

            var classificationStatistics = await dbContext.ExhaustiveSearchInstanceVariableClassification
                .Where(w => ids.Contains(w.ExhaustiveSearchInstanceVariableId.GetValueOrDefault())
                            && w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstance.EntityAnalysisModel
                                .TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            var classificationById = new Dictionary<int, int>();

            foreach (var row in classificationStatistics)
            {
                var target = byId[row.ExhaustiveSearchInstanceVariableId.GetValueOrDefault()];
                classificationById[row.Id] = target.Id;

                target.Classification =
                    BuildSummary(row.Mean, row.StandardDeviation, row.Minimum, row.Maximum, row.Iqr);
            }

            if (classificationById.Count > 0)
            {
                var classificationIds = classificationById.Keys.ToList();

                var histograms = await dbContext.ExhaustiveSearchInstanceVariableHistogramClassification
                    .Where(w => classificationIds.Contains(
                                    w.ExhaustiveSearchInstanceVariableClassificationId.GetValueOrDefault())
                                && (w.Deleted == 0 || w.Deleted == null))
                    .OrderBy(o => o.BinSequence)
                    .Select(s => new
                    {
                        s.ExhaustiveSearchInstanceVariableClassificationId, s.BinRangeStart, s.BinRangeEnd,
                        s.Frequency
                    })
                    .ToListAsync(token).ConfigureAwait(false);

                foreach (var row in histograms)
                {
                    byId[classificationById[row.ExhaustiveSearchInstanceVariableClassificationId.GetValueOrDefault()]]
                        .ClassificationBins.Add(Bin(row.BinRangeStart, row.BinRangeEnd, row.Frequency));
                }
            }

            var anomalyStatistics = await dbContext.ExhaustiveSearchInstanceVariableAnomaly
                .Where(w => ids.Contains(w.ExhaustiveSearchInstanceVariableId.GetValueOrDefault())
                            && w.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstance.EntityAnalysisModel
                                .TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id)
                .ToListAsync(token).ConfigureAwait(false);

            var anomalyById = new Dictionary<int, int>();

            foreach (var row in anomalyStatistics)
            {
                var target = byId[row.ExhaustiveSearchInstanceVariableId.GetValueOrDefault()];
                anomalyById[row.Id] = target.Id;
                target.Anomaly = BuildSummary(row.Mean, row.StandardDeviation, row.Minimum, row.Maximum, row.Iqr);
            }

            if (anomalyById.Count > 0)
            {
                var anomalyIds = anomalyById.Keys.ToList();

                var histograms = await dbContext.ExhaustiveSearchInstanceVariableHistogramAnomaly
                    .Where(w => anomalyIds.Contains(w.ExhaustiveSearchInstanceVariableAnomalyId.GetValueOrDefault())
                                && (w.Deleted == 0 || w.Deleted == null))
                    .OrderBy(o => o.BinSequence)
                    .Select(s => new
                    {
                        s.ExhaustiveSearchInstanceVariableAnomalyId, s.BinRangeStart, s.BinRangeEnd, s.Frequency
                    })
                    .ToListAsync(token).ConfigureAwait(false);

                foreach (var row in histograms)
                {
                    byId[anomalyById[row.ExhaustiveSearchInstanceVariableAnomalyId.GetValueOrDefault()]]
                        .AnomalyBins.Add(Bin(row.BinRangeStart, row.BinRangeEnd, row.Frequency));
                }
            }

            await AttachPrescriptionAsync(exhaustiveSearchInstanceId, ids, byId, token).ConfigureAwait(false);

            return result;
        }

        private async Task AttachPrescriptionAsync(int exhaustiveSearchInstanceId, List<int> ids,
            Dictionary<int, GetExhaustiveSearchInstanceVariableStatisticsQueryDto> byId, CancellationToken token)
        {
            var promotedTrialInstanceId = await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w => w.Active == 1
                            && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId ==
                            exhaustiveSearchInstanceId
                            && w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.EntityAnalysisModel
                                .TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null)
                            && (w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == 0
                                || w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.Deleted == null))
                .OrderByDescending(o => o.Id)
                .Select(s => s.ExhaustiveSearchInstanceTrialInstanceId)
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (promotedTrialInstanceId == null)
            {
                return;
            }

            var promoted = await (
                from t in dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                from p in dbContext.ExhaustiveSearchInstancePromotedTrialInstanceVariable
                    .Where(w => w.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id)
                where t.ExhaustiveSearchInstanceTrialInstanceId == promotedTrialInstanceId
                      && ids.Contains(t.ExhaustiveSearchInstanceVariableId.GetValueOrDefault())
                      && (t.Removed == 0 || t.Removed == null)
                      && (t.Deleted == 0 || t.Deleted == null)
                      && (p.Deleted == 0 || p.Deleted == null)
                      && p.Positive != false
                      && t.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.EntityAnalysisModel
                          .TenantRegistryId == tenantRegistryId
                orderby p.Id
                select new
                {
                    VariableId = t.ExhaustiveSearchInstanceVariableId.GetValueOrDefault(), PromotedId = p.Id, p.Mean,
                    p.StandardDeviation, p.Minimum, p.Maximum, p.Iqr
                }).ToListAsync(token).ConfigureAwait(false);

            if (promoted.Count == 0)
            {
                return;
            }

            var promotedById = new Dictionary<int, int>();

            foreach (var row in promoted)
            {
                promotedById[row.PromotedId] = row.VariableId;

                byId[row.VariableId].Prescription =
                    BuildSummary(row.Mean, row.StandardDeviation, row.Minimum, row.Maximum, row.Iqr);
            }

            var promotedIds = promotedById.Keys.ToList();

            var bins = await dbContext.ExhaustiveSearchInstancePrescriptionHistogram
                .Where(w => promotedIds.Contains(
                    w.ExhaustiveSearchInstancePromotedTrialInstanceVariableId.GetValueOrDefault()))
                .OrderBy(o => o.BinIndex)
                .ToListAsync(token).ConfigureAwait(false);

            foreach (var row in bins)
            {
                byId[promotedById[row.ExhaustiveSearchInstancePromotedTrialInstanceVariableId.GetValueOrDefault()]]
                    .PrescriptionBins.Add(Bin(row.BinRangeStart, row.BinRangeEnd, row.Frequency));
            }
        }

        private static HistogramBin Bin(double? start, double? end, int? frequency)
        {
            return new HistogramBin
            {
                Start = start.GetValueOrDefault(), End = end.GetValueOrDefault(),
                Frequency = frequency.GetValueOrDefault()
            };
        }

        private static Summary BuildSummary(double? mean, double? standardDeviation, double? minimum,
            double? maximum, double? iqr)
        {
            return new Summary
            {
                Mean = mean.GetValueOrDefault(), StandardDeviation = standardDeviation.GetValueOrDefault(),
                Minimum = minimum.GetValueOrDefault(), Maximum = maximum.GetValueOrDefault(),
                Iqr = iqr.GetValueOrDefault()
            };
        }
    }
}