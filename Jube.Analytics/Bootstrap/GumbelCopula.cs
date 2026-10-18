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
    internal sealed class GumbelCopula(double theta) : IBivariateCopula
    {
        private const double MinimumTheta = 1d + 1e-4;
        public string Family => "Gumbel";
        public double Parameter { get; } = Math.Max(theta, MinimumTheta);

        public double LogDensity(double u, double v)
        {
            var t = Parameter;
            var (a, b, m) = Components(u, v);

            return -m - Math.Log(u) - Math.Log(v) + (1d - 2d * t) * Math.Log(m) +
                   (t - 1d) * (Math.Log(a) + Math.Log(b)) + Math.Log(t - 1d + m);
        }

        public double ConditionalCdf(double u, double v)
        {
            var t = Parameter;
            var (a, _, m) = Components(u, v);

            return Math.Exp(-m) * Math.Pow(a, t - 1d) * Math.Pow(m, 1d - t) / u;
        }

        public double InverseConditionalCdf(double u, double w)
        {
            var lower = 1e-9;
            var upper = 1d - 1e-9;

            for (var i = 0; i < 100; i++)
            {
                var mid = (lower + upper) / 2d;
                var value = ConditionalCdf(u, mid);

                if (value < w)
                {
                    lower = mid;
                }
                else
                {
                    upper = mid;
                }
            }

            return (lower + upper) / 2d;
        }

        public static double TauToParameter(double tau)
        {
            var bounded = Math.Clamp(tau, 0.01, 0.99);

            return 1d / (1d - bounded);
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
                var copula = new GumbelCopula(theta);
                var total = 0d;

                for (var i = 0; i < u.Length; i++)
                {
                    total += copula.LogDensity(u[i], v[i]);
                }

                return total;
            }

            var lower = Math.Max(MinimumTheta, start * 0.5);
            var upper = start * 2d + 1d;
            var fitted = GoldenSectionSearch.Maximise(LogLikelihood, lower, upper);
            var logLikelihood = LogLikelihood(fitted);

            return new FittedCopula(new GumbelCopula(fitted), logLikelihood, -2d * logLikelihood + 2d);
        }

        private (double A, double B, double M) Components(double u, double v)
        {
            var t = Parameter;
            var a = -Math.Log(u);
            var b = -Math.Log(v);
            var m = Math.Pow(Math.Pow(a, t) + Math.Pow(b, t), 1d / t);

            return (a, b, m);
        }
    }
}