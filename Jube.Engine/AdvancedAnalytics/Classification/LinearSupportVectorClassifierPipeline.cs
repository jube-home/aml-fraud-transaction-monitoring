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
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    internal sealed class LinearSupportVectorClassifierPipeline(
        bool standardise,
        double[] fill,
        double[] means,
        double[] standardDeviations,
        double[] weights,
        double bias,
        Calibrator? calibrator)
    {
        private readonly double[] fill = (double[])fill.Clone();
        private readonly double[] means = (double[])means.Clone();
        private readonly double[] standardDeviations = (double[])standardDeviations.Clone();
        private readonly double[] weights = (double[])weights.Clone();

        public int InputCount => fill.Length;
        public bool Standardise { get; } = standardise;
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public double[] Weights => (double[])weights.Clone();
        public double Bias { get; } = bias;
        public Calibrator? Calibrator { get; } = calibrator;

        public LinearSupportVectorRawExplain Explain(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var contributions = new double[count];
            var sum = Bias;

            for (var j = 0; j < count; j++)
            {
                var value = double.IsFinite(row[j]) ? row[j] : fill[j];

                if (Standardise)
                {
                    value = Standardisation.ZScore(value, means[j], standardDeviations[j]);
                }

                contributions[j] = weights[j] * value;
                sum += contributions[j];
            }

            return new LinearSupportVectorRawExplain(sum, Bias, contributions);
        }

        public double DecisionFunction(double[] row)
        {
            return Explain(row).RawDecisionFunction;
        }

        public double Probability(double[] row)
        {
            var raw = DecisionFunction(row);

            return Calibrator is { } calibrator ? calibrator.Apply(raw) : Sigmoid(raw);
        }

        public double[] Probabilities(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                result[i] = Probability(rows[i]);
            }

            return result;
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
    }
}