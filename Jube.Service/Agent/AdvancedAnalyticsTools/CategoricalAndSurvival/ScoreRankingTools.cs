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
using Jube.Analytics;
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    [AdvancedAnalyticsToolGroup("CategoricalAndSurvival")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ScoreRankingTools
    {
        private const int MaximumLiftGroups = 1000;

        [AdvancedAnalyticsTool("ComputeRocCurve")]
        [Description(
            "Computes the ROC curve of a score or probability against a 0/1 outcome, with the area under it (AUC) and its DeLong standard error and confidence interval. " +
            "Each point gives a threshold (score at or above it is flagged), the false positive rate and the true positive rate; the first point, with a NaN threshold, is the origin where nothing is flagged. " +
            "AUC is the chance that a random event scores above a random non-event: 0.5 is no skill, 1 is perfect, below 0.5 means the score is inverted. Gini is 2 AUC minus 1. The result also gives the Kolmogorov-Smirnov maximum gap and the Youden-optimal point. " +
            "Rows with a NaN score are dropped; both classes must be present. Use maxPoints to thin a long curve for display; AUC always uses every point.")]
        public RocCurveResult ComputeRocCurve(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Confidence level for the AUC interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "Maximum number of curve points to return; 0 (default) returns every point. Thinning keeps the first and last points.")]
            int maxPoints = 0)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (maxPoints < 0)
            {
                throw new ArgumentException("maxPoints cannot be negative; use 0 for every point.", nameof(maxPoints));
            }

            var curve = BinaryMetrics.RocCurve(s, y);
            var auc = BinaryMetrics.RocAucWithStandardError(s, y);
            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);
            var best = -1;
            var bestJ = double.NegativeInfinity;
            var ksBest = -1;
            var ksValue = double.NegativeInfinity;

            for (var i = 0; i < curve.Length; i++)
            {
                var j = curve[i].TruePositiveRate - curve[i].FalsePositiveRate;

                if (j > bestJ)
                {
                    bestJ = j;
                    best = i;
                }

                var gap = Math.Abs(j);

                if (gap > ksValue)
                {
                    ksValue = gap;
                    ksBest = i;
                }
            }

            var points = curve.Select(p => new RocCurvePoint
            {
                Threshold = double.IsInfinity(p.Threshold) ? double.NaN : p.Threshold,
                FalsePositiveRate = p.FalsePositiveRate,
                TruePositiveRate = p.TruePositiveRate
            }).ToArray();

            return new RocCurveResult
            {
                Points = Thin(points, maxPoints),
                TotalPoints = points.Length,
                Auc = auc.Auc,
                AucStandardError = auc.StandardError,
                AucLower = Math.Max(0d, auc.Auc - z * auc.StandardError),
                AucUpper = Math.Min(1d, auc.Auc + z * auc.StandardError),
                Gini = 2d * auc.Auc - 1d,
                KolmogorovSmirnov = ksValue,
                KolmogorovSmirnovThreshold = points[ksBest].Threshold,
                YoudenJ = bestJ,
                YoudenThreshold = points[best].Threshold,
                YoudenSensitivity = curve[best].TruePositiveRate,
                YoudenSpecificity = 1d - curve[best].FalsePositiveRate,
                Positives = y.Count(l => l == 1),
                Negatives = y.Count(l => l == 0),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("ComparePairedRocAucDelong")]
        [Description(
            "Compares the AUC of two different scores evaluated on the same cases, using the paired DeLong test that accounts for the correlation between the two scores. " +
            "Returns both AUCs, their difference (first minus second) with standard error, confidence interval, z statistic and two-sided p-value; a small p-value means one score genuinely ranks better. " +
            "Rows where either score is NaN are dropped. Use it to decide whether a new model beats the incumbent on a shared validation set. Do not use it for scores measured on different cases.")]
        public PairedRocComparisonResult ComparePairedRocAucDelong(
            [Description("First score for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scoresFirst,
            [Description("Second score for the same cases in the same order and length as scoresFirst.")]
            double[] scoresSecond,
            [Description("True outcome per case: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Confidence level for the difference interval, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CategoricalGuards.Required(scoresFirst, nameof(scoresFirst));
            CategoricalGuards.Required(scoresSecond, nameof(scoresSecond));

            CategoricalGuards.SameLength(scoresFirst.Length, scoresSecond.Length, nameof(scoresFirst),
                nameof(scoresSecond));

            CategoricalGuards.BinaryLabels(labels, nameof(labels));
            CategoricalGuards.SameLength(scoresFirst.Length, labels.Length, nameof(scoresFirst), nameof(labels));
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            var keep = Enumerable.Range(0, labels.Length)
                .Where(i => !double.IsNaN(scoresFirst[i]) && !double.IsNaN(scoresSecond[i])).ToArray();

            if (keep.Length == 0)
            {
                throw new ArgumentException("There are no rows where both scores are present.", nameof(scoresFirst));
            }

            var a = keep.Select(i => scoresFirst[i]).ToArray();
            var b = keep.Select(i => scoresSecond[i]).ToArray();
            var y = keep.Select(i => labels[i]).ToArray();
            ScoreGuards.BothClasses(y);
            var positives = Enumerable.Range(0, y.Length).Where(i => y[i] == 1).ToArray();
            var negatives = Enumerable.Range(0, y.Length).Where(i => y[i] == 0).ToArray();
            var m = positives.Length;
            var n = negatives.Length;
            var (v10A, v01A) = Placements(a, positives, negatives);
            var (v10B, v01B) = Placements(b, positives, negatives);
            var aucA = v10A.Average();
            var aucB = v10B.Average();
            var difference = aucA - aucB;
            var se = double.NaN;

            if (m >= 2 && n >= 2)
            {
                var s10 = Covariance(v10A, v10B, m);
                var s01 = Covariance(v01A, v01B, n);
                var variance = (s10.AA + s10.BB - 2d * s10.AB) / m + (s01.AA + s01.BB - 2d * s01.AB) / n;
                se = Math.Sqrt(Math.Max(0d, variance));
            }

            var z = CategoricalGuards.TwoSidedZ(confidenceLevel);
            var statistic = se > 0d ? difference / se : double.NaN;

            return new PairedRocComparisonResult
            {
                AucFirst = aucA,
                AucSecond = aucB,
                Difference = difference,
                StandardError = se,
                DifferenceLower = difference - z * se,
                DifferenceUpper = difference + z * se,
                ZStatistic = statistic,
                PValue = double.IsNaN(statistic)
                    ? double.NaN
                    : 2d * CategoricalLibrary.NormalSurvival(Math.Abs(statistic)),
                CorrelationOfScores = Pearson(a, b),
                Positives = m,
                Negatives = n,
                SkippedRows = labels.Length - keep.Length
            };
        }

        [AdvancedAnalyticsTool("ComputePrecisionRecallCurve")]
        [Description(
            "Computes the precision-recall curve of a score against a 0/1 outcome together with the average precision. " +
            "Each point gives a threshold (score at or above it is flagged), the precision (share of flagged cases that are events) and the recall (share of events flagged). " +
            "AveragePrecision is the step-wise area under the curve and should be compared with Prevalence, the precision of flagging everything; it is far more informative than AUC when events are rare, as in fraud and AML. BestF1 and its threshold are also returned. " +
            "Rows with a NaN score are dropped; at least one event is needed. Use maxPoints to thin the returned curve.")]
        public PrecisionRecallCurveResult ComputePrecisionRecallCurve(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Maximum number of curve points to return; 0 (default) returns every point.")]
            int maxPoints = 0)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);

            if (y.All(l => l == 0))
            {
                throw new ArgumentException(
                    "The outcome has no events among the usable rows, so precision-recall is undefined.",
                    nameof(labels));
            }

            if (maxPoints < 0)
            {
                throw new ArgumentException("maxPoints cannot be negative; use 0 for every point.", nameof(maxPoints));
            }

            var curve = BinaryMetrics.PrecisionRecallCurve(s, y);
            var ap = BinaryMetrics.AveragePrecision(s, y);
            var bestF1 = double.NegativeInfinity;
            var bestThreshold = double.NaN;

            foreach (var p in curve)
            {
                var f = p.Precision + p.Recall > 0d ? 2d * p.Precision * p.Recall / (p.Precision + p.Recall) : 0d;

                if (f > bestF1)
                {
                    bestF1 = f;
                    bestThreshold = p.Threshold;
                }
            }

            var points = curve.Select(p => new PrecisionRecallCurvePoint
            {
                Threshold = p.Threshold,
                Precision = p.Precision,
                Recall = p.Recall
            }).ToArray();

            var positives = y.Count(l => l == 1);

            return new PrecisionRecallCurveResult
            {
                Points = Thin(points, maxPoints),
                TotalPoints = points.Length,
                AveragePrecision = ap,
                Prevalence = positives / (double)y.Length,
                BestF1 = bestF1,
                BestF1Threshold = bestThreshold,
                Positives = positives,
                Negatives = y.Length - positives,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("ComputeLiftGainTable")]
        [Description(
            "Builds the classic decile (or n-group) lift and gain table: cases are ranked by score from highest to lowest and cut into equal-sized groups, with group 1 holding the highest scores. " +
            "For each group it returns the score range, count, events, event rate, lift (group rate over overall rate), the cumulative event rate and lift, cumulative gain (share of all events captured so far) and the Kolmogorov-Smirnov gap between the cumulative event and non-event shares. " +
            "Read it as 'reviewing the top 20 percent of cases finds X percent of events'. Ties at a group boundary are split by input order. Rows with a NaN score are dropped; both classes are needed.")]
        public LiftGainResult ComputeLiftGainTable(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description(
                "Number of equal-sized groups, from 2 to 1000. Default 10 (deciles); use 20 for ventiles or 100 for percentiles.")]
            int groups = 10)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);

            if (groups < 2 || groups > MaximumLiftGroups)
            {
                throw new ArgumentException($"groups must be between 2 and {MaximumLiftGroups}.", nameof(groups));
            }

            if (groups > s.Length)
            {
                throw new ArgumentException(
                    $"There are only {s.Length} usable rows, fewer than the {groups} groups requested.",
                    nameof(groups));
            }

            var order = Enumerable.Range(0, s.Length).OrderByDescending(i => s[i]).ThenBy(i => i).ToArray();
            var totalEvents = y.Count(l => l == 1);
            var totalNonEvents = y.Length - totalEvents;
            var overall = totalEvents / (double)y.Length;
            var rows = new List<LiftGainRow>();
            var position = 0;
            var cumulativeEvents = 0;
            var cumulativeCount = 0;
            var maxKs = double.NegativeInfinity;
            var maxKsGroup = 0;

            for (var g = 0; g < groups; g++)
            {
                var size = y.Length / groups + (g < y.Length % groups ? 1 : 0);
                var events = 0;
                var top = double.NaN;
                var bottom = double.NaN;

                for (var k = 0; k < size; k++)
                {
                    var index = order[position++];

                    if (k == 0)
                    {
                        top = s[index];
                    }

                    bottom = s[index];
                    events += y[index];
                }

                cumulativeEvents += events;
                cumulativeCount += size;
                var cumulativeNonEventShare = (cumulativeCount - cumulativeEvents) / (double)totalNonEvents;
                var gain = cumulativeEvents / (double)totalEvents;
                var ks = Math.Abs(gain - cumulativeNonEventShare);

                if (ks > maxKs)
                {
                    maxKs = ks;
                    maxKsGroup = g + 1;
                }

                rows.Add(new LiftGainRow
                {
                    Group = g + 1,
                    ScoreMax = top,
                    ScoreMin = bottom,
                    Count = size,
                    Events = events,
                    EventRate = events / (double)size,
                    Lift = events / (double)size / overall,
                    CumulativeCount = cumulativeCount,
                    CumulativeEvents = cumulativeEvents,
                    CumulativeEventRate = cumulativeEvents / (double)cumulativeCount,
                    CumulativeLift = cumulativeEvents / (double)cumulativeCount / overall,
                    CumulativeGain = gain,
                    CumulativeNonEventShare = cumulativeNonEventShare,
                    KolmogorovSmirnov = ks
                });
            }

            return new LiftGainResult
            {
                Groups = rows.ToArray(),
                TotalRows = y.Length,
                TotalEvents = totalEvents,
                OverallEventRate = overall,
                MaximumKolmogorovSmirnov = maxKs,
                MaximumKolmogorovSmirnovGroup = maxKsGroup,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("ComputeKolmogorovSmirnovSeparation")]
        [Description(
            "Measures how well a score separates events from non-events with the Kolmogorov-Smirnov statistic, the largest gap between the cumulative score distributions of the two classes. " +
            "Returns the KS value (0 is no separation, 1 is perfect), the threshold at which the gap is largest with the sensitivity and false positive rate there, AUC and Gini, the mean and median score of each class. Use the two-sample Kolmogorov-Smirnov test from the hypothesis-testing tools if you need a p-value. " +
            "KS around 0.2 to 0.4 is typical of a usable credit or fraud score; above about 0.7 check for leakage. Rows with a NaN score are dropped; both classes are required.")]
        public KolmogorovSmirnovSeparationResult ComputeKolmogorovSmirnovSeparation(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            var sweep = ScoreSweep.Build(s, y);
            var best = 0;
            var bestGap = double.NegativeInfinity;

            for (var i = 0; i < sweep.Length; i++)
            {
                var gap = Math.Abs(sweep.TruePositives[i] / (double)sweep.Positives -
                                   sweep.FalsePositives[i] / (double)sweep.Negatives);

                if (gap > bestGap)
                {
                    bestGap = gap;
                    best = i;
                }
            }

            var auc = BinaryMetrics.RocAuc(s, y);
            var eventScores = s.Where((_, i) => y[i] == 1).OrderBy(v => v).ToArray();
            var nonEventScores = s.Where((_, i) => y[i] == 0).OrderBy(v => v).ToArray();

            return new KolmogorovSmirnovSeparationResult
            {
                KolmogorovSmirnov = bestGap,
                Threshold = sweep.Thresholds[best],
                SensitivityAtThreshold = sweep.TruePositives[best] / (double)sweep.Positives,
                FalsePositiveRateAtThreshold = sweep.FalsePositives[best] / (double)sweep.Negatives,
                Auc = auc,
                Gini = 2d * auc - 1d,
                MeanScoreEvents = eventScores.Average(),
                MeanScoreNonEvents = nonEventScores.Average(),
                MedianScoreEvents = CategoricalNumerics.Median(eventScores),
                MedianScoreNonEvents = CategoricalNumerics.Median(nonEventScores),
                Positives = sweep.Positives,
                Negatives = sweep.Negatives,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("ComputeThresholdSweep")]
        [Description(
            "Evaluates a score at many cut-off thresholds at once and returns one row per threshold with the confusion counts, alert rate (share of cases flagged), precision, recall, specificity, F1, F-beta, Youden J, Matthews correlation and, when costs are given, the expected cost per case. " +
            "Give explicit thresholds to inspect specific cut-offs, or leave them empty to get up to maxThresholds cut-offs spread evenly across the distinct scores. A case is flagged when its score is at or above the threshold. " +
            "Use it to see the trade-off curve before choosing an operating point; the SelectThreshold tools pick one automatically.")]
        public ThresholdSweepResult ComputeThresholdSweep(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Optional explicit thresholds to evaluate; leave empty to pick them automatically.")]
            double[]? thresholds = null,
            [Description("Number of automatic thresholds when thresholds is empty, from 2 to 1000. Default 20.")]
            int maxThresholds = 20,
            [Description("F-beta weight, above 0: 1 balances precision and recall, 2 favours recall. Default 1.")]
            double beta = 1d,
            [Description(
                "Cost of a missed event (false negative); give together with costFalsePositive to get expected costs.")]
            double? costFalseNegative = null,
            [Description("Cost of a false alert (false positive); give together with costFalseNegative.")]
            double? costFalsePositive = null)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            ClassifierMath.CheckBeta(beta);
            ClassifierMath.CheckCosts(costFalseNegative, costFalsePositive);

            if (maxThresholds < 2 || maxThresholds > 1000)
            {
                throw new ArgumentException("maxThresholds must be between 2 and 1000.", nameof(maxThresholds));
            }

            var sweep = ScoreSweep.Build(s, y);
            double[] chosen;

            if (thresholds is { Length: > 0 })
            {
                if (thresholds.Any(double.IsNaN))
                {
                    throw new ArgumentException("thresholds cannot contain NaN.", nameof(thresholds));
                }

                chosen = thresholds.OrderByDescending(t => t).ToArray();
            }
            else if (sweep.Length <= maxThresholds)
            {
                chosen = sweep.Thresholds;
            }
            else
            {
                chosen = Enumerable.Range(0, maxThresholds)
                    .Select(k => sweep.Thresholds[(int)Math.Round(k * (sweep.Length - 1d) / (maxThresholds - 1d))])
                    .Distinct().ToArray();
            }

            var rows = new List<ThresholdSweepRow>();

            foreach (var threshold in chosen)
            {
                var (tp, fp) = sweep.CountsAt(threshold);
                var fn = sweep.Positives - tp;
                var tn = sweep.Negatives - fp;
                var matrix = new ConfusionMatrix(tp, fp, tn, fn);

                rows.Add(new ThresholdSweepRow
                {
                    Threshold = threshold,
                    TruePositives = tp,
                    FalsePositives = fp,
                    TrueNegatives = tn,
                    FalseNegatives = fn,
                    AlertRate = (tp + fp) / (double)sweep.Total,
                    Precision = matrix.Precision,
                    Recall = matrix.Recall,
                    Specificity = matrix.Specificity,
                    F1 = matrix.F1,
                    FBeta = matrix.FBeta(beta),
                    YoudenJ = matrix.Recall + matrix.Specificity - 1d,
                    MatthewsCorrelation = matrix.Mcc,
                    ExpectedCostPerCase = costFalseNegative.HasValue
                        ? matrix.ExpectedCost(new CostMatrix(costFalseNegative.Value, costFalsePositive!.Value))
                        : double.NaN
                });
            }

            return new ThresholdSweepResult
            {
                Rows = rows.ToArray(),
                Positives = sweep.Positives,
                Negatives = sweep.Negatives,
                Beta = beta,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("SelectThresholdMaximisingFBeta")]
        [Description(
            "Finds the score threshold that maximises the F-beta measure, the weighted harmonic mean of precision and recall (beta 1 for F1, above 1 to favour recall, below 1 to favour precision). " +
            "A case is flagged when its score is at or above the returned threshold. It returns the threshold, the achieved F-beta and the full confusion metrics at that point. Ties choose the highest threshold, which flags the fewest cases. " +
            "Rows with a NaN score are dropped; both classes are required. F-beta ignores true negatives, which suits rare-event problems but not cost-sensitive ones (use SelectThresholdMinimisingCost for those).")]
        public ThresholdSelectionResult SelectThresholdMaximisingFBeta(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description(
                "F-beta weight, above 0: 1 (default) balances precision and recall, 2 favours recall, 0.5 favours precision.")]
            double beta = 1d,
            [Description(
                "Confidence level for the intervals in the returned metrics, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            ClassifierMath.CheckBeta(beta);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var sweep = ScoreSweep.Build(s, y);
            var best = ArgMax(sweep.Length, i => sweep.F(i, beta));

            return Selection($"maximum F-beta (beta {beta})", sweep, best, sweep.F(best, beta), true, string.Empty,
                beta,
                confidenceLevel, null, null, skipped);
        }

        [AdvancedAnalyticsTool("SelectThresholdMaximisingYoudenIndex")]
        [Description(
            "Finds the score threshold that maximises Youden's J statistic, sensitivity plus specificity minus one, the point on the ROC curve furthest above the chance diagonal. " +
            "A case is flagged when its score is at or above the returned threshold. It treats missing an event and raising a false alert as equally costly per class regardless of how rare events are, so it can flag many cases when events are rare. Ties choose the highest threshold. " +
            "Returns the threshold, the achieved J and the full confusion metrics. Rows with a NaN score are dropped; both classes are required.")]
        public ThresholdSelectionResult SelectThresholdMaximisingYoudenIndex(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description(
                "Confidence level for the intervals in the returned metrics, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var sweep = ScoreSweep.Build(s, y);
            var best = ArgMax(sweep.Length, sweep.Youden);

            return Selection("maximum Youden J", sweep, best, sweep.Youden(best), true, string.Empty, 1d,
                confidenceLevel, null, null, skipped);
        }

        [AdvancedAnalyticsTool("SelectThresholdMaximisingMatthewsCorrelation")]
        [Description(
            "Finds the score threshold that maximises the Matthews correlation coefficient, a balanced single-number summary of the whole confusion matrix that stays honest when events are rare. " +
            "A case is flagged when its score is at or above the returned threshold. Ties choose the highest threshold. Returns the threshold, the achieved MCC (from -1 to 1) and the full confusion metrics. " +
            "Rows with a NaN score are dropped; both classes are required.")]
        public ThresholdSelectionResult SelectThresholdMaximisingMatthewsCorrelation(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description(
                "Confidence level for the intervals in the returned metrics, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var sweep = ScoreSweep.Build(s, y);
            var best = ArgMax(sweep.Length, sweep.Mcc);

            return Selection("maximum Matthews correlation", sweep, best, sweep.Mcc(best), true, string.Empty, 1d,
                confidenceLevel, null, null, skipped);
        }

        [AdvancedAnalyticsTool("SelectThresholdMinimisingCost")]
        [Description(
            "Finds the score threshold that minimises the total expected cost per case given what a missed event (false negative) and a false alert (false positive) each cost, with optional costs for true positives (for example review cost) and true negatives. " +
            "A case is flagged when its score is at or above the returned threshold. Flagging nothing is also considered, and if it is cheapest the threshold returned is just above the highest score and the note says so. " +
            "It also returns the theoretical Bayes-optimal probability threshold, cost of a false positive over the sum of both misclassification costs, which is only meaningful when the scores are calibrated probabilities, and the cost of the flag-nothing and flag-everything policies for comparison. Ties choose the highest threshold. Rows with a NaN score are dropped.")]
        public ThresholdSelectionResult SelectThresholdMinimisingCost(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Cost of missing an event (false negative), above 0.")]
            double costFalseNegative,
            [Description("Cost of a false alert (false positive), above 0.")]
            double costFalsePositive,
            [Description("Cost incurred for a correctly flagged event, for example the review cost. Default 0.")]
            double costTruePositive = 0d,
            [Description("Cost incurred for a correctly cleared non-event. Default 0.")]
            double costTrueNegative = 0d,
            [Description(
                "Confidence level for the intervals in the returned metrics, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var costs = new CostMatrix(costFalseNegative, costFalsePositive, costTruePositive, costTrueNegative);
            double bayes;

            try
            {
                bayes = costs.MinimumRiskThreshold();
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(exception.Message, nameof(costFalseNegative));
            }

            var sweep = ScoreSweep.Build(s, y);

            double CostAt(int tp, int fp)
            {
                return new ConfusionMatrix(tp, fp, sweep.Negatives - fp, sweep.Positives - tp).ExpectedCost(costs);
            }

            var best = -1;
            var bestCost = CostAt(0, 0);

            for (var i = 0; i < sweep.Length; i++)
            {
                var cost = CostAt(sweep.TruePositives[i], sweep.FalsePositives[i]);

                if (cost < bestCost - 1e-12 * Math.Max(1d, Math.Abs(bestCost)))
                {
                    bestCost = cost;
                    best = i;
                }
            }

            var flagNone = CostAt(0, 0);
            var flagAll = CostAt(sweep.Positives, sweep.Negatives);

            if (best < 0)
            {
                var above = Math.BitIncrement(sweep.Thresholds[0]);

                var none = ClassifierMath.Metrics(0, 0, sweep.Negatives, sweep.Positives, 1d, confidenceLevel, above,
                    costFalseNegative, costFalsePositive);

                return new ThresholdSelectionResult
                {
                    Objective = "minimum expected cost per case",
                    Threshold = above,
                    ObjectiveValue = flagNone,
                    Achievable = true,
                    Note = "Flagging no cases is the cheapest policy for these costs: no threshold beats it.",
                    Metrics = none,
                    BayesOptimalThreshold = bayes,
                    ExpectedCostFlagNone = flagNone,
                    ExpectedCostFlagAll = flagAll,
                    SkippedRows = skipped
                };
            }

            var result = Selection("minimum expected cost per case", sweep, best, bestCost, true, string.Empty, 1d,
                confidenceLevel, costFalseNegative, costFalsePositive, skipped);

            return new ThresholdSelectionResult
            {
                Objective = result.Objective,
                Threshold = result.Threshold,
                ObjectiveValue = bestCost,
                Achievable = true,
                Note = result.Note,
                Metrics = result.Metrics,
                BayesOptimalThreshold = bayes,
                ExpectedCostFlagNone = flagNone,
                ExpectedCostFlagAll = flagAll,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("SelectThresholdForTargetRecall")]
        [Description(
            "Finds the highest score threshold, and therefore the fewest alerts, that still catches at least a target share of the events (recall). " +
            "A case is flagged when its score is at or above the returned threshold. Use it when a detection rate is mandated, for example catching 90 percent of known fraud, and you want to know the alert volume and precision that costs. " +
            "Returns the threshold, the achieved recall, the alert rate and precision inside the confusion metrics. Rows with a NaN score are dropped; both classes are required.")]
        public ThresholdSelectionResult SelectThresholdForTargetRecall(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Minimum recall required, above 0 and at most 1, for example 0.9.")]
            double targetRecall,
            [Description(
                "Confidence level for the intervals in the returned metrics, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (double.IsNaN(targetRecall) || targetRecall <= 0d || targetRecall > 1d)
            {
                throw new ArgumentException("targetRecall must be above 0 and at most 1.", nameof(targetRecall));
            }

            var sweep = ScoreSweep.Build(s, y);
            var chosen = -1;

            for (var i = 0; i < sweep.Length; i++)
            {
                if (sweep.TruePositives[i] / (double)sweep.Positives >= targetRecall - 1e-12)
                {
                    chosen = i;
                    break;
                }
            }

            return Selection($"highest threshold reaching recall {targetRecall}", sweep, chosen,
                sweep.TruePositives[chosen] / (double)sweep.Positives, true, string.Empty, 1d, confidenceLevel, null,
                null, skipped);
        }

        [AdvancedAnalyticsTool("SelectThresholdForTargetPrecision")]
        [Description(
            "Finds the lowest score threshold, and therefore the most events caught, that keeps precision (the share of alerts that are real events) at or above a target. " +
            "A case is flagged when its score is at or above the returned threshold. Use it when analysts can only tolerate a certain false alert share. " +
            "If precision never reaches the target at any threshold, Achievable is false and the threshold with the highest precision is returned with an explanatory note. Rows with a NaN score are dropped; both classes are required.")]
        public ThresholdSelectionResult SelectThresholdForTargetPrecision(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Minimum precision required, above 0 and at most 1, for example 0.5.")]
            double targetPrecision,
            [Description(
                "Confidence level for the intervals in the returned metrics, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (double.IsNaN(targetPrecision) || targetPrecision <= 0d || targetPrecision > 1d)
            {
                throw new ArgumentException("targetPrecision must be above 0 and at most 1.", nameof(targetPrecision));
            }

            var sweep = ScoreSweep.Build(s, y);
            var chosen = -1;
            var highest = 0;
            var highestPrecision = double.NegativeInfinity;

            for (var i = 0; i < sweep.Length; i++)
            {
                var precision = sweep.TruePositives[i] / (double)(sweep.TruePositives[i] + sweep.FalsePositives[i]);

                if (precision >= targetPrecision - 1e-12)
                {
                    chosen = i;
                }

                if (precision > highestPrecision)
                {
                    highestPrecision = precision;
                    highest = i;
                }
            }

            if (chosen < 0)
            {
                return Selection($"lowest threshold keeping precision at {targetPrecision}", sweep, highest,
                    highestPrecision, false,
                    "No threshold reaches the target precision; the returned point has the highest precision available.",
                    1d, confidenceLevel, null, null, skipped);
            }

            var achieved = sweep.TruePositives[chosen] /
                           (double)(sweep.TruePositives[chosen] + sweep.FalsePositives[chosen]);

            return Selection($"lowest threshold keeping precision at {targetPrecision}", sweep, chosen, achieved, true,
                string.Empty, 1d, confidenceLevel, null, null, skipped);
        }

        [AdvancedAnalyticsTool("SelectThresholdForAlertBudget")]
        [Description(
            "Finds the lowest score threshold whose number of alerts stays within a capacity budget, given either as a share of all cases (alertFraction) or as a count (alertCount); give exactly one. " +
            "A case is flagged when its score is at or above the returned threshold. Use it when analyst capacity limits how many cases can be reviewed and you need the precision and recall obtained under that limit. " +
            "Cases with tied scores are flagged together, so the realised alerts may be below the budget; if the very top score is tied across more cases than the budget allows, Achievable is false and the smallest possible alert set is returned. Rows with a NaN score are dropped; both classes are required.")]
        public ThresholdSelectionResult SelectThresholdForAlertBudget(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description(
                "Maximum share of cases that may be flagged, above 0 and at most 1. Give this or alertCount, not both.")]
            double? alertFraction = null,
            [Description(
                "Maximum number of cases that may be flagged, at least 1. Give this or alertFraction, not both.")]
            int? alertCount = null,
            [Description(
                "Confidence level for the intervals in the returned metrics, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            ScoreGuards.BothClasses(y);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (alertFraction.HasValue == alertCount.HasValue)
            {
                throw new ArgumentException("Give exactly one of alertFraction or alertCount.", nameof(alertFraction));
            }

            if (alertFraction.HasValue && (double.IsNaN(alertFraction.Value) || alertFraction.Value <= 0d ||
                                           alertFraction.Value > 1d))
            {
                throw new ArgumentException("alertFraction must be above 0 and at most 1.", nameof(alertFraction));
            }

            if (alertCount.HasValue && alertCount.Value < 1)
            {
                throw new ArgumentException("alertCount must be at least 1.", nameof(alertCount));
            }

            var sweep = ScoreSweep.Build(s, y);
            var budget = alertCount ?? (int)Math.Floor(alertFraction!.Value * sweep.Total + 1e-9);
            var chosen = -1;

            for (var i = 0; i < sweep.Length; i++)
            {
                if (sweep.TruePositives[i] + sweep.FalsePositives[i] <= budget)
                {
                    chosen = i;
                }
            }

            var objective = $"lowest threshold within an alert budget of {budget} cases";

            if (chosen < 0)
            {
                return Selection(objective, sweep, 0, sweep.TruePositives[0] / (double)sweep.Positives, false,
                    "The highest score is tied across more cases than the budget allows; the smallest possible alert set is returned.",
                    1d, confidenceLevel, null, null, skipped);
            }

            return Selection(objective, sweep, chosen, sweep.TruePositives[chosen] / (double)sweep.Positives, true,
                string.Empty, 1d, confidenceLevel, null, null, skipped);
        }

        [AdvancedAnalyticsTool("EvaluateTopKAlertQueue")]
        [Description(
            "Evaluates a ranked alert queue at several queue sizes: if analysts work the k highest-scoring cases, how many real events do they find? " +
            "For each requested size k it returns the true positives found, precision at k, recall at k (share of all events found), lift at k (precision over the overall event rate) and the lowest score included. " +
            "Ties at the cut are broken by input order. Sizes larger than the number of cases are rejected. Rows with a NaN score are dropped. At least one case is required; events may be absent, in which case precision is 0 and recall is NaN.")]
        public AlertQueueResult EvaluateTopKAlertQueue(
            [Description(
                "Model score or probability for each case, higher meaning more likely an event; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description(
                "Queue sizes to evaluate, each at least 1 and no more than the number of usable cases, for example 10, 50, 100.")]
            int[] queueSizes)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out var skipped);
            CategoricalGuards.Required(queueSizes, nameof(queueSizes));

            if (queueSizes.Length == 0)
            {
                throw new ArgumentException("Give at least one queue size.", nameof(queueSizes));
            }

            foreach (var k in queueSizes)
            {
                if (k < 1 || k > s.Length)
                {
                    throw new ArgumentException(
                        $"Every queue size must be between 1 and the number of usable cases ({s.Length}); {k} is not.",
                        nameof(queueSizes));
                }
            }

            var order = Enumerable.Range(0, s.Length).OrderByDescending(i => s[i]).ThenBy(i => i).ToArray();
            var cumulative = new int[order.Length + 1];

            for (var i = 0; i < order.Length; i++)
            {
                cumulative[i + 1] = cumulative[i] + y[order[i]];
            }

            var totalEvents = cumulative[order.Length];
            var overall = totalEvents / (double)s.Length;

            var rows = queueSizes.OrderBy(k => k).Distinct().Select(k => new AlertQueueRow
            {
                QueueSize = k,
                TruePositives = cumulative[k],
                Precision = cumulative[k] / (double)k,
                Recall = totalEvents > 0 ? cumulative[k] / (double)totalEvents : double.NaN,
                Lift = overall > 0d ? cumulative[k] / (double)k / overall : double.NaN,
                MinimumScoreInQueue = s[order[k - 1]]
            }).ToArray();

            return new AlertQueueResult
            {
                Rows = rows,
                TotalRows = s.Length,
                TotalEvents = totalEvents,
                OverallEventRate = overall,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CalculateOneVersusRestMulticlassAuc")]
        [Description(
            "Evaluates the ranking quality of a multi-class probability model with one-versus-rest AUC: for each class, how well its probability column separates that class from all others. " +
            "Returns the AUC for each class, the support of each class, the macro average (every class equal) and the support-weighted average. A class that never occurs, or that occurs in every row, has NaN AUC and is excluded from the averages. " +
            "Rows with any NaN probability are dropped. Probabilities need not sum to one for AUC, but each column should be higher for cases more likely in that class.")]
        public MulticlassAucResult CalculateOneVersusRestMulticlassAuc(
            [Description(
                "One array per case holding a score or probability for each class, columns in the same order as classNames.")]
            double[][] probabilities,
            [Description("Actual class label for each case, one of the entries of classNames.")]
            string[] actual,
            [Description("Class names in probability column order; each must be unique.")]
            string[] classNames)
        {
            CategoricalGuards.Required(probabilities, nameof(probabilities));
            CategoricalGuards.Required(actual, nameof(actual));
            CategoricalGuards.Required(classNames, nameof(classNames));
            CategoricalGuards.SameLength(probabilities.Length, actual.Length, nameof(probabilities), nameof(actual));

            if (classNames.Length < 2 || classNames.Distinct().Count() != classNames.Length)
            {
                throw new ArgumentException("Give at least two unique class names.", nameof(classNames));
            }

            for (var i = 0; i < probabilities.Length; i++)
            {
                if (probabilities[i] == null || probabilities[i].Length != classNames.Length)
                {
                    throw new ArgumentException(
                        $"Row {i} of probabilities must have {classNames.Length} entries, one per class.",
                        nameof(probabilities));
                }

                if (!classNames.Contains(actual[i]))
                {
                    throw new ArgumentException($"Actual class '{actual[i]}' in row {i} is not listed in classNames.",
                        nameof(actual));
                }
            }

            var keep = Enumerable.Range(0, probabilities.Length).Where(i => probabilities[i].All(v => !double.IsNaN(v)))
                .ToArray();

            if (keep.Length == 0)
            {
                throw new ArgumentException("There are no rows without NaN probabilities.", nameof(probabilities));
            }

            var aucs = new double[classNames.Length];
            var supports = new int[classNames.Length];

            for (var c = 0; c < classNames.Length; c++)
            {
                var column = keep.Select(i => probabilities[i][c]).ToArray();
                var binary = keep.Select(i => actual[i] == classNames[c] ? 1 : 0).ToArray();
                supports[c] = binary.Sum();
                aucs[c] = BinaryMetrics.RocAuc(column, binary);
            }

            var valid = Enumerable.Range(0, classNames.Length).Where(c => !double.IsNaN(aucs[c])).ToArray();

            return new MulticlassAucResult
            {
                Classes = classNames,
                AucPerClass = aucs,
                SupportPerClass = supports,
                MacroAuc = valid.Length > 0 ? valid.Average(c => aucs[c]) : double.NaN,
                WeightedAuc = valid.Length > 0
                    ? valid.Sum(c => aucs[c] * supports[c]) / valid.Sum(c => (double)supports[c])
                    : double.NaN
            };
        }

        private static ThresholdSelectionResult Selection(string objective, ScoreSweep sweep, int index, double value,
            bool achievable, string note, double beta, double level, double? costFalseNegative,
            double? costFalsePositive, int skipped)
        {
            return new ThresholdSelectionResult
            {
                Objective = objective,
                Threshold = sweep.Thresholds[index],
                ObjectiveValue = value,
                Achievable = achievable,
                Note = note,
                Metrics = sweep.Metrics(index, beta, level, costFalseNegative, costFalsePositive),
                BayesOptimalThreshold = double.NaN,
                ExpectedCostFlagNone = double.NaN,
                ExpectedCostFlagAll = double.NaN,
                SkippedRows = skipped
            };
        }

        private static int ArgMax(int length, Func<int, double> objective)
        {
            var best = 0;
            var bestValue = double.NegativeInfinity;

            for (var i = 0; i < length; i++)
            {
                var value = objective(i);

                if (value > bestValue + 1e-12)
                {
                    bestValue = value;
                    best = i;
                }
            }

            return best;
        }

        private static T[] Thin<T>(T[] points, int maxPoints)
        {
            if (maxPoints <= 0 || points.Length <= maxPoints)
            {
                return points;
            }

            if (maxPoints == 1)
            {
                return [points[0]];
            }

            var picked = new SortedSet<int>();

            for (var k = 0; k < maxPoints; k++)
            {
                picked.Add((int)Math.Round(k * (points.Length - 1d) / (maxPoints - 1d)));
            }

            return picked.Select(i => points[i]).ToArray();
        }

        private static (double[] V10, double[] V01) Placements(double[] scores, int[] positives, int[] negatives)
        {
            var all = CategoricalNumerics.AverageRanks(scores);
            var positiveRanks = CategoricalNumerics.AverageRanks(positives.Select(i => scores[i]).ToArray());
            var negativeRanks = CategoricalNumerics.AverageRanks(negatives.Select(i => scores[i]).ToArray());
            var m = positives.Length;
            var n = negatives.Length;
            var v10 = new double[m];

            for (var k = 0; k < m; k++)
            {
                v10[k] = (all[positives[k]] - positiveRanks[k]) / n;
            }

            var v01 = new double[n];

            for (var k = 0; k < n; k++)
            {
                v01[k] = 1d - (all[negatives[k]] - negativeRanks[k]) / m;
            }

            return (v10, v01);
        }

        private static (double AA, double BB, double AB) Covariance(double[] a, double[] b, int count)
        {
            var meanA = a.Average();
            var meanB = b.Average();
            double aa = 0;
            double bb = 0;
            double ab = 0;

            for (var i = 0; i < count; i++)
            {
                aa += (a[i] - meanA) * (a[i] - meanA);
                bb += (b[i] - meanB) * (b[i] - meanB);
                ab += (a[i] - meanA) * (b[i] - meanB);
            }

            return (aa / (count - 1), bb / (count - 1), ab / (count - 1));
        }

        private static double Pearson(double[] a, double[] b)
        {
            var meanA = a.Average();
            var meanB = b.Average();
            double ab = 0;
            double aa = 0;
            double bb = 0;

            for (var i = 0; i < a.Length; i++)
            {
                ab += (a[i] - meanA) * (b[i] - meanB);
                aa += (a[i] - meanA) * (a[i] - meanA);
                bb += (b[i] - meanB) * (b[i] - meanB);
            }

            return aa > 0d && bb > 0d ? ab / Math.Sqrt(aa * bb) : double.NaN;
        }
    }
}