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
    public readonly record struct MinMaxScaler(double Minimum, double Maximum, bool Clip = false)
    {
        public double Transform(double value)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            var range = Maximum - Minimum;
            var scaled = range == 0 ? 0d : (value - Minimum) / range;

            return Clip ? Math.Min(Math.Max(scaled, 0d), 1d) : scaled;
        }

        public double[] Transform(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            return values.Select(Transform).ToArray();
        }

        public static MinMaxScaler Fit(double[] values, bool clip = false)
        {
            var finite = Preprocessing.FiniteOrThrow(values);

            return new MinMaxScaler(finite.Min(), finite.Max(), clip);
        }
    }
}