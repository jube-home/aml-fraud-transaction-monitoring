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

namespace Jube.Analytics.Bootstrap
{
    internal sealed class EmpiricalMargin
    {
        private readonly double[] sorted;

        private EmpiricalMargin(double[] sorted, double[] pseudoObservations)
        {
            this.sorted = sorted;
            PseudoObservations = pseudoObservations;
        }

        public double[] PseudoObservations { get; }

        public static EmpiricalMargin Fit(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (values.Length < 2)
            {
                throw new ArgumentException("A margin needs at least two observations.");
            }

            var ranks = Numerics.AverageRanks(values);
            var n = values.Length;
            var pseudoObservations = ranks.Select(r => r / (n + 1d)).ToArray();

            return new EmpiricalMargin(values.OrderBy(v => v).ToArray(), pseudoObservations);
        }

        public double Inverse(double u)
        {
            return Numerics.Quantile(sorted, Math.Clamp(u, 0d, 1d));
        }
    }
}