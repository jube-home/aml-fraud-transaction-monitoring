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

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    internal sealed class DecisionTreeClassifierPipeline(
        double[] fill,
        bool[] isLeaf,
        int[] attributeIndex,
        double[] threshold,
        int[] leftChild,
        int[] rightChild,
        double[] nodeProbability)
    {
        private readonly int[] attributeIndex = (int[])attributeIndex.Clone();
        private readonly double[] fill = (double[])fill.Clone();
        private readonly bool[] isLeaf = (bool[])isLeaf.Clone();
        private readonly int[] leftChild = (int[])leftChild.Clone();
        private readonly double[] nodeProbability = (double[])nodeProbability.Clone();
        private readonly int[] rightChild = (int[])rightChild.Clone();
        private readonly double[] threshold = (double[])threshold.Clone();

        public int InputCount => fill.Length;
        public int NodeCount => isLeaf.Length;
        public double[] Fill => (double[])fill.Clone();
        public bool[] IsLeaf => (bool[])isLeaf.Clone();
        public int[] AttributeIndex => (int[])attributeIndex.Clone();
        public double[] Threshold => (double[])threshold.Clone();
        public int[] LeftChild => (int[])leftChild.Clone();
        public int[] RightChild => (int[])rightChild.Clone();
        public double[] NodeProbability => (double[])nodeProbability.Clone();

        public double[] Prepare(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var prepared = new double[count];

            for (var j = 0; j < count; j++)
            {
                prepared[j] = double.IsFinite(row[j]) ? row[j] : fill[j];
            }

            return prepared;
        }

        public IReadOnlyList<int> Path(double[] row)
        {
            var prepared = Prepare(row);
            var path = new List<int> { 0 };
            var node = 0;

            while (!isLeaf[node])
            {
                var value = prepared[attributeIndex[node]];
                node = value <= threshold[node] ? leftChild[node] : rightChild[node];
                path.Add(node);
            }

            return path;
        }

        public double LeafProbability(double[] row)
        {
            var path = Path(row);

            return nodeProbability[path[^1]];
        }
    }
}