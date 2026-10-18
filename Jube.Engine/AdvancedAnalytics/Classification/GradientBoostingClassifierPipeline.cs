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

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    internal sealed class GradientBoostingClassifierPipeline(
        double baseScore,
        double learningRate,
        double[] fill,
        double[] means,
        double[] standardDeviations,
        GradientBoostingTree[] trees,
        Calibrator? calibrator)
    {
        private readonly double[] fill = (double[])fill.Clone();
        private readonly double[] means = (double[])means.Clone();
        private readonly double[] standardDeviations = (double[])standardDeviations.Clone();

        public int InputCount => fill.Length;
        public int TreeCount => trees.Length;
        public double BaseScore { get; } = baseScore;
        public double LearningRate { get; } = learningRate;
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
            var margin = BaseScore;

            foreach (var tree in trees)
            {
                margin += LearningRate * tree.Predict(prepared);
            }

            return margin;
        }

        public static double Sigmoid(double z)
        {
            if (z >= 0)
            {
                return 1d / (1d + Math.Exp(-z));
            }

            var e = Math.Exp(z);

            return e / (1d + e);
        }

        public double FinalProbability(double[] row)
        {
            var raw = RawScore(row);

            return Calibrator is { } calibrator ? calibrator.Apply(raw) : Sigmoid(raw);
        }

        public (double RawScore, double BaseValue, double[] Contributions) Explain(double[] row)
        {
            var prepared = Prepare(row);
            var contributions = new double[fill.Length];
            var baseValue = BaseScore;

            foreach (var tree in trees)
            {
                baseValue += tree.WalkPath(prepared, LearningRate, contributions);
            }

            var rawScore = baseValue;

            for (var j = 0; j < contributions.Length; j++)
            {
                rawScore += contributions[j];
            }

            return (rawScore, baseValue, contributions);
        }
    }
}