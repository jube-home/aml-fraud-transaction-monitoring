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
    internal static class ConfigFactory
    {
        public static LearnerConfig Build(EvaluationFamily family, HyperParameter[]? parameters, double[]? values,
            ClassImbalanceHandling imbalance, TreeAlgorithm treeAlgorithm, SvmKernel kernel, NaiveBayesKind bayesKind,
            string parameterName)
        {
            var config = new LearnerConfig
            {
                Family = family,
                Imbalance = imbalance,
                Algorithm = treeAlgorithm,
                Kernel = kernel,
                BayesKind = bayesKind,
                Trees = 100,
                MaxDepth = family switch
                {
                    EvaluationFamily.AdaBoost => 2,
                    EvaluationFamily.GradientBoosting => 3,
                    EvaluationFamily.RandomForest or EvaluationFamily.Bagging => 10,
                    _ => 6
                },
                Epochs = 100
            };

            if (parameters == null && values == null)
            {
                return config;
            }

            if (parameters == null || values == null || parameters.Length != values.Length)
            {
                throw new ArgumentException("Give one value for each fixed hyper-parameter, in the same order.",
                    parameterName);
            }

            if (parameters.Distinct().Count() != parameters.Length)
            {
                throw new ArgumentException("Each fixed hyper-parameter may be given only once.", parameterName);
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                config = config.With(parameters[i], values[i], parameterName);
            }

            return config;
        }

        public static RegressorConfig BuildRegressor(RegressionFamily family, HyperParameter[]? parameters,
            double[]? values,
            LinearPenalty penalty, SvmKernel kernel, string parameterName)
        {
            var config = new RegressorConfig
            {
                Family = family,
                Penalty = penalty,
                Kernel = kernel,
                MaxDepth = family switch
                {
                    RegressionFamily.GradientBoosting => 3,
                    RegressionFamily.RandomForest => 12,
                    _ => 6
                },
                Epochs = 200
            };

            if (parameters == null && values == null)
            {
                return config;
            }

            if (parameters == null || values == null || parameters.Length != values.Length)
            {
                throw new ArgumentException("Give one value for each fixed hyper-parameter, in the same order.",
                    parameterName);
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                config = config.With(parameters[i], values[i], parameterName);
            }

            return config;
        }
    }
}