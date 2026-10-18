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
using System.Linq;
using Jube.Analytics;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    internal sealed class RandomForestClassifierPipeline(
        double[] fill,
        double[] means,
        double[] standardDeviations,
        RandomForestTree[] trees,
        Calibrator? calibrator)
    {
        private readonly double[] fill = (double[])fill.Clone();
        private readonly double[] means = (double[])means.Clone();
        private readonly double[] standardDeviations = (double[])standardDeviations.Clone();

        public int InputCount => fill.Length;
        public int TreeCount => trees.Length;
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public Calibrator? Calibrator { get; } = calibrator;

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

        public double RawScore(double[] row)
        {
            var prepared = Prepare(row);
            var sum = 0d;

            foreach (var tree in trees)
            {
                sum += tree.Predict(prepared);
            }

            return sum / trees.Length;
        }

        public double FinalProbability(double[] row)
        {
            var raw = RawScore(row);

            return Calibrator is { } calibrator ? calibrator.Apply(raw) : raw;
        }

        public (double RawScore, double Bias, double[] Contributions) Explain(double[] row)
        {
            var prepared = Prepare(row);
            var contributions = new double[fill.Length];
            var biases = new double[trees.Length];

            for (var t = 0; t < trees.Length; t++)
            {
                trees[t].WalkPath(prepared, biases, t, contributions);
            }

            var count = trees.Length;
            var bias = biases.Average();

            for (var j = 0; j < contributions.Length; j++)
            {
                contributions[j] /= count;
            }

            var rawScore = bias + contributions.Sum();

            return (rawScore, bias, contributions);
        }
    }
}