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

namespace Jube.Charting.Svg
{
    internal static class Spline
    {
        public static void Append(PathBuilder path, IReadOnlyList<double> xs, IReadOnlyList<double> ys,
            bool continueFrom)
        {
            var n = xs.Count;

            if (n == 0)
            {
                return;
            }

            if (continueFrom)
            {
                path.Line(xs[0], ys[0]);
            }
            else
            {
                path.Move(xs[0], ys[0]);
            }

            if (n == 1)
            {
                return;
            }

            var increasing = true;
            var decreasing = true;

            for (var i = 1; i < n; i++)
            {
                increasing &= xs[i] > xs[i - 1];
                decreasing &= xs[i] < xs[i - 1];
            }

            if (n == 2 || !(increasing || decreasing))
            {
                for (var i = 1; i < n; i++)
                {
                    path.Line(xs[i], ys[i]);
                }

                return;
            }

            var d = new double[n - 1];
            var h = new double[n - 1];

            for (var i = 0; i < n - 1; i++)
            {
                h[i] = xs[i + 1] - xs[i];
                d[i] = (ys[i + 1] - ys[i]) / h[i];
            }

            var m = new double[n];
            m[0] = d[0];
            m[n - 1] = d[n - 2];

            for (var i = 1; i < n - 1; i++)
            {
                m[i] = d[i - 1] * d[i] <= 0 ? 0 : (d[i - 1] + d[i]) / 2;
            }

            for (var i = 0; i < n - 1; i++)
            {
                if (d[i] == 0)
                {
                    m[i] = 0;
                    m[i + 1] = 0;
                    continue;
                }

                var a = m[i] / d[i];
                var b = m[i + 1] / d[i];
                var s = a * a + b * b;

                if (s > 9)
                {
                    var t = 3 / Math.Sqrt(s);
                    m[i] = t * a * d[i];
                    m[i + 1] = t * b * d[i];
                }
            }

            for (var i = 0; i < n - 1; i++)
            {
                path.Cubic(xs[i] + h[i] / 3, ys[i] + m[i] * h[i] / 3, xs[i + 1] - h[i] / 3,
                    ys[i + 1] - m[i + 1] * h[i] / 3,
                    xs[i + 1], ys[i + 1]);
            }
        }
    }
}