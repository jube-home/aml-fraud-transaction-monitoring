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

using Accord.Math;
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    internal static class CategoricalLibrary
    {
        public static double NormalQuantile(double probability)
        {
            return NormalDistribution.Standard.InverseDistributionFunction(probability);
        }

        public static double NormalCdf(double z)
        {
            return NormalDistribution.Standard.DistributionFunction(z);
        }

        public static double NormalSurvival(double z)
        {
            return NormalDistribution.Standard.ComplementaryDistributionFunction(z);
        }

        public static double ChiSquareSurvival(double statistic, int degreesOfFreedom)
        {
            if (degreesOfFreedom < 1 || double.IsNaN(statistic))
            {
                return double.NaN;
            }

            if (statistic <= 0)
            {
                return 1d;
            }

            return new ChiSquareDistribution(degreesOfFreedom).ComplementaryDistributionFunction(statistic);
        }

        public static double ChiSquareQuantile(double probability, double degreesOfFreedom)
        {
            return new ChiSquareDistribution((int)degreesOfFreedom).InverseDistributionFunction(probability);
        }

        public static double BetaQuantile(double probability, double alpha, double beta)
        {
            return new BetaDistribution(alpha, beta).InverseDistributionFunction(probability);
        }

        public static double BinomialCdf(int successes, int trials, double probability)
        {
            return new BinomialDistribution(trials, probability).DistributionFunction(successes);
        }

        public static double BinomialSurvivalInclusive(int successes, int trials, double probability)
        {
            if (successes <= 0)
            {
                return 1d;
            }

            return 1d - new BinomialDistribution(trials, probability).DistributionFunction(successes - 1);
        }

        public static double BinomialPmf(int successes, int trials, double probability)
        {
            return new BinomialDistribution(trials, probability).ProbabilityMassFunction(successes);
        }

        public static double[] BinomialMassFunction(int trials, double probability)
        {
            var distribution = new BinomialDistribution(trials, probability);
            var masses = new double[trials + 1];

            for (var k = 0; k <= trials; k++)
            {
                masses[k] = distribution.ProbabilityMassFunction(k);
            }

            return masses;
        }

        public static double PoissonCdf(int events, double mean)
        {
            return new PoissonDistribution(mean).DistributionFunction(events);
        }

        public static (double Kappa, double StandardError) Kappa(int[,] matrix, int weighting)
        {
            if (weighting == 1)
            {
                var linear = WeightedConfusionMatrix.LinearWeighting(matrix);

                return (linear.WeightedKappa, linear.WeightedStandardError);
            }

            if (weighting == 2)
            {
                var quadratic = WeightedConfusionMatrix.QuadraticWeighting(matrix);

                return (quadratic.WeightedKappa, quadratic.WeightedStandardError);
            }

            var general = new GeneralConfusionMatrix(matrix);

            return (general.Kappa, general.StandardError);
        }

        public static double[] SolveLinearSystem(double[][] matrix, double[] rightSide)
        {
            return matrix.Solve(rightSide);
        }

        public static (double[] Coefficients, double[] StandardErrors, double PartialLogLikelihood, bool Converged, int
            Iterations)
            FitProportionalHazards(double[][] covariates, double[] rankedTimes, int[] events, int maxIterations,
                double tolerance)
        {
            var parameters = covariates[0].Length;
            var model = new ProportionalHazards(parameters);

            var learner = new ProportionalHazardsNewtonRaphson(model)
            {
                MaxIterations = maxIterations,
                Tolerance = tolerance
            };

            var fitted = learner.Learn(covariates, rankedTimes, events);
            var likelihood = fitted.GetPartialLogLikelihood(covariates, rankedTimes, events);

            return ((double[])fitted.Coefficients.Clone(), (double[])fitted.StandardErrors.Clone(), likelihood,
                learner.HasConverged, learner.CurrentIteration);
        }

        public static double NullPartialLogLikelihood(double[][] covariates, double[] rankedTimes, int[] events)
        {
            var model = new ProportionalHazards(covariates[0].Length);

            return model.GetPartialLogLikelihood(covariates, rankedTimes, events);
        }
    }
}