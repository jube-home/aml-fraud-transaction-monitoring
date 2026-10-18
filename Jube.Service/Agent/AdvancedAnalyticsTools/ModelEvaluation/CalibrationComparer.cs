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

using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class CalibrationComparer
    {
        public static (CalibrationMethodRow[] Methods, string Best, List<EvaluationWarning> Warnings) Compare(
            double[] calibrationScores, int[] calibrationLabels, double[] testScores, int[] testLabels,
            bool scoresAreProbabilities)
        {
            var warnings = new List<EvaluationWarning>();

            double[] Raw(double[] scores)
            {
                return Clamp(scoresAreProbabilities ? scores : scores.Select(ModelMath.Sigmoid));
            }

            var methods = new List<CalibrationMethodRow>
            {
                Row(scoresAreProbabilities ? "Uncalibrated" : "UncalibratedSigmoid", Raw(testScores), testLabels)
            };

            var bothCalibration = calibrationLabels.Any(v => v == 1) && calibrationLabels.Any(v => v == 0);

            if (bothCalibration)
            {
                try
                {
                    var mode = scoresAreProbabilities
                        ? CalibrationInput.LogitOfBoundedScore
                        : CalibrationInput.RawScore;

                    var platt = PlattCalibration.Fit(calibrationScores, calibrationLabels, null, mode);
                    methods.Add(Row("Platt", Clamp(platt.Apply(testScores)), testLabels));
                }
                catch (InvalidOperationException exception)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.CalibrationSkipped,
                        "Platt scaling could not be fitted: " + exception.Message));
                }

                var isotonic = FitIsotonic(calibrationScores, calibrationLabels);
                methods.Add(Row("Isotonic", Clamp(testScores.Select(isotonic.Apply)), testLabels));

                if (calibrationLabels.Length < 200 ||
                    Math.Min(calibrationLabels.Count(v => v == 1), calibrationLabels.Count(v => v == 0)) < 30)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SmallSample,
                        "The calibration set is small, so isotonic regression is likely to overfit its steps; prefer Platt scaling unless it is clearly better on a large test set."));
                }
            }
            else
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.CalibrationSkipped,
                    "The calibration rows do not contain both classes, so no calibration method could be fitted."));
            }

            var rate = calibrationLabels.Length == 0 ? 0.5 : calibrationLabels.Average(v => (double)v);
            methods.Add(Row("ConstantBaseRate", Clamp(testLabels.Select(_ => rate)), testLabels));

            var best = methods.Where(m => m.Method != "ConstantBaseRate" && m.LogLoss.HasValue)
                .OrderBy(m => m.LogLoss!.Value).Select(m => m.Method).FirstOrDefault() ?? "Uncalibrated";

            return (methods.ToArray(), best, warnings);
        }

        private static Isotonic FitIsotonic(double[] scores, int[] labels)
        {
            var order = Enumerable.Range(0, scores.Length).OrderBy(i => scores[i]).ToArray();
            var blocks = new List<(double Sum, double Count, double Low, double High)>();

            foreach (var i in order)
            {
                blocks.Add((labels[i], 1d, scores[i], scores[i]));

                while (blocks.Count > 1 && blocks[^2].Sum / blocks[^2].Count >= blocks[^1].Sum / blocks[^1].Count)
                {
                    var b = blocks[^1];
                    var a = blocks[^2];
                    blocks.RemoveRange(blocks.Count - 2, 2);
                    blocks.Add((a.Sum + b.Sum, a.Count + b.Count, a.Low, b.High));
                }
            }

            var thresholds = blocks.Select(b => (b.Low + b.High) / 2d).ToArray();
            var values = blocks.Select(b => Math.Min(1d - 1e-6, Math.Max(1e-6, b.Sum / b.Count))).ToArray();

            for (var i = 1; i < thresholds.Length; i++)
            {
                if (thresholds[i] <= thresholds[i - 1])
                {
                    thresholds[i] = thresholds[i - 1] + 1e-12;
                }
            }

            return new Isotonic(thresholds, values);
        }

        private static double[] Clamp(IEnumerable<double> values)
        {
            return values.Select(v => Math.Min(1d - 1e-9, Math.Max(1e-9, v))).ToArray();
        }

        private static CalibrationMethodRow Row(string method, double[] probabilities, int[] labels)
        {
            double? slope;
            double? intercept = null;

            try
            {
                var logits = probabilities.Select(p => Math.Log(Math.Min(1d - 1e-6, Math.Max(1e-6, p)) /
                                                                (1d - Math.Min(1d - 1e-6, Math.Max(1e-6, p)))))
                    .ToArray();

                var fit = PlattCalibration.Fit(logits, labels);
                slope = ModelMath.Finite(fit.Slope);
                intercept = ModelMath.Finite(fit.Intercept);
            }
            catch (InvalidOperationException)
            {
                slope = null;
            }

            return new CalibrationMethodRow(method,
                ModelMath.Finite(BinaryMetrics.LogLoss(probabilities, labels)),
                ModelMath.Finite(BinaryMetrics.BrierScore(probabilities, labels)),
                ModelMath.Finite(BinaryMetrics.ExpectedCalibrationError(probabilities, labels)),
                ModelMath.Finite(BinaryMetrics.RocAuc(probabilities, labels)),
                slope,
                intercept,
                probabilities.Average());
        }

        private sealed record Isotonic(double[] Thresholds, double[] Values)
        {
            public double Apply(double score)
            {
                if (Thresholds.Length == 0)
                {
                    return 0.5;
                }

                if (score <= Thresholds[0])
                {
                    return Values[0];
                }

                if (score >= Thresholds[^1])
                {
                    return Values[^1];
                }

                var index = Array.BinarySearch(Thresholds, score);

                if (index >= 0)
                {
                    return Values[index];
                }

                var upper = ~index;
                var lower = upper - 1;
                var t = (score - Thresholds[lower]) / (Thresholds[upper] - Thresholds[lower]);

                return Values[lower] + t * (Values[upper] - Values[lower]);
            }
        }
    }
}