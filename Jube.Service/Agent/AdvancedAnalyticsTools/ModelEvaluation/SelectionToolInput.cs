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

using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class SelectionToolInput
    {
        public static int RowLimit(EvaluationFamily family, int normal)
        {
            return family == EvaluationFamily.KernelSupportVector
                ? Math.Min(normal, ModelEvaluationLimits.MaximumKernelRows)
                : normal;
        }

        public static EvaluationOptions Common(double[][] inputs, int[] labels, string[] variableNames,
            EvaluationFamily family,
            int normalRowLimit, double validationFraction, double testFraction, int seed, int folds, int repeats)
        {
            return ClassificationToolInput.Validate(inputs, labels, variableNames, validationFraction, testFraction,
                seed,
                folds, null, repeats, true, RowLimit(family, normalRowLimit));
        }

        public static LearnerConfig Config(EvaluationFamily family, HyperParameter[]? fixedParameters,
            double[]? fixedValues,
            ClassImbalanceHandling imbalance, TreeAlgorithm treeAlgorithm, SvmKernel kernel, NaiveBayesKind bayesKind)
        {
            var config = ConfigFactory.Build(family, fixedParameters, fixedValues, imbalance, treeAlgorithm, kernel,
                bayesKind, nameof(fixedParameters));

            if (family == EvaluationFamily.NaiveBayes && bayesKind == NaiveBayesKind.TreeAugmented)
            {
                return config with { Family = EvaluationFamily.TreeAugmentedNaiveBayes };
            }

            return config;
        }

        public static void ValidateSearch(int folds, SelectionMetric metric)
        {
            ModelEvaluationGuard.StrictFolds(folds);

            if (!Enum.IsDefined(metric))
            {
                throw new ArgumentException("Unknown metric.", nameof(metric));
            }
        }
    }
}