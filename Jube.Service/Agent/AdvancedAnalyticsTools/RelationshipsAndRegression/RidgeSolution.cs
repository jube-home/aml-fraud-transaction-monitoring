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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal sealed class RidgeSolution
    {
        public double Lambda { get; init; }
        public double[] Standardised { get; init; } = Array.Empty<double>();
        public double[] Coefficients { get; init; } = Array.Empty<double>();
        public double Intercept { get; init; }
        public double EffectiveDegreesOfFreedom { get; init; }
        public double Rss { get; init; }
        public double RSquared { get; init; }
        public double Gcv { get; init; }
        public double[] StandardErrors { get; init; } = Array.Empty<double>();
        public double[] Fitted { get; init; } = Array.Empty<double>();
        public double ResidualStandardError { get; init; }

        public static RidgeSolution Solve(RidgeSetup s, double lambda)
        {
            var p = s.P;
            var m = new double[p][];

            for (var a = 0; a < p; a++)
            {
                m[a] = (double[])s.ZtZ[a].Clone();
                m[a][a] += lambda;
            }

            double[] beta;
            double[][] inverse;

            try
            {
                inverse = m.Inverse();
                beta = m.Solve(s.ZtY);
            }
            catch (Exception)
            {
                throw new ArgumentException(
                    "The predictors are perfectly collinear and lambda is 0, so the coefficients are not identifiable. Use a positive lambda or remove a redundant column.",
                    "lambda");
            }

            var trace = 0d;

            for (var a = 0; a < p; a++)
            {
                for (var b = 0; b < p; b++)
                {
                    trace += inverse[a][b] * s.ZtZ[b][a];
                }
            }

            var fittedCentred = s.Z.Select(r => r.Zip(beta, (v, c) => v * c).Sum()).ToArray();
            var rss = 0d;

            for (var i = 0; i < s.N; i++)
            {
                var e = s.CentredY[i] - fittedCentred[i];
                rss += e * e;
            }

            var residualDf = s.N - 1 - trace;
            var sigma2 = residualDf > 0 ? rss / residualDf : double.NaN;
            var sandwich = inverse.Dot(s.ZtZ).Dot(inverse);

            var standardErrors = Enumerable.Range(0, p)
                .Select(a => Math.Sqrt(Math.Max(0, sigma2 * sandwich[a][a])) / s.Deviations[a]).ToArray();

            var coefficients = beta.Select((b, a) => b / s.Deviations[a]).ToArray();
            var intercept = s.YMean - coefficients.Select((c, a) => c * s.Means[a]).Sum();

            return new RidgeSolution
            {
                Lambda = lambda,
                Standardised = beta,
                Coefficients = coefficients,
                Intercept = intercept,
                EffectiveDegreesOfFreedom = trace,
                Rss = rss,
                RSquared = s.Tss > 0 ? 1 - rss / s.Tss : double.NaN,
                Gcv = residualDf > 0 ? s.N * rss / (residualDf * residualDf) : double.NaN,
                StandardErrors = standardErrors,
                Fitted = fittedCentred.Select(v => v + s.YMean).ToArray(),
                ResidualStandardError = Math.Sqrt(sigma2)
            };
        }
    }
}