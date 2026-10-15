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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class RegressionToolInput
    {
        public static EvaluationOptions Validate(double[][] inputs, double[] targets, string[] variableNames,
            double validationFraction, double testFraction, int seed, int crossValidationFolds, int permutationRepeats,
            int maximumRows = ModelEvaluationLimits.MaximumRows)
        {
            ModelEvaluationGuard.Features(inputs, variableNames, maximumRows);
            ModelEvaluationGuard.Targets(targets, inputs.Length);
            ModelEvaluationGuard.Fractions(validationFraction, testFraction);
            ModelEvaluationGuard.Folds(crossValidationFolds, nameof(crossValidationFolds));

            ModelEvaluationGuard.IntRange(permutationRepeats, 0, ModelEvaluationLimits.MaximumPermutationRepeats,
                nameof(permutationRepeats), "The number of permutation repeats");

            return new EvaluationOptions(validationFraction, testFraction, crossValidationFolds, seed, false, null,
                permutationRepeats);
        }
    }
}