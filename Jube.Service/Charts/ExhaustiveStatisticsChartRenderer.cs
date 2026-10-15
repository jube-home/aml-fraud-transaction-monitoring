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

using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Dto.Query.ExhaustiveSearchInstanceVariableStatistics;

namespace Jube.Service.Charts
{
    public static class ExhaustiveStatisticsChartRenderer
    {
        public const string PositiveSeriesName = "Positive class";
        public const string NegativeSeriesName = "Negative class";
        public const string AllRowsSeriesName = "All rows";
        public const string MonteCarloSeriesName = "Model profile (Monte Carlo)";
        public const string ClassesTitle = "Distribution by class";
        public const string MonteCarloTitle = "Model profile";
        public const string SummaryTitle = "Model profile summary";
        public const string EmptyMessage = ChartEmptyState.Message;

        public const string MonteCarloEmptyMessage =
            "No Monte Carlo statistics: an older instance, no promoted trial, or a variable the model does not use";

        public const string AlignedCaption =
            "Both classes are binned on the same bins. Each class is shown as its share of the class.";

        public const string AlignedCountsCaption = "Both classes are binned on the same bins. Counts per bin.";

        public const string SeparateCaption =
            "This instance was trained before class histograms were kept: the classes were binned separately, so each is shown as a density (area of one).";

        public const string MonteCarloCaption =
            "Densities (area of one) on a shared axis. The model profile is what the promoted model scores above 0.5 over simulated inputs.";

        public const string MonteCarloCountsCaption =
            "Counts per bin. The model profile is what the promoted model scores above 0.5 over simulated inputs.";

        public const string SummaryCaption =
            "Each statistic in standard deviations of all rows. Negative class: mean and deviation by subtraction, minimum, maximum and interquartile range from its histogram.";

        public const string AlignedShort = "Share of each class, common bins";
        public const string AlignedCountsShort = "Count per bin, common bins";
        public const string SeparateShort = "Binned separately, so densities";
        public const string NotStoredShort = "Positive class not stored";
        public const string MonteCarloShort = "Densities on a shared axis";
        public const string MonteCarloCountsShort = "Counts per bin";
        public const string SummaryShort = "In deviations of all rows";

        private static readonly string[] summaryCategories =
            ["Mean", "Standard deviation", "Minimum", "Maximum", "Interquartile range"];

        public static string VariableClasses(ExhaustiveSearchInstanceVariableStatisticsDto? variable,
            bool counts = false, bool large = false, ChartStyleMode styleMode = ChartStyleMode.External,
            string? id = null)
        {
            var (width, height) = Size(large);

            if (variable == null)
            {
                return ClassesEmpty(width, height, styleMode, id);
            }

            var negative = Edges(variable.NegativeBins);
            var positive = Edges(variable.PositiveBins);

            if (negative != null && positive != null && negative.Length == positive.Length &&
                negative.Zip(positive).All(p => p.First == p.Second))
            {
                var builder = Chart.Histogram()
                    .Title(Title(variable))
                    .Subtitle(large ? counts ? AlignedCountsCaption : AlignedCaption :
                        counts ? AlignedCountsShort : AlignedShort)
                    .Description(counts ? AlignedCountsCaption : AlignedCaption)
                    .XAxisTitle("Value")
                    .YAxisTitle(counts ? "Frequency" : large ? "Share of class" : "Share")
                    .Size(width, height)
                    .Legend(LegendPosition.Bottom)
                    .Bins(NegativeSeriesName, negative, Counts(variable.NegativeBins))
                    .Bins(PositiveSeriesName, positive, Counts(variable.PositiveBins));

                if (!counts)
                {
                    builder.Normalized();
                }

                return builder.StyleMode(styleMode).Id(id).Render();
            }

            var all = Edges(variable.PopulationBins);
            var separate = Edges(variable.ClassificationBins);
            var positiveBins = variable.ClassificationBins;

            if (separate == null)
            {
                separate = Edges(variable.AnomalyBins);
                positiveBins = variable.AnomalyBins;
            }

            if (all == null)
            {
                return ClassesEmpty(width, height, styleMode, id);
            }

            var densities = Chart.Histogram()
                .Title(Title(variable))
                .Subtitle(separate == null ? NotStoredShort : large ? SeparateCaption : SeparateShort)
                .Description(
                    separate == null ? "The positive class was not stored for this instance." : SeparateCaption)
                .XAxisTitle("Value")
                .YAxisTitle("Density")
                .Size(width, height)
                .Legend(LegendPosition.Bottom)
                .Density()
                .Bins(AllRowsSeriesName, all, Counts(variable.PopulationBins));

            if (separate != null)
            {
                densities.Bins(PositiveSeriesName, separate, Counts(positiveBins));
            }

            return densities.StyleMode(styleMode).Id(id).Render();
        }

        public static string MonteCarlo(ExhaustiveSearchInstanceVariableStatisticsDto? variable,
            bool counts = false, bool large = false, ChartStyleMode styleMode = ChartStyleMode.External,
            string? id = null)
        {
            var (width, height) = Size(large);
            var model = variable == null ? null : Edges(variable.PrescriptionBins);

            if (variable == null || model == null)
            {
                return Empty(MonteCarloTitle, "Value", counts ? "Count" : "Density", MonteCarloEmptyMessage,
                    width, height, styleMode, id);
            }

            var negative = Edges(variable.NegativeBins);
            var positive = Edges(variable.PositiveBins);
            var aligned = negative != null && positive != null;
            var slot0 = aligned ? negative : Edges(variable.PopulationBins);
            var slot0Bins = aligned ? variable.NegativeBins : variable.PopulationBins;
            var slot0Name = aligned ? NegativeSeriesName : AllRowsSeriesName;
            var slot1 = aligned ? positive : Edges(variable.ClassificationBins) ?? Edges(variable.AnomalyBins);

            var slot1Bins = aligned ? variable.PositiveBins :
                variable.ClassificationBins.Count > 0 ? variable.ClassificationBins : variable.AnomalyBins;

            var builder = Chart.Histogram()
                .Title(Title(variable))
                .Subtitle(large ? counts ? MonteCarloCountsCaption : MonteCarloCaption :
                    counts ? MonteCarloCountsShort : MonteCarloShort)
                .Description(counts ? MonteCarloCountsCaption : MonteCarloCaption)
                .XAxisTitle("Value")
                .YAxisTitle(counts ? "Count" : "Density")
                .Size(width, height)
                .Legend(LegendPosition.Bottom);

            if (!counts)
            {
                builder.Density();
            }

            if (slot0 != null)
            {
                builder.Bins(slot0Name, slot0, Counts(slot0Bins));
            }
            else
            {
                builder.Bins(slot0Name + " (not available)", model, new double[model.Length - 1]);
            }

            if (slot1 != null)
            {
                builder.Bins(PositiveSeriesName, slot1, Counts(slot1Bins));
            }
            else
            {
                builder.Bins(PositiveSeriesName + " (not available)", model, new double[model.Length - 1]);
            }

            builder.Bins(MonteCarloSeriesName, model, Counts(variable.PrescriptionBins));

            return builder.StyleMode(styleMode).Id(id).Render();
        }

        public static string MonteCarloSummary(ExhaustiveSearchInstanceVariableStatisticsDto? variable,
            bool large = false, ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            var (width, height) = Size(large);
            var scale = variable?.Population.StandardDeviation ?? 0;

            if (variable?.Prescription == null || !(scale > 0))
            {
                return Empty(SummaryTitle, "Statistic", "Standard deviations", MonteCarloEmptyMessage, width, height,
                    styleMode, id);
            }

            var negative = NegativeSummary(variable);
            var positive = variable.Classification ?? variable.Anomaly;
            var origin = variable.Population.Mean;

            var builder = Chart.Column()
                .Title(Title(variable))
                .Subtitle(large ? SummaryCaption : SummaryShort)
                .Description(SummaryCaption)
                .XAxisTitle("Statistic")
                .YAxisTitle("Standard deviations")
                .Size(width, height)
                .Legend(LegendPosition.Bottom)
                .Categories(summaryCategories);

            builder.Series(negative == null ? NegativeSeriesName + " (not available)" : NegativeSeriesName,
                Standardised(negative, origin, scale));
            builder.Series(positive == null ? PositiveSeriesName + " (not available)" : PositiveSeriesName,
                Standardised(positive, origin, scale));

            builder.Series(MonteCarloSeriesName, Standardised(variable.Prescription, origin, scale));

            return builder.StyleMode(styleMode).Id(id).Render();
        }

        public static ExhaustiveSearchInstanceVariableStatisticsSummaryDto? NegativeSummary(
            ExhaustiveSearchInstanceVariableStatisticsDto variable)
        {
            if (variable.Classification != null)
            {
                return variable.Population;
            }

            if (variable.Anomaly == null || variable.NegativeBins.Count == 0 || variable.PositiveBins.Count == 0)
            {
                return null;
            }

            var nNegative = (double)variable.NegativeBins.Sum(b => (long)b.Frequency);
            var nPositive = (double)variable.PositiveBins.Sum(b => (long)b.Frequency);
            var n = nNegative + nPositive;

            if (nNegative < 2)
            {
                return null;
            }

            var population = variable.Population;
            var positive = variable.Anomaly;
            var mean = (n * population.Mean - nPositive * positive.Mean) / nNegative;

            var sumSquares = (n - 1) * population.StandardDeviation * population.StandardDeviation +
                             n * population.Mean * population.Mean;
            var positiveSumSquares = (nPositive - 1) * positive.StandardDeviation * positive.StandardDeviation +
                                     nPositive * positive.Mean * positive.Mean;

            var variance = (sumSquares - positiveSumSquares - nNegative * mean * mean) / (nNegative - 1);

            var populated = variable.NegativeBins.Where(b => b.Frequency > 0).ToList();

            return new ExhaustiveSearchInstanceVariableStatisticsSummaryDto
            {
                Mean = mean,
                StandardDeviation = Math.Sqrt(Math.Max(variance, 0)),
                Minimum = populated.Count == 0 ? mean : populated.First().Start,
                Maximum = populated.Count == 0 ? mean : populated.Last().End,
                Iqr = Quantile(variable.NegativeBins, 0.75) - Quantile(variable.NegativeBins, 0.25)
            };
        }

        public static double Quantile(IReadOnlyList<ExhaustiveSearchInstanceVariableStatisticsBinDto> bins, double p)
        {
            var total = (double)bins.Sum(b => (long)b.Frequency);

            if (total <= 0)
            {
                return 0;
            }

            var target = p * total;
            var cumulative = 0.0;

            foreach (var bin in bins)
            {
                if (bin.Frequency > 0 && cumulative + bin.Frequency >= target)
                {
                    return bin.Start + (bin.End - bin.Start) * (target - cumulative) / bin.Frequency;
                }

                cumulative += bin.Frequency;
            }

            return bins[^1].End;
        }

        public static double[]? Edges(IReadOnlyList<ExhaustiveSearchInstanceVariableStatisticsBinDto> bins)
        {
            if (bins.Count == 0)
            {
                return null;
            }

            if (bins.Count == 1 && !(bins[0].End > bins[0].Start) && double.IsFinite(bins[0].Start) &&
                double.IsFinite(bins[0].End))
            {
                return [bins[0].Start - 0.5, bins[0].Start + 0.5];
            }

            var edges = new double[bins.Count + 1];

            for (var i = 0; i < bins.Count; i++)
            {
                edges[i] = bins[i].Start;

                if (i > 0 && Math.Abs(bins[i].Start - bins[i - 1].End) >
                    1e-9 * Math.Max(1, Math.Abs(bins[i].Start) + Math.Abs(bins[i - 1].End)))
                {
                    return null;
                }
            }

            edges[bins.Count] = bins[^1].End;

            for (var i = 1; i < edges.Length; i++)
            {
                if (!double.IsFinite(edges[i]) || !(edges[i] > edges[i - 1]))
                {
                    return null;
                }
            }

            return edges;
        }

        private static double[] Counts(IEnumerable<ExhaustiveSearchInstanceVariableStatisticsBinDto> bins)
        {
            return bins.Select(b => (double)b.Frequency).ToArray();
        }

        private static double[] Standardised(ExhaustiveSearchInstanceVariableStatisticsSummaryDto? summary,
            double origin, double scale)
        {
            if (summary == null)
            {
                return new double[summaryCategories.Length];
            }

            return
            [
                (summary.Mean - origin) / scale,
                summary.StandardDeviation / scale,
                (summary.Minimum - origin) / scale,
                (summary.Maximum - origin) / scale,
                summary.Iqr / scale
            ];
        }

        private static string Title(ExhaustiveSearchInstanceVariableStatisticsDto variable)
        {
            return string.IsNullOrWhiteSpace(variable.Name) ? ClassesTitle : variable.Name;
        }

        private static (double Width, double Height) Size(bool large)
        {
            return large ? (760, 440) : (340, 210);
        }

        private static string ClassesEmpty(double width, double height, ChartStyleMode styleMode, string? id)
        {
            return Empty(ClassesTitle, "Value", "Share", EmptyMessage, width, height, styleMode, id);
        }

        public static string Empty(string title, string xAxisTitle, string yAxisTitle, string message,
            double width, double height, ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            return ChartEmptyState.Render(title, width >= 600 ? message : message.Split(':')[0], message, xAxisTitle,
                yAxisTitle, width, height, styleMode, id);
        }
    }
}