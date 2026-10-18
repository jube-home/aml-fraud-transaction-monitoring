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
using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;

namespace Jube.Engine.AdvancedAnalytics.Anomaly
{
    internal sealed class IsolationForestAnomalyPipeline(
        double[] fill,
        int subsampleSize,
        IsolationForestTree[] trees,
        Calibrator? calibrator)
    {
        private readonly double[] fill = (double[])fill.Clone();
        private readonly double normaliser = Math.Max(AveragePath(subsampleSize), 1e-9);

        internal static double AveragePath(int n)
        {
            if (n <= 1)
            {
                return 0d;
            }

            if (n == 2)
            {
                return 1d;
            }

            return 2d * (Math.Log(n - 1) + 0.5772156649015329) - 2d * (n - 1) / n;
        }

        private double[] Prepare(double[] row)
        {
            var count = fill.Length;
            var values = new double[count];

            for (var j = 0; j < count; j++)
            {
                values[j] = double.IsFinite(row[j]) ? row[j] : fill[j];
            }

            return values;
        }

        public int InputCount => fill.Length;
        public int SubsampleSize { get; } = subsampleSize;
        public int TreeCount => trees.Length;
        public double[] Fill => (double[])fill.Clone();
        public Calibrator? Calibrator { get; } = calibrator;

        public IsolationForestAnomalyRawExplain Explain(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var prepared = Prepare(row);
            var contributions = new double[count];
            var totalPath = 0d;

            foreach (var tree in trees)
            {
                totalPath += tree.Path(prepared, contributions);
            }

            var averagePath = totalPath / trees.Length;
            var score = Math.Pow(2d, -averagePath / normaliser);

            for (var j = 0; j < count; j++)
            {
                contributions[j] /= trees.Length;
            }

            return new IsolationForestAnomalyRawExplain(score, contributions);
        }

        public double Score(double[] row)
        {
            return Explain(row).Score;
        }
    }
}