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
    internal static class KendallTau
    {
        public static double Compute(double[] x, double[] y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            if (x.Length != y.Length)
            {
                throw new ArgumentException("x and y must have the same length.");
            }

            var n = x.Length;

            if (n < 2)
            {
                return 0d;
            }

            long concordant = 0;
            long discordant = 0;
            long tiesX = 0;
            long tiesY = 0;

            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var dx = x[i] - x[j];
                    var dy = y[i] - y[j];
                    var sign = Math.Sign(dx) * Math.Sign(dy);

                    if (dx == 0d && dy == 0d)
                    {
                        continue;
                    }

                    if (dx == 0d)
                    {
                        tiesX++;
                    }
                    else if (dy == 0d)
                    {
                        tiesY++;
                    }
                    else if (sign > 0)
                    {
                        concordant++;
                    }
                    else
                    {
                        discordant++;
                    }
                }
            }

            var denominator = Math.Sqrt((concordant + discordant + tiesX) * (double)(concordant + discordant + tiesY));

            return denominator == 0d ? 0d : (concordant - discordant) / denominator;
        }
    }
}