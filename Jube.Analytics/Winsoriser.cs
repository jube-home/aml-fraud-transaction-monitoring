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
    public readonly record struct Winsoriser(double Lower, double Upper)
    {
        public double Transform(double value)
        {
            return double.IsNaN(value) ? double.NaN : Math.Min(Math.Max(value, Lower), Upper);
        }

        public double[] Transform(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            return values.Select(Transform).ToArray();
        }

        public static Winsoriser Fit(double[] values, double lowerQuantile = 0.01, double upperQuantile = 0.99)
        {
            if (double.IsNaN(lowerQuantile) || double.IsNaN(upperQuantile) || lowerQuantile < 0 ||
                upperQuantile > 1 || lowerQuantile > upperQuantile)
            {
                throw new ArgumentOutOfRangeException(nameof(lowerQuantile),
                    "Quantiles must satisfy 0 <= lower <= upper <= 1.");
            }

            var sorted = Preprocessing.FiniteOrThrow(values).OrderBy(v => v).ToArray();

            return new Winsoriser(Numerics.Quantile(sorted, lowerQuantile), Numerics.Quantile(sorted, upperQuantile));
        }

        public static Winsoriser Bounds(double lower, double upper)
        {
            if (double.IsNaN(lower) || double.IsNaN(upper) || lower > upper)
            {
                throw new ArgumentException("Lower must not exceed upper.");
            }

            return new Winsoriser(lower, upper);
        }
    }
}