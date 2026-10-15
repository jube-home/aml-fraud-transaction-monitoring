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

using System;
using System.Collections.Generic;

namespace Jube.Test.Analytics
{
    internal static class FoundationFixtures
    {
        public static readonly double[] D1Scores = [0.1, 0.2, 0.2, 0.4, 0.4, 0.4, 0.6, 0.7, 0.7, 0.9];
        public static readonly int[] D1Labels = [0, 0, 1, 0, 1, 0, 1, 1, 0, 1];

        public static double Sigmoid(double z)
        {
            return 1d / (1d + Math.Exp(-z));
        }

        public static (double[] Scores, int[] Labels) Miscalibrated()
        {
            var rng = new Lcg(12345);
            var scores = new List<double>();
            var labels = new List<int>();

            for (var i = 0; i < 400; i++)
            {
                var u1 = rng.Next();
                var u2 = rng.Next();
                var z = 6 * u1 - 3;
                labels.Add(u2 < Sigmoid(1.5 * z - 0.5) ? 1 : 0);
                scores.Add(Sigmoid(0.3 * z + 1.0));
            }

            return ([.. scores], [.. labels]);
        }

        public static (double[] Features, int[] Labels) Population()
        {
            var rng = new Lcg(777);
            var features = new List<double>();
            var labels = new List<int>();

            for (var i = 0; i < 60000; i++)
            {
                var y = rng.Next() < 0.02 ? 1 : 0;
                var z = rng.Next() + rng.Next() + rng.Next() + rng.Next() - 2 + (y == 1 ? 1.0 : 0.0);
                features.Add(z);
                labels.Add(y);
            }

            return ([.. features], [.. labels]);
        }
    }
}