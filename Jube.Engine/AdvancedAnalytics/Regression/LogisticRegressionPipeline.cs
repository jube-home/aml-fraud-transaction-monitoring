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
    internal sealed class LogisticRegressionPipeline(
        bool standardise,
        bool rejectMissing,
        double[] fill,
        double[] means,
        double[] standardDeviations,
        int[] indicatorInputs,
        double[] weights,
        double intercept,
        Calibrator? calibrator,
        double trainingPrior,
        double baseRate,
        double?[] significances = null)
    {
        private readonly double[] fill = (double[])fill.Clone();
        private readonly int[] indicatorInputs = (int[])indicatorInputs.Clone();
        private readonly double[] means = (double[])means.Clone();
        private readonly double priorOffset = PriorShift.Offset(trainingPrior, baseRate);
        private readonly double?[] significances = significances == null ? null : (double?[])significances.Clone();
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
        public double?[] Significances => significances == null ? null : (double?[])significances.Clone();
        public double Intercept { get; } = intercept;
        public Calibrator? Calibrator { get; } = calibrator;
        public double TrainingPrior { get; } = trainingPrior;
        public double BaseRate { get; } = baseRate;

        public double RawLogit(double[] row)
        {
            return Explain(row).RawLogit;
        }

        public LogisticRegressionRawExplain Explain(double[] row)
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

            return new LogisticRegressionRawExplain(sum, Intercept, contributions);
        }

        public double FinalLogit(double[] row)
        {
            var logit = RawLogit(row);

            if (Calibrator is { } calibrator)
            {
                logit = calibrator.Slope * logit + calibrator.Intercept;
            }

            return logit + priorOffset;
        }

        public double Probability(double[] row)
        {
            return Sigmoid(FinalLogit(row));
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