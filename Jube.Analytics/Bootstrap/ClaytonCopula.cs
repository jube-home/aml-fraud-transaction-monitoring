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
    internal sealed class ClaytonCopula(double theta) : IBivariateCopula
    {
        private const double MinimumTheta = 1e-4;
        public string Family => "Clayton";
        public double Parameter { get; } = Math.Max(theta, MinimumTheta);

        public double LogDensity(double u, double v)
        {
            var t = Parameter;
            var s = Math.Pow(u, -t) + Math.Pow(v, -t) - 1d;

            return Math.Log(1d + t) - (1d + t) * (Math.Log(u) + Math.Log(v)) - (1d / t + 2d) * Math.Log(s);
        }

        public double ConditionalCdf(double u, double v)
        {
            var t = Parameter;
            var s = Math.Pow(u, -t) + Math.Pow(v, -t) - 1d;

            return Math.Pow(u, -t - 1d) * Math.Pow(s, -1d / t - 1d);
        }

        public double InverseConditionalCdf(double u, double w)
        {
            var t = Parameter;
            var s = Math.Pow(u, -t) * (Math.Pow(w, -t / (1d + t)) - 1d) + 1d;

            return Math.Pow(Math.Max(s, 1e-12), -1d / t);
        }

        public static double TauToParameter(double tau)
        {
            var bounded = Math.Clamp(tau, 0.01, 0.99);

            return 2d * bounded / (1d - bounded);
        }

        public static FittedCopula? Fit(double[] u, double[] v, double tau)
        {
            if (tau <= 0.02)
            {
                return null;
            }

            var start = TauToParameter(tau);

            double LogLikelihood(double theta)
            {
                var copula = new ClaytonCopula(theta);
                var total = 0d;

                for (var i = 0; i < u.Length; i++)
                {
                    total += copula.LogDensity(u[i], v[i]);
                }

                return total;
            }

            var lower = Math.Max(MinimumTheta, start * 0.3);
            var upper = start * 3d + 1d;
            var fitted = GoldenSectionSearch.Maximise(LogLikelihood, lower, upper);
            var logLikelihood = LogLikelihood(fitted);

            return new FittedCopula(new ClaytonCopula(fitted), logLikelihood, -2d * logLikelihood + 2d);
        }
    }
}