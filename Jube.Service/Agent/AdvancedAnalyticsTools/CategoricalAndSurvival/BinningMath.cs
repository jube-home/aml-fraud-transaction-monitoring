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
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    public enum CategoricalBinningMethod
    {
        EqualFrequency = 0,
        EqualWidth = 1
    }

    internal static class BinningMath
    {
        public static double[] CutPoints(double[] finiteValues, int bins, CategoricalBinningMethod method)
        {
            if (finiteValues.Length == 0)
            {
                return [];
            }

            var sorted = finiteValues.OrderBy(v => v).ToArray();
            var cuts = new SortedSet<double>();

            if (method == CategoricalBinningMethod.EqualWidth)
            {
                var min = sorted[0];
                var max = sorted[^1];

                for (var k = 1; k < bins; k++)
                {
                    cuts.Add(min + (max - min) * k / bins);
                }
            }
            else
            {
                for (var k = 1; k < bins; k++)
                {
                    cuts.Add(CategoricalNumerics.Quantile(sorted, k / (double)bins));
                }
            }

            return cuts.Where(c => c > sorted[0] && c <= sorted[^1]).ToArray();
        }

        public static int Assign(double value, double[] cuts)
        {
            var index = 0;

            while (index < cuts.Length && value >= cuts[index])
            {
                index++;
            }

            return index;
        }

        public static string Label(int index, double[] cuts)
        {
            var lower = index == 0
                ? "-inf"
                : cuts[index - 1].ToString("G6", CultureInfo.InvariantCulture);

            var upper = index == cuts.Length
                ? "+inf"
                : cuts[index].ToString("G6", CultureInfo.InvariantCulture);

            return $"[{lower}, {upper})";
        }

        public static InformationValueResult InformationValue(string[] labels, double[] lowerBounds,
            double[] upperBounds,
            int[] eventCounts, int[] nonEventCounts, double smoothing, int skipped)
        {
            var totalEvents = eventCounts.Sum();
            var totalNonEvents = nonEventCounts.Sum();

            if (totalEvents == 0 || totalNonEvents == 0)
            {
                throw new ArgumentException(
                    "Information value needs at least one event and one non-event among the usable rows; the outcome is constant.",
                    "labels");
            }

            var bins = labels.Length;
            var zeroCell = eventCounts.Any(e => e == 0) || nonEventCounts.Any(e => e == 0);
            var adjustment = zeroCell ? smoothing : 0d;
            var eventDenominator = totalEvents + adjustment * bins;
            var nonEventDenominator = totalNonEvents + adjustment * bins;
            var result = new List<InformationValueBin>();
            var total = 0d;

            for (var i = 0; i < bins; i++)
            {
                var shareEvents = (eventCounts[i] + adjustment) / eventDenominator;
                var shareNonEvents = (nonEventCounts[i] + adjustment) / nonEventDenominator;
                double woe;
                double contribution;

                if (shareEvents <= 0d || shareNonEvents <= 0d)
                {
                    woe = double.NaN;
                    contribution = double.NaN;
                }
                else
                {
                    woe = Math.Log(shareEvents / shareNonEvents);
                    contribution = (shareEvents - shareNonEvents) * woe;
                }

                total += double.IsNaN(contribution) ? 0d : contribution;
                var count = eventCounts[i] + nonEventCounts[i];

                result.Add(new InformationValueBin
                {
                    Label = labels[i],
                    LowerBound = lowerBounds[i],
                    UpperBound = upperBounds[i],
                    Count = count,
                    Events = eventCounts[i],
                    NonEvents = nonEventCounts[i],
                    EventRate = count > 0 ? eventCounts[i] / (double)count : double.NaN,
                    ShareOfEvents = shareEvents,
                    ShareOfNonEvents = shareNonEvents,
                    WeightOfEvidence = woe,
                    InformationValueContribution = contribution
                });
            }

            var strength = total < 0.02
                ? "not predictive"
                : total < 0.1
                    ? "weak"
                    : total < 0.3
                        ? "medium"
                        : total < 0.5
                            ? "strong"
                            : "very strong, check for leakage or overfitting";

            return new InformationValueResult
            {
                Bins = result.ToArray(),
                TotalInformationValue = total,
                Strength = strength,
                TotalEvents = totalEvents,
                TotalNonEvents = totalNonEvents,
                OverallEventRate = totalEvents / (double)(totalEvents + totalNonEvents),
                SmoothingApplied = zeroCell && smoothing > 0d,
                WeightOfEvidenceIsMonotonic = IsMonotonic(result
                    .Where(b => b.Count > 0 && !double.IsNaN(b.WeightOfEvidence))
                    .Select(b => b.WeightOfEvidence).ToArray()),
                SkippedRows = skipped
            };
        }

        private static bool IsMonotonic(double[] values)
        {
            var rising = true;
            var falling = true;

            for (var i = 1; i < values.Length; i++)
            {
                if (values[i] < values[i - 1])
                {
                    rising = false;
                }

                if (values[i] > values[i - 1])
                {
                    falling = false;
                }
            }

            return rising || falling;
        }

        public static PopulationStabilityResult Stability(string[] labels, double[] baseline, double[] current,
            double floor)
        {
            var baselineTotal = baseline.Sum();
            var currentTotal = current.Sum();

            if (baselineTotal <= 0d || currentTotal <= 0d)
            {
                throw new ArgumentException(
                    "Both the baseline and the current population need at least one observation.");
            }

            var bins = new List<PopulationStabilityBin>();
            var psi = 0d;

            for (var i = 0; i < labels.Length; i++)
            {
                var expected = Math.Max(baseline[i] / baselineTotal, floor);
                var actual = Math.Max(current[i] / currentTotal, floor);
                var contribution = (actual - expected) * Math.Log(actual / expected);
                psi += contribution;

                bins.Add(new PopulationStabilityBin
                {
                    Label = labels[i],
                    BaselineCount = baseline[i],
                    CurrentCount = current[i],
                    BaselineShare = baseline[i] / baselineTotal,
                    CurrentShare = current[i] / currentTotal,
                    Contribution = contribution
                });
            }

            return new PopulationStabilityResult
            {
                Bins = bins.ToArray(),
                PopulationStabilityIndex = psi,
                Interpretation = psi < 0.1
                    ? "stable: no meaningful shift"
                    : psi < 0.25
                        ? "moderate shift: investigate"
                        : "significant shift: the population has changed materially"
            };
        }
    }
}