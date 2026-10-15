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
    internal static class BivariateCopulaFitter
    {
        public static IBivariateCopula FitBest(double[] u, double[] v)
        {
            var tau = KendallTau.Compute(u, v);
            var candidates = new List<FittedCopula> { GaussianCopula.Fit(u, v, tau), FrankCopula.Fit(u, v, tau) };

            var clayton = ClaytonCopula.Fit(u, v, tau);

            if (clayton != null)
            {
                candidates.Add(clayton);
            }

            var gumbel = GumbelCopula.Fit(u, v, tau);

            if (gumbel != null)
            {
                candidates.Add(gumbel);
            }

            return candidates.OrderBy(f => f.Aic).First().Copula;
        }
    }
}