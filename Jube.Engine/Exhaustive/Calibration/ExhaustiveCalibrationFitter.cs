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
using System.Text.Json;
using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Analytics.Calibration;
using Jube.Engine.Exhaustive.Calibration.Models;

namespace Jube.Engine.Exhaustive.Calibration
{
    internal static class ExhaustiveCalibrationFitter
    {
        internal const int MinimumPerClass = 20;
        internal const int LowSamplePerClass = 50;
        internal const double Epsilon = 1e-6;

        internal static IActivationFunction OutputFunction(ActivationNetwork network)
        {
            var layer = network.Layers[^1];

            return layer.Neurons.Length > 0 && layer.Neurons[0] is ActivationNeuron neuron
                ? neuron.ActivationFunction
                : null;
        }

        internal static OutputFamily Classify(IActivationFunction function)
        {
            return function switch
            {
                ThresholdFunction or BernoulliFunction => OutputFamily.Hard,
                SigmoidFunction => OutputFamily.Logit,
                _ => OutputFamily.Raw
            };
        }

        internal static double[] Scores(ActivationNetwork network, double[][] data)
        {
            var scores = new double[data.Length];

            for (var i = 0; i < data.Length; i++)
            {
                scores[i] = network.Compute(data[i])[0];
            }

            return scores;
        }

        internal static Outcome Fit(ActivationNetwork network, double[][] validationData,
            double[][] validationOutputs, double[][] testData, double[][] testOutputs, double[][] trainingOutputs,
            BaseRateResolution baseRate, string requestedMethod = ExhaustiveCalibrationRequest.Platt)
        {
            return Fit(OutputFunction(network), Scores(network, validationData),
                [.. validationOutputs.Select(o => o[0])], Scores(network, testData),
                [.. testOutputs.Select(o => o[0])], Rate([.. trainingOutputs.Select(o => o[0])]),
                baseRate, requestedMethod);
        }

        internal static Outcome Fit(IActivationFunction outputFunction, double[] validationScores,
            double[] validationLabels, double[] testScores, double[] testLabels, double? trainingPrior,
            BaseRateResolution baseRate, string requestedMethod = ExhaustiveCalibrationRequest.Platt)
        {
            baseRate ??= BaseRateResolution.Unavailable;
            var labels = validationLabels.Select(l => l > 0.5 ? 1 : 0).ToArray();
            var positives = labels.Sum();
            var negatives = labels.Length - positives;
            var family = Classify(outputFunction);

            string reason = null;

            if (family == OutputFamily.Hard)
            {
                reason =
                    $"The output activation function ({outputFunction?.GetType().Name}) gives hard 0 or 1 outputs, for which calibration is degenerate.";
            }
            else if (labels.Length == 0 || positives == 0 || negatives == 0)
            {
                reason = "The validation split has a single class.";
            }
            else if (Math.Min(positives, negatives) < MinimumPerClass)
            {
                reason =
                    $"The validation split has {positives} positives and {negatives} negatives; at least {MinimumPerClass} of each are needed.";
            }
            else if (validationScores.Any(s => !double.IsFinite(s)))
            {
                reason = "The validation scores are not all finite.";
            }
            else
            {
                var distinct = validationScores.Distinct().Count();

                if (distinct == 1)
                {
                    reason = "The validation scores are constant.";
                }
                else if (distinct == 2)
                {
                    reason = "The validation scores take only two distinct values, which is a hard output.";
                }
            }

            Candidate chosen = null;
            var candidateReasons = new List<string>();

            if (reason == null)
            {
                var requested = ExhaustiveCalibrationRequest.OrDefault(requestedMethod);

                try
                {
                    chosen = requested == ExhaustiveCalibrationRequest.Automatic
                        ? PickAutomatic(family, validationScores, labels, candidateReasons)
                        : FitOne(requested, family, validationScores, labels);
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    reason = ex.Message;
                }

                if (chosen == null && reason == null)
                {
                    reason = candidateReasons.Count > 0
                        ? string.Join(" ", candidateReasons)
                        : "No calibration method could be fitted to this data.";
                }
            }

            var validationPrior = labels.Length == 0 ? (double?)null : positives / (double)labels.Length;

            if (chosen == null)
            {
                return new Outcome
                {
                    Method = ExhaustiveCalibration.MethodNone,
                    TrainingPrior = trainingPrior,
                    CalibrationPrior = validationPrior,
                    BaseRate = baseRate.Rate,
                    BaseRateSource = baseRate.Source,
                    SampleSize = labels.Length,
                    Status = ExhaustiveCalibration.StatusSkipped,
                    Reason = reason
                };
            }

            var applied = baseRate.IsUsable && validationPrior is > 0 and < 1 ? baseRate.Rate : null;

            var status = Math.Min(positives, negatives) < LowSamplePerClass
                ? ExhaustiveCalibration.StatusCalibratedLowSample
                : ExhaustiveCalibration.StatusCalibrated;

            var isPlatt = chosen.Method == ExhaustiveCalibration.MethodPlatt;
            var plattForLogging = isPlatt ? DecodePlattForLogging(chosen.ParametersJson) : null;

            return new Outcome
            {
                Method = chosen.Method,
                Input = family == OutputFamily.Logit
                    ? ExhaustiveCalibration.InputLogit
                    : ExhaustiveCalibration.InputRaw,
                Slope = plattForLogging?.Slope,
                Intercept = plattForLogging?.Intercept,
                ParametersJson = isPlatt ? null : chosen.ParametersJson,
                CalibratorEpsilon = isPlatt ? plattForLogging?.Epsilon : Epsilon,
                TrainingPrior = trainingPrior,
                CalibrationPrior = validationPrior,
                BaseRate = applied,
                BaseRateSource = applied.HasValue ? baseRate.Source : BaseRateResolution.SourceSamplePrior,
                SampleSize = labels.Length,
                Status = status,
                Reason = status == ExhaustiveCalibration.StatusCalibratedLowSample
                    ? $"Fitted on {positives} positives and {negatives} negatives, fewer than {LowSamplePerClass} of the smaller class."
                    : null,
                Quality = validationPrior is { } resolvedValidationPrior
                    ? BuildQuality(chosen.Apply, validationScores, labels, testScores,
                        [.. testLabels.Select(l => l > 0.5 ? 1 : 0)], resolvedValidationPrior, applied)
                    : throw new InvalidOperationException(
                        "validationPrior is always set once a calibration method has been chosen.")
            };
        }

        private static Candidate FitOne(string method, OutputFamily family, double[] scores, int[] labels)
        {
            switch (method)
            {
                case ExhaustiveCalibrationRequest.Platt:
                {
                    var mode = family == OutputFamily.Logit
                        ? CalibrationInput.LogitOfBoundedScore
                        : CalibrationInput.RawScore;

                    var fitted = PlattCalibration.Fit(scores, labels, null, mode);

                    if (!fitted.IsIncreasing)
                    {
                        throw new InvalidOperationException(
                            "The fitted Platt slope is not positive: calibration would invert the model's ranking.");
                    }

                    return new Candidate(ExhaustiveCalibration.MethodPlatt, fitted.Apply,
                        JsonSerializer.Serialize(fitted.ToPrimitives()));
                }

                case ExhaustiveCalibrationRequest.Isotonic:
                {
                    var fitted = IsotonicCalibrator.Fit(scores, labels);

                    return new Candidate(ExhaustiveCalibration.MethodIsotonic, fitted.Apply, fitted.ToJson());
                }

                case ExhaustiveCalibrationRequest.Beta:
                {
                    if (family != OutputFamily.Logit)
                    {
                        throw new InvalidOperationException(
                            "Beta calibration needs a bounded [0,1] score family; this network's output " +
                            "activation is not a sigmoid.");
                    }

                    var fitted = BetaCalibration.Fit(scores, labels);

                    if (!IsMonotoneNonDecreasing(fitted.Apply, scores))
                    {
                        throw new InvalidOperationException(
                            "The fitted beta calibrator is not monotone over the observed scores.");
                    }

                    return new Candidate(ExhaustiveCalibration.MethodBeta, fitted.Apply,
                        JsonSerializer.Serialize(fitted.ToPrimitives()));
                }

                default:
                    throw new InvalidOperationException($"The calibration method '{method}' is not known.");
            }
        }

        private static Candidate PickAutomatic(OutputFamily family, double[] scores, int[] labels,
            List<string> reasons)
        {
            var candidates = new List<Candidate>();

            foreach (var method in new[]
                     {
                         ExhaustiveCalibrationRequest.Platt, ExhaustiveCalibrationRequest.Isotonic,
                         ExhaustiveCalibrationRequest.Beta
                     })
            {
                if (method == ExhaustiveCalibrationRequest.Beta && family != OutputFamily.Logit)
                {
                    continue;
                }

                try
                {
                    candidates.Add(FitOne(method, family, scores, labels));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    reasons.Add($"{method}: {ex.Message}");
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates
                .Select(c => (Candidate: c, Brier: BinaryMetrics.BrierScore(c.Apply(scores), labels)))
                .OrderBy(c => c.Brier)
                .First().Candidate;
        }

        private static bool IsMonotoneNonDecreasing(Func<double, double> apply, double[] observedScores)
        {
            var probes = observedScores.Distinct().OrderBy(s => s).ToArray();
            var previous = double.NegativeInfinity;

            foreach (var value in probes.Select(apply))
            {
                if (value < previous - 1e-9)
                {
                    return false;
                }

                previous = value;
            }

            return true;
        }

        private static (double Slope, double Intercept, double Epsilon)? DecodePlattForLogging(string parametersJson)
        {
            var primitives = JsonSerializer.Deserialize<double[]>(parametersJson);

            return primitives is { Length: 4 } ? (primitives[0], primitives[1], primitives[3]) : null;
        }

        internal static double? Rate(double[] labels)
        {
            return labels.Length == 0 ? null : labels.Count(l => l > 0.5) / (double)labels.Length;
        }

        private static double[] Clamp(double[] scores)
        {
            return [.. scores.Select(s => double.IsNaN(s) ? s : Math.Min(Math.Max(s, 0d), 1d))];
        }

        private static double? Finite(double value)
        {
            return double.IsFinite(value) ? value : null;
        }

        private static SplitQuality BuildSplit(Func<double[], double[]> apply, double[] scores, int[] labels)
        {
            var quality = new SplitQuality { Rows = labels.Length, Positives = labels.Sum() };

            if (labels.Length == 0)
            {
                return quality;
            }

            var raw = Clamp(scores);
            var calibrated = apply(scores);
            quality.PositiveRate = quality.Positives / (double)labels.Length;
            quality.MeanRaw = Finite(raw.Where(double.IsFinite).DefaultIfEmpty(double.NaN).Average());
            quality.MeanCalibrated = Finite(calibrated.Where(double.IsFinite).DefaultIfEmpty(double.NaN).Average());
            quality.Ece.Raw = Finite(BinaryMetrics.ExpectedCalibrationError(raw, labels));
            quality.Ece.Calibrated = Finite(BinaryMetrics.ExpectedCalibrationError(calibrated, labels));
            quality.Brier.Raw = Finite(BinaryMetrics.BrierScore(raw, labels));
            quality.Brier.Calibrated = Finite(BinaryMetrics.BrierScore(calibrated, labels));
            quality.LogLoss.Raw = Finite(BinaryMetrics.LogLoss(raw, labels));
            quality.LogLoss.Calibrated = Finite(BinaryMetrics.LogLoss(calibrated, labels));

            return quality;
        }

        private static CalibrationQuality BuildQuality(Func<double[], double[]> apply, double[] validationScores,
            int[] validationLabels, double[] testScores, int[] testLabels, double calibrationPrior, double? baseRate)
        {
            var quality = new CalibrationQuality
            {
                Validation = BuildSplit(apply, validationScores, validationLabels),
                Test = BuildSplit(apply, testScores, testLabels)
            };

            var testPositives = testLabels.Sum();

            if (baseRate.HasValue && testPositives > 0 && testPositives < testLabels.Length)
            {
                var testPrior = testPositives / (double)testLabels.Length;

                var weights = testLabels.Select(l => l == 1
                    ? baseRate.Value / testPrior
                    : (1 - baseRate.Value) / (1 - testPrior)).ToArray();

                var shifted = PriorShift.Apply(apply(testScores), calibrationPrior, baseRate.Value);
                var raw = Clamp(testScores);

                quality.TestAtBaseRate = new BaseRateQuality
                {
                    BaseRate = baseRate.Value,
                    Brier =
                    {
                        Raw = Finite(BinaryMetrics.BrierScore(raw, testLabels, weights)),
                        Calibrated = Finite(BinaryMetrics.BrierScore(shifted, testLabels, weights))
                    },
                    LogLoss =
                    {
                        Raw = Finite(BinaryMetrics.LogLoss(raw, testLabels, weights)),
                        Calibrated = Finite(BinaryMetrics.LogLoss(shifted, testLabels, weights))
                    }
                };
            }

            return quality;
        }

        internal enum OutputFamily
        {
            Logit,
            Raw,
            Hard
        }

        private sealed record Candidate(string Method, Func<double[], double[]> Apply, string ParametersJson);
    }
}