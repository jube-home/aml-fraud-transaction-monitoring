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
    internal sealed class FrankCopula(double theta) : IBivariateCopula
    {
        private const double MinimumAbsoluteTheta = 1e-3;
        public string Family => "Frank";

        public double Parameter { get; } = Math.Abs(theta) < MinimumAbsoluteTheta
            ? Math.CopySign(MinimumAbsoluteTheta, theta == 0d ? 1d : theta)
            : theta;

        public double LogDensity(double u, double v)
        {
            var t = Parameter;
            var numerator = t * (1d - Math.Exp(-t)) * Math.Exp(-t * (u + v));
            var bracket = 1d - Math.Exp(-t) - (1d - Math.Exp(-t * u)) * (1d - Math.Exp(-t * v));

            return Math.Log(numerator) - 2d * Math.Log(Math.Abs(bracket));
        }

        public double ConditionalCdf(double u, double v)
        {
            var t = Parameter;
            var p = Math.Exp(-t * u) - 1d;
            var q = Math.Exp(-t * v) - 1d;
            var d = Math.Exp(-t) - 1d;

            return Math.Exp(-t * u) * q / (d + p * q);
        }

        public double InverseConditionalCdf(double u, double w)
        {
            var t = Parameter;
            var p = Math.Exp(-t * u) - 1d;
            var d = Math.Exp(-t) - 1d;
            var expNegTu = Math.Exp(-t * u);

            var q = w * d / (expNegTu - w * p);

            return -1d / t * Math.Log(1d + q);
        }

        public static FittedCopula Fit(double[] u, double[] v, double tau)
        {
            var start = Math.Clamp(tau * 20d, -30d, 30d);

            if (Math.Abs(start) < 0.5)
            {
                start = tau >= 0 ? 0.5 : -0.5;
            }

            double LogLikelihood(double theta)
            {
                var copula = new FrankCopula(theta);
                var total = 0d;

                for (var i = 0; i < u.Length; i++)
                {
                    total += copula.LogDensity(u[i], v[i]);
                }

                return total;
            }

            var lower = start - 15d;
            var upper = start + 15d;
            var fitted = GoldenSectionSearch.Maximise(LogLikelihood, lower, upper);
            var logLikelihood = LogLikelihood(fitted);

            return new FittedCopula(new FrankCopula(fitted), logLikelihood, -2d * logLikelihood + 2d);
        }
    }
}