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

namespace Jube.Analytics
{
    public readonly record struct RobustScaler(double Median, double Scale)
    {
        public const double NormalConsistency = 1.482602218505602;

        public double Transform(double value)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            return Scale == 0 || !Numerics.IsFinite(Scale) ? 0d : (value - Median) / Scale;
        }

        public double[] Transform(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            return values.Select(Transform).ToArray();
        }

        public static RobustScaler Fit(double[] values)
        {
            var sorted = Preprocessing.FiniteOrThrow(values).OrderBy(v => v).ToArray();
            var median = Numerics.Median(sorted);
            var deviations = sorted.Select(v => Math.Abs(v - median)).OrderBy(v => v).ToArray();

            return new RobustScaler(median, NormalConsistency * Numerics.Median(deviations));
        }
    }
}