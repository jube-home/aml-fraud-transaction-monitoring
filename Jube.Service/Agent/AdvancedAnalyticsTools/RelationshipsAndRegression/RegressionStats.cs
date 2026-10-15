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
using Accord.Statistics.Distributions.Univariate;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal static class RegressionStats
    {
        public static double TwoSidedTp(double t, double degreesOfFreedom)
        {
            if (double.IsNaN(t) || !(degreesOfFreedom > 0))
            {
                return double.NaN;
            }

            if (double.IsInfinity(t))
            {
                return 0;
            }

            return Math.Min(1, 2 * new TDistribution(degreesOfFreedom).ComplementaryDistributionFunction(Math.Abs(t)));
        }

        public static double TQuantile(double probability, double degreesOfFreedom)
        {
            return new TDistribution(degreesOfFreedom).InverseDistributionFunction(probability);
        }

        public static double FUpperTail(double f, double numeratorDegrees, double denominatorDegrees)
        {
            if (double.IsNaN(f) || !(numeratorDegrees >= 1) || !(denominatorDegrees >= 1))
            {
                return double.NaN;
            }

            if (f <= 0)
            {
                return 1;
            }

            if (double.IsInfinity(f))
            {
                return 0;
            }

            return new FDistribution((int)Math.Round(numeratorDegrees), (int)Math.Round(denominatorDegrees))
                .ComplementaryDistributionFunction(f);
        }

        public static double ChiSquareUpperTail(double statistic, double degreesOfFreedom)
        {
            if (double.IsNaN(statistic) || !(degreesOfFreedom >= 1))
            {
                return double.NaN;
            }

            if (statistic <= 0)
            {
                return 1;
            }

            if (double.IsInfinity(statistic))
            {
                return 0;
            }

            return new ChiSquareDistribution((int)Math.Round(degreesOfFreedom))
                .ComplementaryDistributionFunction(statistic);
        }

        public static double NormalTwoSided(double z)
        {
            if (double.IsNaN(z))
            {
                return double.NaN;
            }

            return Math.Min(1, 2 * NormalDistribution.Standard.ComplementaryDistributionFunction(Math.Abs(z)));
        }

        public static double NormalQuantile(double probability)
        {
            return NormalDistribution.Standard.InverseDistributionFunction(probability);
        }

        public static double[] BenjaminiHochberg(double[] pValues)
        {
            var m = pValues.Length;
            var adjusted = new double[m];

            var order = Enumerable.Range(0, m).OrderBy(i => double.IsNaN(pValues[i]) ? double.MaxValue : pValues[i])
                .ToArray();

            var valid = pValues.Count(p => !double.IsNaN(p));
            var running = 1d;

            for (var rank = m - 1; rank >= 0; rank--)
            {
                var index = order[rank];

                if (double.IsNaN(pValues[index]))
                {
                    adjusted[index] = double.NaN;
                    continue;
                }

                running = Math.Min(running, pValues[index] * valid / (rank + 1));
                adjusted[index] = running;
            }

            return adjusted;
        }

        public static double Strength(double coefficient)
        {
            return Math.Abs(coefficient);
        }

        public static string DescribeStrength(double coefficient)
        {
            if (double.IsNaN(coefficient))
            {
                return "undefined";
            }

            var magnitude = Math.Abs(coefficient);
            var direction = coefficient > 0 ? "positive" : coefficient < 0 ? "negative" : "no";
            string size;

            if (magnitude < 0.1)
            {
                size = "negligible";
            }
            else if (magnitude < 0.3)
            {
                size = "weak";
            }
            else if (magnitude < 0.5)
            {
                size = "moderate";
            }
            else if (magnitude < 0.8)
            {
                size = "strong";
            }
            else
            {
                size = "very strong";
            }

            return magnitude < 0.1 ? "negligible relationship" : $"{size} {direction} relationship";
        }
    }
}