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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class ModelEvaluationGuard
    {
        public static void Features(double[][] inputs, string[] variableNames,
            int maximumRows = ModelEvaluationLimits.MaximumRows)
        {
            if (inputs == null || inputs.Length == 0)
            {
                throw new ArgumentException("Give at least one row of predictor values.", nameof(inputs));
            }

            if (variableNames == null || variableNames.Length == 0)
            {
                throw new ArgumentException("Give the name of every predictor column.", nameof(variableNames));
            }

            if (variableNames.Length > ModelEvaluationLimits.MaximumFeatures)
            {
                throw new ArgumentException(
                    $"At most {ModelEvaluationLimits.MaximumFeatures} predictor columns are supported; select the most relevant ones first.",
                    nameof(variableNames));
            }

            if (variableNames.Any(string.IsNullOrWhiteSpace) ||
                variableNames.Distinct(StringComparer.Ordinal).Count() != variableNames.Length)
            {
                throw new ArgumentException("Variable names must be non-blank and unique.", nameof(variableNames));
            }

            if (inputs.Length > maximumRows)
            {
                throw new ArgumentException(
                    $"At most {maximumRows} rows are supported by this tool; give a stratified sample instead.",
                    nameof(inputs));
            }

            if (inputs.Length < ModelEvaluationLimits.MinimumRows)
            {
                throw new ArgumentException(
                    $"At least {ModelEvaluationLimits.MinimumRows} rows are needed to split into train, validation and test sets.",
                    nameof(inputs));
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null || inputs[i].Length != variableNames.Length)
                {
                    throw new ArgumentException(
                        $"Row {i} does not have {variableNames.Length} values, one per variable name.", nameof(inputs));
                }

                for (var j = 0; j < inputs[i].Length; j++)
                {
                    if (double.IsInfinity(inputs[i][j]))
                    {
                        throw new ArgumentException(
                            $"Row {i}, variable '{variableNames[j]}' is infinite; use NaN for a missing value.",
                            nameof(inputs));
                    }
                }
            }
        }

        public static void BinaryLabels(int[] labels, int rows)
        {
            if (labels == null || labels.Length != rows)
            {
                throw new ArgumentException("There must be exactly one label per row.", nameof(labels));
            }

            if (labels.Any(l => l != 0 && l != 1))
            {
                throw new ArgumentException("Labels must be 0 or 1.", nameof(labels));
            }

            var positives = labels.Count(l => l == 1);

            if (positives < 4 || labels.Length - positives < 4)
            {
                throw new ArgumentException(
                    "Each class needs at least 4 rows to split into train, validation and test.",
                    nameof(labels));
            }
        }

        public static void Targets(double[] targets, int rows)
        {
            if (targets == null || targets.Length != rows)
            {
                throw new ArgumentException("There must be exactly one target value per row.", nameof(targets));
            }

            if (targets.Any(t => !double.IsFinite(t)))
            {
                throw new ArgumentException("Every target must be a finite number; remove rows with a missing target.",
                    nameof(targets));
            }

            if (targets.Distinct().Count() < 2)
            {
                throw new ArgumentException("The target is constant, so there is nothing to predict.", nameof(targets));
            }
        }

        public static void Fractions(double validationFraction, double testFraction)
        {
            if (!double.IsFinite(validationFraction) || validationFraction < 0 || validationFraction > 0.5)
            {
                throw new ArgumentException("The validation fraction must be between 0 and 0.5.",
                    nameof(validationFraction));
            }

            if (!double.IsFinite(testFraction) || testFraction <= 0 || testFraction > 0.5)
            {
                throw new ArgumentException("The test fraction must be above 0 and at most 0.5.", nameof(testFraction));
            }

            if (validationFraction + testFraction > 0.8)
            {
                throw new ArgumentException("The validation and test fractions together must not exceed 0.8.",
                    nameof(testFraction));
            }
        }

        public static void Folds(int folds, string paramName = "folds")
        {
            if (folds != 0 && (folds < 2 || folds > ModelEvaluationLimits.MaximumFolds))
            {
                throw new ArgumentException(
                    $"Cross validation folds must be 0 (single split) or between 2 and {ModelEvaluationLimits.MaximumFolds}.",
                    paramName);
            }
        }

        public static void StrictFolds(int folds, string paramName = "folds")
        {
            if (folds < 2 || folds > ModelEvaluationLimits.MaximumFolds)
            {
                throw new ArgumentException($"Folds must be between 2 and {ModelEvaluationLimits.MaximumFolds}.",
                    paramName);
            }
        }

        public static void IntRange(int value, int minimum, int maximum, string name, string what)
        {
            if (value < minimum || value > maximum)
            {
                throw new ArgumentException($"{what} must be between {minimum} and {maximum}.", name);
            }
        }

        public static void DoubleRange(double value, double minimum, double maximum, string name, string what)
        {
            if (!double.IsFinite(value) || value < minimum || value > maximum)
            {
                throw new ArgumentException($"{what} must be a number between {minimum} and {maximum}.", name);
            }
        }

        public static void PositiveFinite(double value, string name, string what)
        {
            if (!double.IsFinite(value) || value <= 0)
            {
                throw new ArgumentException($"{what} must be a positive finite number.", name);
            }
        }

        public static void Sweep(int[]? values, string name, int minimum, int maximum, string what)
        {
            if (values == null)
            {
                return;
            }

            if (values.Length > ModelEvaluationLimits.MaximumSweepValues)
            {
                throw new ArgumentException($"Give at most {ModelEvaluationLimits.MaximumSweepValues} values.", name);
            }

            if (values.Any(v => v < minimum || v > maximum))
            {
                throw new ArgumentException($"Every {what} must be between {minimum} and {maximum}.", name);
            }
        }

        public static void Sweep(double[]? values, string name, double minimum, double maximum, string what)
        {
            if (values == null)
            {
                return;
            }

            if (values.Length > ModelEvaluationLimits.MaximumSweepValues)
            {
                throw new ArgumentException($"Give at most {ModelEvaluationLimits.MaximumSweepValues} values.", name);
            }

            if (values.Any(v => !double.IsFinite(v) || v < minimum || v > maximum))
            {
                throw new ArgumentException($"Every {what} must be a number between {minimum} and {maximum}.", name);
            }
        }

        public static void HiddenLayers(int[]? layers, string name)
        {
            if (layers == null || layers.Length == 0)
            {
                return;
            }

            if (layers.Length > ModelEvaluationLimits.MaximumHiddenLayers ||
                layers.Any(l => l < 1 || l > ModelEvaluationLimits.MaximumHiddenUnits))
            {
                throw new ArgumentException(
                    $"Give at most {ModelEvaluationLimits.MaximumHiddenLayers} hidden layers of 1 to {ModelEvaluationLimits.MaximumHiddenUnits} units each.",
                    name);
            }
        }

        public static string Format(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}