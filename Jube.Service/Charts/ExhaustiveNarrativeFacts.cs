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

using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotionHistory;

namespace Jube.Service.Charts
{
    public sealed record VariableNarrativeFact(
        string Name,
        double Sensitivity,
        double? PositiveMean,
        double? PositiveMinimum,
        double? PositiveMaximum,
        double? NegativeMean,
        double? NegativeMinimum,
        double? NegativeMaximum);

    public sealed record ScoreDistributionFacts(
        double PositiveMean,
        double NegativeMean,
        double PositiveStandardDeviation,
        double NegativeStandardDeviation,
        double SeparationInPooledStandardDeviations);

    public sealed record GainsFacts(double TopDecileCaptureShare, double TopDecileLift);

    public sealed record LiftFacts(IReadOnlyList<double> DecileLifts, int EffectiveDeciles);

    public sealed record LearningCurveFacts(
        int PromotionCount,
        int TrialInstancesToDate,
        double FirstScore,
        double LastScore,
        bool? StillImproving);

    public sealed record ExhaustiveNarrativeFacts(
        PerformanceMeasures? Performance,
        bool Calibrated,
        string? CalibrationStatus,
        int? TopologyComplexity,
        double? Score,
        IReadOnlyList<VariableNarrativeFact> Variables,
        ScoreDistributionFacts? ScoreDistribution,
        GainsFacts? Gains,
        LiftFacts? Lift,
        LearningCurveFacts? LearningCurve)
    {
        public static ExhaustiveNarrativeFacts Build(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> predictedActual,
            ExhaustiveSearchInstancePromotedTrialInstanceQueryDto? active,
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto> prescriptions,
            IReadOnlyList<ExhaustiveSearchInstancePromotionHistoryDto>? promotionHistory = null)
        {
            var performance = ExhaustivePerformanceRenderer.Measures(predictedActual);

            var variables = prescriptions
                .GroupBy(p => p.Name)
                .Select(group =>
                {
                    var positive = group.FirstOrDefault(p => p.Positive == true);
                    var negative = group.FirstOrDefault(p => p.Positive == false);
                    var sensitivity = group.Select(p => p.Sensitivity).FirstOrDefault();

                    return new VariableNarrativeFact(
                        group.Key ?? string.Empty,
                        sensitivity,
                        positive?.PrescriptionMean,
                        positive?.PrescriptionMinimum,
                        positive?.PrescriptionMaximum,
                        negative?.PrescriptionMean,
                        negative?.PrescriptionMinimum,
                        negative?.PrescriptionMaximum);
                })
                .OrderByDescending(v => v.Sensitivity)
                .ToList();

            return new ExhaustiveNarrativeFacts(
                performance,
                active?.CalibrationStatus == "Calibrated" || active?.CalibrationStatus == "CalibratedLowSample",
                active?.CalibrationStatus,
                active?.TopologyComplexity,
                active?.Score,
                variables,
                BuildScoreDistribution(predictedActual),
                BuildGains(predictedActual),
                BuildLift(predictedActual),
                BuildLearningCurve(promotionHistory));
        }

        private static ScoreDistributionFacts? BuildScoreDistribution(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows)
        {
            var positive = rows.Where(r => r.Actual >= 0.5 && double.IsFinite(r.Predicted)).Select(r => r.Predicted)
                .ToList();

            var negative = rows.Where(r => r.Actual < 0.5 && double.IsFinite(r.Predicted)).Select(r => r.Predicted)
                .ToList();

            if (positive.Count < 2 || negative.Count < 2)
            {
                return null;
            }

            var positiveMean = positive.Average();
            var negativeMean = negative.Average();
            var positiveSd = StandardDeviation(positive, positiveMean);
            var negativeSd = StandardDeviation(negative, negativeMean);
            var pooledSd = Math.Sqrt((positiveSd * positiveSd + negativeSd * negativeSd) / 2);
            var separation = pooledSd > 0 ? Math.Abs(positiveMean - negativeMean) / pooledSd : 0;

            return new ScoreDistributionFacts(positiveMean, negativeMean, positiveSd, negativeSd, separation);
        }

        private static double StandardDeviation(IReadOnlyList<double> values, double mean)
        {
            return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1));
        }

        private static GainsFacts? BuildGains(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows)
        {
            var ordered = rows.Where(r => double.IsFinite(r.Predicted)).OrderByDescending(r => r.Predicted).ToList();
            var totalPositives = ordered.Count(r => r.Actual >= 0.5);

            if (ordered.Count < 10 || totalPositives == 0)
            {
                return null;
            }

            var topDecileCount = Math.Max(1, ordered.Count / 10);
            var topDecilePositives = ordered.Take(topDecileCount).Count(r => r.Actual >= 0.5);
            var captureShare = topDecilePositives / (double)totalPositives;
            var overallRate = totalPositives / (double)ordered.Count;
            var topDecileRate = topDecilePositives / (double)topDecileCount;
            var lift = overallRate > 0 ? topDecileRate / overallRate : 0;

            return new GainsFacts(captureShare, lift);
        }

        private static LiftFacts? BuildLift(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows)
        {
            var ordered = rows.Where(r => double.IsFinite(r.Predicted)).OrderByDescending(r => r.Predicted).ToList();
            var totalPositives = ordered.Count(r => r.Actual >= 0.5);

            if (ordered.Count < 10 || totalPositives == 0)
            {
                return null;
            }

            var overallRate = totalPositives / (double)ordered.Count;
            var decileSize = ordered.Count / 10.0;
            var lifts = new List<double>();

            for (var decile = 0; decile < 10; decile++)
            {
                var start = (int)Math.Floor(decile * decileSize);
                var end = decile == 9 ? ordered.Count : (int)Math.Floor((decile + 1) * decileSize);
                var slice = ordered.Skip(start).Take(end - start).ToList();

                if (slice.Count == 0)
                {
                    lifts.Add(0);
                    continue;
                }

                var rate = slice.Count(r => r.Actual >= 0.5) / (double)slice.Count;
                lifts.Add(overallRate > 0 ? rate / overallRate : 0);
            }

            var effectiveDeciles = 0;

            foreach (var decileLift in lifts)
            {
                if (decileLift < 1)
                {
                    break;
                }

                effectiveDeciles++;
            }

            return new LiftFacts(lifts, effectiveDeciles);
        }

        private static LearningCurveFacts? BuildLearningCurve(
            IReadOnlyList<ExhaustiveSearchInstancePromotionHistoryDto>? promotionHistory)
        {
            if (promotionHistory is null || promotionHistory.Count == 0)
            {
                return null;
            }

            var ordered = promotionHistory.OrderBy(p => p.Id).ToList();
            var first = ordered[0];
            var last = ordered[^1];
            bool? stillImproving = null;

            if (ordered.Count >= 3)
            {
                var recent = ordered.TakeLast(3).Select(p => p.Score).ToList();
                stillImproving = recent[^1] > recent[0] + 0.001;
            }

            return new LearningCurveFacts(ordered.Count, last.TrialInstancesToDate, first.Score, last.Score,
                stillImproving);
        }
    }
}