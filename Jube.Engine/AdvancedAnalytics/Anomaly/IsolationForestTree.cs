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

namespace Jube.Engine.AdvancedAnalytics.Anomaly
{
    internal sealed class IsolationForestTree(
        int[] featureIndex,
        double[] splitValue,
        int[] left,
        int[] right,
        int[] size)
    {
        private readonly int[] featureIndex = (int[])featureIndex.Clone();
        private readonly int[] left = (int[])left.Clone();
        private readonly int[] right = (int[])right.Clone();
        private readonly int[] size = (int[])size.Clone();
        private readonly double[] splitValue = (double[])splitValue.Clone();

        public int NodeCount => featureIndex.Length;
        public int[] FeatureIndex => (int[])featureIndex.Clone();
        public double[] SplitValue => (double[])splitValue.Clone();
        public int[] Left => (int[])left.Clone();
        public int[] Right => (int[])right.Clone();
        public int[] Size => (int[])size.Clone();

        public double Path(double[] row, double[] contributions)
        {
            var node = 0;
            var depth = 0d;

            while (featureIndex[node] != -1)
            {
                var feature = featureIndex[node];

                if (contributions != null)
                {
                    contributions[feature] += 1d / (depth + 1d);
                }

                node = row[feature] < splitValue[node] ? left[node] : right[node];
                depth++;
            }

            return depth + IsolationForestAnomalyPipeline.AveragePath(size[node]);
        }
    }
}