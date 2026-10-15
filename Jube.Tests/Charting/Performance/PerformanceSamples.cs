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

using System;
using System.Collections.Generic;
using System.Linq;
using Jube.Charting;
using Jube.Charting.Interfaces;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Performance.Models;

namespace Jube.Test.Charting.Performance
{
    public static class PerformanceSamples
    {
        public static IReadOnlyDictionary<string, ISvgChart> All()
        {
            return new SortedDictionary<string, ISvgChart>(StringComparer.Ordinal)
            {
                ["performance-gains"] = Chart.Gains().Title("Cumulative Gains")
                    .Subtitle("Share of fraud captured by the highest scoring share of the population")
                    .FromScores("Model", PerformanceData.Actual, PerformanceData.Scores),
                ["performance-lift"] = Chart.Lift().Title("Lift by Decile")
                    .FromScores(PerformanceData.Actual, PerformanceData.Scores),
                ["performance-ks"] = Chart.Ks().Title("Kolmogorov-Smirnov").Classes("Fraud", "Genuine")
                    .FromScores(PerformanceData.Actual, PerformanceData.Scores).Threshold(0.5).Domain(0, 1),
                ["performance-threshold-sweep"] = Chart.ThresholdSweep().Title("Threshold Sweep")
                    .MinimumRiskThreshold(0.3)
                    .FromScores(PerformanceData.Actual, PerformanceData.Scores),
                ["performance-score-distribution"] = Chart.ScoreDistribution().Title("Score Distribution")
                    .Subtitle("Predicted probability by actual class").Classes("Fraud", "Genuine")
                    .FromScores(PerformanceData.Actual, PerformanceData.Scores, 0, 1, 10).Threshold(0.5),
                ["performance-metrics-panel"] = MetricsPanel(),
                ["performance-promotion-history"] = PromotionHistory(),
                ["network-emphasis"] = Emphasis(),
                ["network-elided"] = Network(60, [10], 1, NetworkDetail.Auto),
                ["network-density"] = Network(200, [100, 50], 1, NetworkDetail.Auto)
            };
        }

        public static double[][] Weights(int rows, int columns, int seed)
        {
            return
            [
                .. Enumerable.Range(0, rows).Select(r => Enumerable.Range(0, columns)
                    .Select(c =>
                        Math.Round(Math.Sin(r * 1.7 + c * 0.37 + seed) * (0.2 + 1.6 * ((r * 31 + c * 17) % 11) / 10.0),
                            3))
                    .ToArray())
            ];
        }

        public static NetworkDiagramBuilder NetworkBuilder(int inputs, int[] hidden, int outputs, NetworkDetail detail)
        {
            var builder = Diagram.Network().Title("Network " + inputs + " to " + string.Join(" to ", hidden) + " to " +
                                                  outputs)
                .Input(Enumerable.Range(1, inputs).Select(i => "x" + i)).Detail(detail);

            var previous = inputs;

            for (var l = 0; l < hidden.Length; l++)
            {
                builder.Hidden(Enumerable.Range(1, hidden[l]).Select(i => "h" + (l + 1) + "." + i),
                    Weights(hidden[l], previous, l), "Sigmoid, alpha 2",
                    [.. Enumerable.Range(0, hidden[l]).Select(i => Math.Round(Math.Cos(i) * 0.3, 3))]);

                previous = hidden[l];
            }

            return builder.Output(Enumerable.Range(1, outputs).Select(i => outputs == 1 ? "y" : "y" + i),
                Weights(outputs, previous, 9), "Sigmoid, alpha 2");
        }

        public static ISvgChart Network(int inputs, int[] hidden, int outputs, NetworkDetail detail)
        {
            return NetworkBuilder(inputs, hidden, outputs, detail);
        }

        public static ISvgChart Emphasis()
        {
            return Diagram.Network().Title("Input sensitivity").Input(["amount", "velocity", "country", "age"])
                .Hidden(["h1.1", "h1.2", "h1.3"], Weights(3, 4, 2), "Sigmoid, alpha 2")
                .Output(["y"], Weights(1, 3, 5), "Sigmoid, alpha 2")
                .InputEmphasis("Input sensitivity", [1.0, 0.45, 0.1, null], ["0.31", "0.14", "0.03", null]);
        }

        public static ISvgChart PromotionHistory()
        {
            var start = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);

            return Chart.PromotionHistory().Title("Promotion Over Time")
                .Promotion(start, 0.52, 8, 12, 12)
                .Promotion(start.AddHours(9), 0.61, 14, 40, 52)
                .Promotion(start.AddHours(30), 0.58, 22, 25, 77)
                .Promotion(start.AddHours(55), 0.74, 30, 130, 207)
                .Promotion(start.AddHours(80), 0.79, 41, 61, 268, true);
        }

        public static ISvgChart MetricsPanel()
        {
            return Chart.MetricsPanel().Title("Performance Measures")
                .Subtitle("40 test rows, fraud rate 30.0%, threshold 0.50")
                .Group("Ranking",
                [
                    new MetricTile("AUC", 0.8765, "95% interval 0.770 to 0.983", "Area under the ROC curve"),
                    new MetricTile("Gini", 0.753),
                    new MetricTile("KS", 0.643, "at score 0.35"),
                    new MetricTile("Average precision", 0.737)
                ])
                .Group("Probabilities",
                [
                    new MetricTile("Brier score", 0.153),
                    new MetricTile("Log loss", 0.469),
                    new MetricTile("Expected calibration error", 0.15)
                ])
                .Group("At threshold 0.50",
                [
                    new MetricTile("Accuracy", 0.775),
                    new MetricTile("Precision", 0.588),
                    new MetricTile("Recall", 0.833),
                    new MetricTile("F1", 0.69),
                    new MetricTile("MCC", double.NaN)
                ]);
        }
    }
}