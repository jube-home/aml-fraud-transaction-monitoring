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
using System.Text.Json;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    internal static class ModelEvaluationData
    {
        public static readonly string[] Names = ["a", "b", "c"];

        public static (double[][] X, int[] Y) Interaction(int n = 500, int seed = 3)
        {
            var random = new Random(seed);
            var x = new double[n][];
            var y = new int[n];

            for (var i = 0; i < n; i++)
            {
                x[i] = [random.NextDouble() * 10, random.NextDouble() * 10, random.NextDouble() * 10];
                y[i] = x[i][0] > 5 && x[i][1] > 5 ? 1 : 0;
            }

            return (x, y);
        }

        public static (double[][] X, int[] Y) Separable(int n = 400, int seed = 5)
        {
            var random = new Random(seed);
            var x = new double[n][];
            var y = new int[n];

            for (var i = 0; i < n; i++)
            {
                x[i] = [random.NextDouble() * 10, random.NextDouble() * 10, random.NextDouble() * 10];
                y[i] = x[i][0] > 5 ? 1 : 0;
            }

            return (x, y);
        }

        public static (double[][] X, double[] T) Linear(int n = 400, int seed = 7)
        {
            var random = new Random(seed);
            var x = new double[n][];
            var t = new double[n];

            for (var i = 0; i < n; i++)
            {
                x[i] = [random.NextDouble() * 10, random.NextDouble() * 10, random.NextDouble() * 10];
                t[i] = 2 * x[i][0] + 1;
            }

            return (x, t);
        }

        public static (double[][] X, int[] Labels) Bands(int n = 450, int seed = 9)
        {
            var random = new Random(seed);
            var x = new double[n][];
            var y = new int[n];

            for (var i = 0; i < n; i++)
            {
                x[i] = [random.NextDouble() * 9, random.NextDouble() * 10, random.NextDouble() * 10];
                y[i] = x[i][0] < 3 ? 10 : x[i][0] < 6 ? 20 : 30;
            }

            return (x, y);
        }

        public static (double[][] X, int[] Labels) Anomalies(int n = 300, int seed = 11)
        {
            var random = new Random(seed);
            var x = new double[n][];
            var y = new int[n];

            for (var i = 0; i < n; i++)
            {
                var outlier = i < 15;
                y[i] = outlier ? 1 : 0;
                x[i] = [(outlier ? 8 : 0) + random.NextDouble(), (outlier ? 8 : 0) + random.NextDouble()];
            }

            return (x, y);
        }

        public static string Json(object value)
        {
            return JsonSerializer.Serialize(value);
        }
    }
}