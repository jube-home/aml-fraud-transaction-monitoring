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
using Jube.Analytics;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Performance;
using Jube.Charting.Performance.Models;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotionHistory;

namespace Jube.Service.Charts
{
    public sealed record PerformanceMeasures(
        int Rows,
        int Positives,
        int Negatives,
        double Prevalence,
        double Auc,
        double AucStandardError,
        double AucLower,
        double AucUpper,
        double Gini,
        double AveragePrecision,
        double Ks,
        double KsAtScore,
        double Brier,
        double BrierSkill,
        double LogLoss,
        double ExpectedCalibrationError,
        double Threshold,
        ConfusionMatrix Confusion)
    {
        public double Accuracy => Confusion.Accuracy;
        public double BalancedAccuracy => (Confusion.Recall + Confusion.Specificity) / 2;
        public double Precision => Confusion.Precision;
        public double Recall => Confusion.Recall;
        public double Specificity => Confusion.Specificity;
        public double F1 => Confusion.F1;
        public double Mcc => Confusion.Mcc;
        public double Kappa => Confusion.CohenKappa;
    }

    public static class ExhaustivePerformanceRenderer
    {
        public const double DefaultThreshold = 0.5;
        public const double ActualCutOff = 0.5;
        public const int CalibrationBins = 10;
        public const int DistributionBins = 20;
        public const int SweepSteps = 100;
        public const double IntervalZ = 1.959963984540054;
        public const double MaximumDomain = 1000;
        private const string SweepTitle = "Threshold Sweep";

        public static double SafeThreshold(double? threshold)
        {
            return threshold is { } t && double.IsFinite(t) && t >= 0 && t <= 1 ? t : DefaultThreshold;
        }

        public static PerformanceMeasures? Measures(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            double threshold = DefaultThreshold)
        {
            var data = Prepare(rows);

            if (data is null)
            {
                return null;
            }

            var (scores, labels, actual, _) = data.Value;
            var positives = labels.Count(l => l == 1);
            var negatives = labels.Length - positives;
            var prevalence = positives / (double)labels.Length;
            var auc = BinaryMetrics.RocAucWithStandardError(scores, labels);
            var ks = PerformanceMath.Ks(actual, scores);
            var inUnit = scores.All(s => s >= 0 && s <= 1);
            var brier = inUnit ? BinaryMetrics.BrierScore(scores, labels) : double.NaN;
            var reference = prevalence * (1 - prevalence);

            return new PerformanceMeasures(labels.Length, positives, negatives, prevalence, auc.Auc,
                auc.StandardError,
                double.IsFinite(auc.StandardError) ? Math.Max(0, auc.Auc - IntervalZ * auc.StandardError) : double.NaN,
                double.IsFinite(auc.StandardError) ? Math.Min(1, auc.Auc + IntervalZ * auc.StandardError) : double.NaN,
                2 * auc.Auc - 1,
                BinaryMetrics.AveragePrecision(scores, labels),
                ks.Statistic, ks.AtScore,
                brier,
                reference > 0 ? 1 - brier / reference : double.NaN,
                inUnit ? BinaryMetrics.LogLoss(scores, labels) : double.NaN,
                inUnit ? BinaryMetrics.ExpectedCalibrationError(scores, labels) : double.NaN,
                threshold, BinaryMetrics.Confusion(scores, labels, threshold));
        }

        public static string MeasuresPanel(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            double threshold, ChartStyleMode styleMode, string? id,
            IReadOnlyList<MetricTile>? calibrationTiles = null, string? calibrationCaption = null,
            string? calibrationDescription = null)
        {
            var m = Measures(rows, threshold);

            if (m is null)
            {
                return ExhaustiveChartRenderer.Empty("Performance Measures", string.Empty, string.Empty, 720, 200,
                    styleMode, id);
            }

            var t = Number(threshold);

            var panel = Chart.MetricsPanel()
                .Title("Performance Measures")
                .Subtitle(m.Rows.ToString("N0", CultureInfo.InvariantCulture) + " test rows, " +
                          m.Positives.ToString("N0", CultureInfo.InvariantCulture) + " positive class (" +
                          (m.Prevalence * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%)" +
                          (calibrationCaption is null ? string.Empty : ". " + calibrationCaption))
                .Group("Ranking, independent of any threshold",
                [
                    new MetricTile("AUC", m.Auc,
                        double.IsFinite(m.AucLower)
                            ? "95% interval " + Number(m.AucLower) + " to " + Number(m.AucUpper)
                            : null,
                        "Area under the ROC curve: the probability that a random positive-class case outscores a " +
                        "random negative-class case. The interval is DeLong (1988), normal approximation."),
                    new MetricTile("Gini", m.Gini, null, "2 x AUC - 1."),
                    new MetricTile("KS", m.Ks, "at score " + Number(m.KsAtScore),
                        "Kolmogorov-Smirnov: the largest gap between the cumulative score distributions of the " +
                        "positive and negative classes."),
                    new MetricTile("Average precision", m.AveragePrecision,
                        "no skill " + Number(m.Prevalence),
                        "Area under the precision-recall curve (step-wise). A model with no skill scores the " +
                        "positive class rate.")
                ])
                .Group(calibrationTiles is null ? "Probability quality" : "Probability quality (calibrated)",
                [
                    new MetricTile("Brier score", m.Brier, null,
                        "Mean squared error of the predicted probability (Brier, 1950). Lower is better."),
                    new MetricTile("Brier skill score", m.BrierSkill, null,
                        "1 - Brier / (positive rate x negative rate). Zero is no better than always predicting the " +
                        "positive class rate."),
                    new MetricTile("Log loss", m.LogLoss, null,
                        "Mean negative log likelihood of the outcome. Lower is better."),
                    new MetricTile("Expected calibration error", m.ExpectedCalibrationError, "10 equal bins",
                        "Count weighted mean gap between predicted probability and observed positive class rate.")
                ])
                .Group("At threshold " + t,
                [
                    new MetricTile("Accuracy", m.Accuracy, null, "Correct decisions over all decisions."),
                    new MetricTile("Balanced accuracy", m.BalancedAccuracy, null,
                        "Mean of recall and specificity, robust to class imbalance."),
                    new MetricTile("Precision", m.Precision, null,
                        "Share of flagged cases that are the positive class."),
                    new MetricTile("Recall", m.Recall, null, "Share of the positive class that is flagged."),
                    new MetricTile("Specificity", m.Specificity, null, "Share of negative-class cases left alone."),
                    new MetricTile("F1", m.F1, null, "Harmonic mean of precision and recall."),
                    new MetricTile("MCC", m.Mcc, null,
                        "Matthews correlation coefficient: balanced over all four confusion cells."),
                    new MetricTile("Cohen's kappa", m.Kappa, null, "Agreement beyond chance.")
                ]);

            if (calibrationTiles is { Count: > 0 })
            {
                panel.Group("Calibration", calibrationTiles);
            }

            if (calibrationDescription is not null)
            {
                panel.Description(calibrationDescription);
            }

            return panel.StyleMode(styleMode).Id(id).Render();
        }

        public static string ScoreDistribution(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows, double threshold,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null, string? variableName = null)
        {
            const string title = "Score Distribution";
            var data = Prepare(rows, variableName is not null);
            var yAxisTitle = variableName is null ? "Share of class" : "Share of " + variableName;

            if (data is null)
            {
                return ExhaustiveChartRenderer.Empty(title, "Predicted probability", yAxisTitle,
                    styleMode: styleMode, id: id);
            }

            var (scores, _, actual, weights) = data.Value;
            var (low, high) = Domain(scores);

            return Chart.ScoreDistribution()
                .Title(title)
                .Subtitle(variableName is null
                    ? "Predicted probability of the positive class, each class as a share of itself"
                    : "By value of " + variableName + ", each class as a share of its own total " + variableName)
                .YAxisTitle(yAxisTitle)
                .Classes(ExhaustiveChartRenderer.PositiveSeriesName, ExhaustiveChartRenderer.NegativeSeriesName)
                .FromScores(actual, scores, low, high, DistributionBins, weights)
                .Threshold(threshold)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string Roc(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null, string? variableName = null)
        {
            const string title = "ROC";
            var data = Prepare(rows, variableName is not null);

            if (data is null)
            {
                return ExhaustiveChartRenderer.Empty(title, "False Positive Rate", "True Positive Rate",
                    styleMode: styleMode, id: id);
            }

            var (scores, _, actual, weights) = data.Value;
            var yAxisTitle = variableName is null ? "True Positive Rate" : "Share of " + variableName + " captured";

            return Chart.Roc()
                .Title(title)
                .Subtitle(variableName is null
                    ? "Diagonal is chance; the marked point maximises Youden's J"
                    : "By value of " + variableName + ". Diagonal is chance.")
                .XAxisTitle(variableName is null ? "False Positive Rate" : "Share of " + variableName + " at risk")
                .YAxisTitle(yAxisTitle)
                .Size(640, 480)
                .Legend(LegendPosition.Bottom)
                .FromScores("Model", actual, scores, weights)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string PrecisionRecall(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            const string title = "Precision Recall";
            var data = Prepare(rows);

            if (data is null)
            {
                return ExhaustiveChartRenderer.Empty(title, "Recall", "Precision", styleMode: styleMode, id: id);
            }

            var (scores, _, actual, _) = data.Value;

            return Chart.PrecisionRecall()
                .Title(title)
                .Subtitle("Dashed line is the positive class rate, the precision of a model with no skill")
                .Size(640, 480)
                .Legend(LegendPosition.Bottom)
                .FromScores("Model", actual, scores)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string Calibration(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null, string? note = null)
        {
            const string title = "Calibration";
            var data = Prepare(rows);

            if (data is null || data.Value.Scores.Any(s => s < 0 || s > 1))
            {
                return ExhaustiveChartRenderer.Empty(title, "Mean predicted probability",
                    "Observed fraction of positive class", styleMode: styleMode, id: id);
            }

            var (scores, _, actual, _) = data.Value;

            return Chart.Calibration()
                .Title(title)
                .Subtitle((note is null ? string.Empty : note + ". ") +
                          "Reliability of the predicted probability, with the count of rows in each bin")
                .Size(640, 560)
                .Legend(LegendPosition.Top)
                .FromProbabilities("Model", actual, scores)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string Gains(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null, string? variableName = null)
        {
            const string title = "Cumulative Gains";
            var data = Prepare(rows, variableName is not null);

            if (data is null)
            {
                return ExhaustiveChartRenderer.Empty(title,
                    variableName is null ? "Share of population" : "Share of " + variableName,
                    variableName is null
                        ? "Share of positive class captured"
                        : "Share of " + variableName + " captured",
                    styleMode: styleMode, id: id);
            }

            var (scores, _, actual, weights) = data.Value;

            return Chart.Gains()
                .Title(title)
                .Subtitle(variableName is null
                    ? "Share of positive class captured by reviewing the highest scoring share of the population"
                    : "Share of " + variableName + " captured by reviewing the highest scoring share of the population")
                .FromScores("Model", actual, scores, weights)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string ValueCaptured(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null, string? variableName = null)
        {
            const string title = "Value Captured";

            if (variableName is null)
            {
                return ExhaustiveChartRenderer.Empty(title, "Share of population, highest score first",
                    "Cumulative value captured", styleMode: styleMode, id: id);
            }

            var data = Prepare(rows, true);

            if (data is null)
            {
                return ExhaustiveChartRenderer.Empty(title, "Share of population, highest score first",
                    "Cumulative " + variableName + " captured", styleMode: styleMode, id: id);
            }

            var (scores, _, actual, weights) = data.Value;

            return Chart.Gains()
                .Title(title)
                .Subtitle("Cumulative " + variableName +
                          " captured by reviewing the highest scoring share of the population. Not normalised: " +
                          "the vertical axis is the actual amount, not a share.")
                .YAxisTitle("Cumulative " + variableName + " captured")
                .Absolute()
                .FromScores("Model", actual, scores, weights)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string Lift(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null, string? variableName = null)
        {
            const string title = "Lift by Decile";
            var data = Prepare(rows, variableName is not null);

            if (data is null)
            {
                return ExhaustiveChartRenderer.Empty(title, "Score decile", "Lift", styleMode: styleMode, id: id);
            }

            var (scores, _, actual, weights) = data.Value;

            return Chart.Lift()
                .Title(title)
                .Subtitle(variableName is null
                    ? "Positive class rate in each tenth of the population, relative to the overall positive class rate"
                    : variableName +
                      " rate in each tenth of the population, relative to the overall rate")
                .FromScores(actual, scores, weights: weights)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string Ks(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows, double threshold,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null, string? variableName = null)
        {
            const string title = "Kolmogorov-Smirnov";
            var data = Prepare(rows, variableName is not null);
            var yAxisTitle = variableName is null ? "Cumulative share of class" : "Cumulative share of " + variableName;

            if (data is null)
            {
                return ExhaustiveChartRenderer.Empty(title, "Score", yAxisTitle, styleMode: styleMode, id: id);
            }

            var (scores, _, actual, weights) = data.Value;
            var (low, high) = Domain(scores);

            return Chart.Ks()
                .Title(title)
                .Subtitle(variableName is null
                    ? "Cumulative distribution of the score for each class; the marked gap is the KS statistic"
                    : "By value of " + variableName + "; the marked gap is the value-weighted KS statistic")
                .YAxisTitle(yAxisTitle)
                .Classes(ExhaustiveChartRenderer.PositiveSeriesName, ExhaustiveChartRenderer.NegativeSeriesName)
                .FromScores(actual, scores, weights)
                .Domain(low, high)
                .Threshold(threshold)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string ThresholdSweep(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows, double threshold,
            double? falseNegativeCost = null, double? falsePositiveCost = null,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            var data = Prepare(rows);

            if (data is null)
            {
                return ExhaustiveChartRenderer.Empty(SweepTitle, "Decision threshold", "Measure",
                    styleMode: styleMode, id: id);
            }

            var (scores, _, actual, _) = data.Value;
            var (low, high) = Domain(scores);
            var risk = MinimumRiskThreshold(falseNegativeCost, falsePositiveCost);

            return Chart.ThresholdSweep()
                .Title(SweepTitle)
                .Subtitle(risk is null
                    ? "Precision, recall, F1 and MCC at every decision threshold"
                    : "Precision, recall, F1 and MCC at every decision threshold; minimum-risk threshold from the supplied costs")
                .FromScores(actual, scores, low, high)
                .CurrentThreshold(threshold)
                .MinimumRiskThreshold(risk)
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static double? MinimumRiskThreshold(double? falseNegativeCost, double? falsePositiveCost)
        {
            if (falseNegativeCost is not { } fn || falsePositiveCost is not { } fp || !double.IsFinite(fn) ||
                !double.IsFinite(fp) || fn <= 0 || fp <= 0)
            {
                return null;
            }

            return new CostMatrix(fn, fp).MinimumRiskThreshold();
        }

        public static string Confusion(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows, double threshold,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            const string title = "Confusion Matrix";
            var data = Prepare(rows);

            if (data is null)
            {
                return ExhaustiveChartRenderer.Empty(title, "Predicted", "Actual", styleMode: styleMode, id: id);
            }

            var (scores, labels, _, _) = data.Value;
            var c = BinaryMetrics.Confusion(scores, labels, threshold);

            return Chart.ConfusionMatrix()
                .Title(title)
                .Subtitle(
                    "Test rows at threshold " + Number(threshold) + ", counts with the share of each actual class")
                .Binary(c.TrueNegatives, c.FalsePositives, c.FalseNegatives, c.TruePositives)
                .XAxisTitle("Predicted")
                .YAxisTitle("Actual")
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        public static string PromotionHistory(
            IReadOnlyList<ExhaustiveSearchInstancePromotionHistoryDto> promotions,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            const string title = "Promotion Over Time";

            if (promotions.Count == 0)
            {
                return ExhaustiveChartRenderer.Empty(title, "Promotion date", "Score", styleMode: styleMode, id: id);
            }

            return Chart.PromotionHistory()
                .Title(title)
                .Promotions(promotions.OrderBy(p => p.CreatedDate).ThenBy(p => p.Id).Select(p =>
                    new PromotionPoint(DateTime.SpecifyKind(p.CreatedDate, DateTimeKind.Utc), p.Score,
                        p.TopologyComplexity, p.TrialInstances, p.TrialInstancesToDate, p.Active)))
                .StyleMode(styleMode).Id(id)
                .Render();
        }

        private static string Number(double value)
        {
            return double.IsFinite(value) ? value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a";
        }

        private static (double Low, double High) Domain(double[] scores)
        {
            var low = Math.Min(0, Math.Floor(scores.Min() * 10) / 10);
            var high = Math.Max(1, Math.Ceiling(scores.Max() * 10) / 10);

            return double.IsFinite(low) && double.IsFinite(high) && low >= -MaximumDomain && high <= MaximumDomain
                ? (low, high)
                : (0, 1);
        }

        private static (double[] Scores, int[] Labels, bool[] Actual, double[]? Weights)? Prepare(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            bool includeWeights = false)
        {
            var scores = new List<double>(rows.Count);
            var labels = new List<int>(rows.Count);
            var weights = includeWeights ? new List<double>(rows.Count) : null;

            foreach (var row in rows)
            {
                if (!double.IsFinite(row.Predicted) || !double.IsFinite(row.Actual))
                {
                    continue;
                }

                if (includeWeights && (!row.Value.HasValue || !double.IsFinite(row.Value.Value)))
                {
                    continue;
                }

                scores.Add(row.Predicted);
                labels.Add(row.Actual >= ActualCutOff ? 1 : 0);
                weights?.Add(row.Value!.Value);
            }

            var positives = labels.Count(l => l == 1);

            if (labels.Count == 0 || positives == 0 || positives == labels.Count)
            {
                return null;
            }

            return (scores.ToArray(), labels.ToArray(), labels.Select(l => l == 1).ToArray(), weights?.ToArray());
        }
    }
}