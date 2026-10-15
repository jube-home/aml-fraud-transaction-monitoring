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
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    internal sealed class LinearRegressionPipeline(
        bool standardise,
        bool rejectMissing,
        double[] fill,
        double[] means,
        double[] standardDeviations,
        int[] indicatorInputs,
        double[] weights,
        double intercept)
    {
        private readonly double[] fill = (double[])fill.Clone();
        private readonly int[] indicatorInputs = (int[])indicatorInputs.Clone();
        private readonly double[] means = (double[])means.Clone();
        private readonly double[] standardDeviations = (double[])standardDeviations.Clone();
        private readonly double[] weights = (double[])weights.Clone();

        public int InputCount => fill.Length;
        public bool Standardise { get; } = standardise;
        public bool RejectMissing { get; } = rejectMissing;
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public int[] IndicatorInputs => (int[])indicatorInputs.Clone();
        public double[] Weights => (double[])weights.Clone();
        public double Intercept { get; } = intercept;

        public LinearRegressionRawExplain Explain(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException(
                    $"A row has {row.Length} values but the model expects {count}.");
            }

            var contributions = new double[weights.Length];
            var sum = Intercept;

            for (var j = 0; j < count; j++)
            {
                var value = row[j];

                if (!double.IsFinite(value))
                {
                    if (RejectMissing)
                    {
                        throw new ArgumentException(
                            $"Input {j} is missing (NaN or infinite) and this model rejects missing values.");
                    }

                    value = fill[j];
                }

                if (Standardise)
                {
                    value = Standardisation.ZScore(value, means[j], standardDeviations[j]);
                }

                contributions[j] = weights[j] * value;
                sum += contributions[j];
            }

            for (var k = 0; k < indicatorInputs.Length; k++)
            {
                contributions[count + k] = weights[count + k] * (double.IsFinite(row[indicatorInputs[k]]) ? 0d : 1d);
                sum += contributions[count + k];
            }

            return new LinearRegressionRawExplain(sum, Intercept, contributions);
        }

        public double Predict(double[] row)
        {
            return Explain(row).PredictedValue;
        }

        public double[] Predict(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                result[i] = Predict(rows[i]);
            }

            return result;
        }
    }
}