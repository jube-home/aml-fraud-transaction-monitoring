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

using System.Globalization;

namespace Jube.Service.Charts
{
    public static class ExhaustiveNarrativeRules
    {
        private static string P(double value)
        {
            return value.ToString("0.0%", CultureInfo.InvariantCulture);
        }

        private static string N(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public static string DiscriminationBand(double auc)
        {
            return auc switch
            {
                >= 0.9 => "excellent",
                >= 0.8 => "good",
                >= 0.7 => "acceptable",
                >= 0.6 => "poor",
                _ => "little better than chance"
            };
        }

        public static IReadOnlyList<string> Performance(ExhaustiveNarrativeFacts facts)
        {
            var m = facts.Performance;

            if (m is null || !double.IsFinite(m.Auc))
            {
                return [];
            }

            var band = DiscriminationBand(m.Auc);

            var paragraphs = new List<string>
            {
                "On " + m.Rows.ToString("N0", CultureInfo.InvariantCulture) + " held-out cases (" +
                P(m.Prevalence) + " positive class), this model has an AUC of " + N(m.Auc) + " — by the common " +
                "Hosmer-Lemeshow convention, that is " + band + " discrimination between the positive and " +
                "negative classes."
            };

            if (m.Prevalence is > 0 and < 0.05)
            {
                paragraphs.Add("The positive class is rare in this sample (under 5%), so accuracy alone would be " +
                               "misleading — look at precision, recall and the AUC instead.");
            }

            return paragraphs;
        }

        public static IReadOnlyList<string> Calibration(ExhaustiveNarrativeFacts facts)
        {
            if (facts.Performance is null)
            {
                return [];
            }

            if (facts.Calibrated)
            {
                return
                [
                    "Its scores are calibrated, so a score of 0.30 is meant to be read as roughly a 30% " +
                    "chance of being the positive class, not just a ranking. Brier score " +
                    N(facts.Performance.Brier) +
                    " (lower is better) and expected calibration error " +
                    N(facts.Performance.ExpectedCalibrationError) + " describe how close that promise is kept."
                ];
            }

            return
            [
                "Its scores are not calibrated: they are only meaningful for ranking one case against " +
                "another, and should not be read as a probability or a percentage chance of being the " +
                "positive class."
            ];
        }

        public static IReadOnlyList<string> Sensitivity(ExhaustiveNarrativeFacts facts)
        {
            if (facts.Variables.Count == 0)
            {
                return [];
            }

            var top = facts.Variables[0];

            if (!(top.Sensitivity > 0))
            {
                return ["No single variable stands out as more important than the others to this model."];
            }

            var runnerUp = facts.Variables.Count > 1 ? facts.Variables[1] : null;

            if (runnerUp is not null && runnerUp.Sensitivity > 0 && top.Sensitivity < runnerUp.Sensitivity * 1.5)
            {
                return
                [
                    "No single variable dominates: " + top.Name + " and " + runnerUp.Name +
                    " are the strongest influences, at similar sensitivity (" + N(top.Sensitivity) + " and " +
                    N(runnerUp.Sensitivity) + ")."
                ];
            }

            return
            [
                "The model relies most heavily on " + top.Name + " (sensitivity " + N(top.Sensitivity) +
                ") — perturbing it changes the score more than any other variable."
            ];
        }

        public static IReadOnlyList<string> Prescription(ExhaustiveNarrativeFacts facts)
        {
            var candidates = facts.Variables
                .Where(v => v.PositiveMean.HasValue && v.NegativeMean.HasValue)
                .Take(3)
                .ToList();

            if (candidates.Count == 0)
            {
                return [];
            }

            return candidates.Select(v =>
            {
                var positiveRange = v.PositiveMaximum!.Value - v.PositiveMinimum!.Value;
                var negativeRange = v.NegativeMaximum!.Value - v.NegativeMinimum!.Value;

                var overlap = Math.Min(v.PositiveMaximum.Value, v.NegativeMaximum.Value) -
                              Math.Max(v.PositiveMinimum.Value, v.NegativeMinimum.Value);

                var widest = Math.Max(positiveRange, negativeRange);
                var strong = widest > 0 && overlap / widest < 0.3;

                return v.Name + ": simulated " + N(v.PositiveMinimum.Value) + " to " + N(v.PositiveMaximum.Value) +
                       " for a positive outcome, " + N(v.NegativeMinimum.Value) + " to " +
                       N(v.NegativeMaximum.Value) + " for a negative one — " +
                       (strong
                           ? "the two barely overlap, so this variable is a strong discriminator on its own"
                           : "the ranges overlap substantially, so this variable only helps in combination " +
                             "with others");
            }).ToList();
        }

        public static IReadOnlyList<string> Confusion(ExhaustiveNarrativeFacts facts)
        {
            var m = facts.Performance;

            if (m is null)
            {
                return [];
            }

            var c = m.Confusion;

            return
            [
                "At the current decision threshold (" + N(m.Threshold) + "), the model flags " +
                (c.TruePositives + c.FalsePositives).ToString("N0", CultureInfo.InvariantCulture) + " of " +
                m.Rows.ToString("N0", CultureInfo.InvariantCulture) + " held-out cases as the positive class: " +
                c.TruePositives.ToString("N0", CultureInfo.InvariantCulture) +
                " correctly (true positives) and " +
                c.FalsePositives.ToString("N0", CultureInfo.InvariantCulture) +
                " incorrectly (false positives), while missing " +
                c.FalseNegatives.ToString("N0", CultureInfo.InvariantCulture) +
                " real positive-class cases (false negatives).",

                "Precision is " + P(m.Precision) + " — of the cases it flags, that share are really the positive " +
                "class — and recall is " + P(m.Recall) + " — of the real positive class, that share is " +
                "caught. F1, their " +
                "balance, is " + N(m.F1) + "; Matthews correlation, which also accounts for the cases it " +
                "correctly leaves alone, is " + N(m.Mcc) + "."
            ];
        }

        public static IReadOnlyList<string> Discrimination(ExhaustiveNarrativeFacts facts)
        {
            var m = facts.Performance;

            if (m is null || !double.IsFinite(m.Auc))
            {
                return [];
            }

            var paragraphs = new List<string>
            {
                "The ROC curve traces the trade-off between catching the positive class (recall) and raising " +
                "false alarms " +
                "as the threshold moves; its area, the AUC, is " + N(m.Auc) +
                (double.IsFinite(m.AucLower) && double.IsFinite(m.AucUpper)
                    ? " (95% interval " + N(m.AucLower) + " to " + N(m.AucUpper) + ")"
                    : string.Empty) + "."
            };

            if (double.IsFinite(m.AveragePrecision))
            {
                paragraphs.Add("The precision-recall curve, which matters more than ROC when the positive class is " +
                               "rare, has an average precision of " + N(m.AveragePrecision) +
                               " — the share of held-out positive class is " + P(m.Prevalence) +
                               ", so a model with no " +
                               "skill would score close to that instead.");
            }

            return paragraphs;
        }

        public static IReadOnlyList<string> Ks(ExhaustiveNarrativeFacts facts)
        {
            var m = facts.Performance;

            if (m is null || !double.IsFinite(m.Ks))
            {
                return [];
            }

            return
            [
                "The Kolmogorov-Smirnov statistic, the widest gap between the cumulative score distributions " +
                "of the positive and negative classes, is " + N(m.Ks) + " at a score of " + N(m.KsAtScore) +
                " — a threshold near there separates the two classes as well as this model can."
            ];
        }

        public static IReadOnlyList<string> ThresholdSweep(ExhaustiveNarrativeFacts facts)
        {
            var m = facts.Performance;

            if (m is null)
            {
                return [];
            }

            return
            [
                "Moving the decision threshold trades precision against recall: at the current threshold (" +
                N(m.Threshold) + ") precision is " + P(m.Precision) + " and recall is " + P(m.Recall) +
                "; raising the threshold catches fewer but more certain cases, lowering it catches more at " +
                "the cost of more false alarms. This threshold is a policy choice, not something the model " +
                "fixes on its own."
            ];
        }

        public static IReadOnlyList<string> ScoreDistribution(ExhaustiveNarrativeFacts facts)
        {
            var d = facts.ScoreDistribution;

            if (d is null)
            {
                return [];
            }

            var band = d.SeparationInPooledStandardDeviations switch
            {
                >= 1.5 => "clearly separated",
                >= 0.8 => "separated with some overlap",
                >= 0.3 => "substantially overlapping",
                _ => "barely separated"
            };

            return
            [
                "Positive class cases score " + N(d.PositiveMean) + " on average (spread " +
                N(d.PositiveStandardDeviation) +
                ") against " + N(d.NegativeMean) + " for negative class cases (spread " +
                N(d.NegativeStandardDeviation) +
                ") — the two distributions are " + band + " (" + N(d.SeparationInPooledStandardDeviations) +
                " pooled standard deviations apart)."
            ];
        }

        public static IReadOnlyList<string> Gains(ExhaustiveNarrativeFacts facts)
        {
            var g = facts.Gains;

            if (g is null)
            {
                return [];
            }

            return
            [
                "Reviewing just the highest-scoring tenth of cases would catch " + P(g.TopDecileCaptureShare) +
                " of all the positive class in this sample, a lift of " + N(g.TopDecileLift) +
                "× over reviewing a random tenth" + (g.TopDecileLift < 2
                    ? " — a lift this low suggests the ranking is not concentrating the positive class at the " +
                      "top as strongly as it should; treat prioritised review lists from this model with caution."
                    : " — the model concentrates the positive class strongly at the top of the ranking.")
            ];
        }

        public static IReadOnlyList<string> PrecisionRecall(ExhaustiveNarrativeFacts facts)
        {
            var m = facts.Performance;

            if (m is null)
            {
                return [];
            }

            var gap = m.Precision - m.Recall;

            var balance = Math.Abs(gap) < 0.05
                ? "Precision and recall are closely balanced at " + P(m.Precision) + " and " + P(m.Recall)
                : gap > 0
                    ? "Precision (" + P(m.Precision) + ") is higher than recall (" + P(m.Recall) +
                      "), so the model is more cautious than thorough"
                    : "Recall (" + P(m.Recall) + ") is higher than precision (" + P(m.Precision) +
                      "), so the model is more thorough than cautious";

            return
            [
                balance + " at the current threshold." + (double.IsFinite(m.AveragePrecision)
                    ? " Average precision across every threshold is " + N(m.AveragePrecision) + "."
                    : string.Empty)
            ];
        }

        public static IReadOnlyList<string> Lift(ExhaustiveNarrativeFacts facts)
        {
            var l = facts.Lift;

            if (l is null)
            {
                return [];
            }

            var top = l.DecileLifts[0];
            string tail;

            if (l.EffectiveDeciles >= 10)
            {
                tail = " and stays above random across every decile, so the ranking stays informative all the " +
                       "way down the list.";
            }
            else if (l.EffectiveDeciles <= 1)
            {
                tail = ", but drops to random or worse from the second decile onward — only the very top of the " +
                       "ranking is worth acting on.";
            }
            else
            {
                tail = " and stays above random for the top " + l.EffectiveDeciles +
                       " of 10 deciles before dropping toward random, so review is best concentrated there.";
            }

            return ["Lift starts at " + N(top) + "× in the top decile" + tail];
        }

        public static IReadOnlyList<string> LearningCurve(ExhaustiveNarrativeFacts facts)
        {
            var l = facts.LearningCurve;

            if (l is null)
            {
                return [];
            }

            var paragraphs = new List<string>
            {
                "Over " + l.PromotionCount.ToString("N0", CultureInfo.InvariantCulture) +
                (l.PromotionCount == 1 ? " promotion" : " promotions") + " and " +
                l.TrialInstancesToDate.ToString("N0", CultureInfo.InvariantCulture) +
                " trial instances explored, the score moved from " + N(l.FirstScore) + " to " + N(l.LastScore) + "."
            };

            if (l.StillImproving.HasValue)
            {
                paragraphs.Add(l.StillImproving.Value
                    ? "The most recent promotions are still improving on the score, so further training may " +
                      "still help."
                    : "The score has plateaued over the most recent promotions, suggesting this topology search " +
                      "is close to converged.");
            }

            return paragraphs;
        }

        public static IReadOnlyList<string> Holistic(ExhaustiveNarrativeFacts facts)
        {
            var parts = Performance(facts).Concat(Calibration(facts)).Concat(Sensitivity(facts)).ToList();

            return parts.Count == 0
                ? ["There is not yet enough validation data on this model to say anything reliable about it."]
                : parts;
        }
    }
}