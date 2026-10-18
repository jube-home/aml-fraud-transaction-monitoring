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
    internal static class GoldenSectionSearch
    {
        private const double InverseGoldenRatio = 0.6180339887498949;

        public static double Maximise(Func<double, double> objective, double lower, double upper,
            int iterations = 100, double tolerance = 1e-9)
        {
            var a = lower;
            var b = upper;
            var c = b - InverseGoldenRatio * (b - a);
            var d = a + InverseGoldenRatio * (b - a);
            var fc = objective(c);
            var fd = objective(d);

            for (var i = 0; i < iterations && Math.Abs(b - a) > tolerance; i++)
            {
                if (fc > fd)
                {
                    b = d;
                    d = c;
                    fd = fc;
                    c = b - InverseGoldenRatio * (b - a);
                    fc = objective(c);
                }
                else
                {
                    a = c;
                    c = d;
                    fc = fd;
                    d = a + InverseGoldenRatio * (b - a);
                    fd = objective(d);
                }
            }

            return (a + b) / 2d;
        }
    }
}