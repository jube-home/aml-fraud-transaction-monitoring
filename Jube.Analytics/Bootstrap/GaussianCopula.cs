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

using Jube.Analytics.Bootstrap.Interfaces;
using Jube.Analytics.Bootstrap.Models;

namespace Jube.Analytics.Bootstrap
{
    internal sealed class GaussianCopula(double rho) : IBivariateCopula
    {
        public string Family => "Gaussian";
        public double Parameter { get; } = rho;

        public double LogDensity(double u, double v)
        {
            var x = NormalDistribution.InverseCdf(u);
            var y = NormalDistribution.InverseCdf(v);
            var rhoSquared = Parameter * Parameter;

            return -0.5 * Math.Log(1d - rhoSquared) +
                   (2d * Parameter * x * y - rhoSquared * (x * x + y * y)) / (2d * (1d - rhoSquared));
        }

        public double ConditionalCdf(double u, double v)
        {
            var x = NormalDistribution.InverseCdf(u);
            var y = NormalDistribution.InverseCdf(v);

            return NormalDistribution.Cdf((y - Parameter * x) / Math.Sqrt(1d - Parameter * Parameter));
        }

        public double InverseConditionalCdf(double u, double w)
        {
            var x = NormalDistribution.InverseCdf(u);
            var z = NormalDistribution.InverseCdf(w);
            var y = Parameter * x + Math.Sqrt(1d - Parameter * Parameter) * z;

            return NormalDistribution.Cdf(y);
        }

        public static double TauToParameter(double tau)
        {
            return Math.Sin(Math.PI * tau / 2d);
        }

        public static FittedCopula Fit(double[] u, double[] v, double tau)
        {
            var start = Math.Clamp(TauToParameter(tau), -0.999, 0.999);

            double LogLikelihood(double rho)
            {
                var copula = new GaussianCopula(rho);
                var total = 0d;

                for (var i = 0; i < u.Length; i++)
                {
                    total += copula.LogDensity(u[i], v[i]);
                }

                return total;
            }

            var lower = Math.Max(-0.999, start - 0.3);
            var upper = Math.Min(0.999, start + 0.3);
            var fitted = GoldenSectionSearch.Maximise(LogLikelihood, lower, upper);
            var logLikelihood = LogLikelihood(fitted);

            return new FittedCopula(new GaussianCopula(fitted), logLikelihood, -2d * logLikelihood + 2d);
        }
    }
}