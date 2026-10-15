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

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    internal sealed class RandomForestRegressorTree(
        int[] featureIndex,
        double[] threshold,
        int[] left,
        int[] right,
        double[] value)
    {
        private readonly int[] featureIndex = (int[])featureIndex.Clone();
        private readonly int[] left = (int[])left.Clone();
        private readonly int[] right = (int[])right.Clone();
        private readonly double[] threshold = (double[])threshold.Clone();
        private readonly double[] value = (double[])value.Clone();

        public int NodeCount => value.Length;
        public int[] FeatureIndex => (int[])featureIndex.Clone();
        public double[] Threshold => (double[])threshold.Clone();
        public int[] Left => (int[])left.Clone();
        public int[] Right => (int[])right.Clone();
        public double[] Value => (double[])value.Clone();

        public int LeafIndex(double[] row)
        {
            var node = 0;

            while (featureIndex[node] != -1)
            {
                node = row[featureIndex[node]] <= threshold[node] ? left[node] : right[node];
            }

            return node;
        }

        public double Predict(double[] row)
        {
            return value[LeafIndex(row)];
        }

        public void WalkPath(double[] row, double[] biasAccumulator, int biasIndex, double[] contributions)
        {
            var node = 0;
            biasAccumulator[biasIndex] = value[0];

            while (featureIndex[node] != -1)
            {
                var feature = featureIndex[node];
                var next = row[feature] <= threshold[node] ? left[node] : right[node];
                contributions[feature] += value[next] - value[node];
                node = next;
            }
        }
    }
}