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

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    internal sealed class NeuralNetworkRegressorPipeline
    {
        private const double CompressedLow = 0.05;
        private const double CompressedSpan = 0.9;
        private readonly double[] fill;
        private readonly double[] hiddenBiases;
        private readonly double[][] inputWeights;
        private readonly double[] means;
        private readonly double[] outputWeights;
        private readonly double[] standardDeviations;

        public NeuralNetworkRegressorPipeline(bool standardise, double[] fill, double[] means,
            double[] standardDeviations, double[][] inputWeights, double[] hiddenBiases, double[] outputWeights,
            double outputBias, double targetLow, double targetSpan)
        {
            Standardise = standardise;
            this.fill = (double[])fill.Clone();
            this.means = (double[])means.Clone();
            this.standardDeviations = (double[])standardDeviations.Clone();
            this.inputWeights = [.. inputWeights.Select(row => (double[])row.Clone())];
            this.hiddenBiases = (double[])hiddenBiases.Clone();
            this.outputWeights = (double[])outputWeights.Clone();
            OutputBias = outputBias;
            TargetLow = targetLow;
            TargetSpan = targetSpan;
        }

        public int InputCount => fill.Length;
        public int HiddenUnits => hiddenBiases.Length;
        public bool Standardise { get; }
        public double OutputBias { get; }
        public double TargetLow { get; }
        public double TargetSpan { get; }
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public double[][] InputWeights => [.. inputWeights.Select(row => (double[])row.Clone())];
        public double[] HiddenBiases => (double[])hiddenBiases.Clone();
        public double[] OutputWeights => (double[])outputWeights.Clone();

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
                var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                prepared[j] = Standardise ? Standardisation.ZScore(value, means[j], standardDeviations[j]) : value;
            }

            return prepared;
        }

        public double[] HiddenActivations(double[] prepared)
        {
            var hidden = new double[HiddenUnits];

            for (var h = 0; h < HiddenUnits; h++)
            {
                var z = hiddenBiases[h];
                var weights = inputWeights[h];

                for (var j = 0; j < prepared.Length; j++)
                {
                    z += weights[j] * prepared[j];
                }

                hidden[h] = Sigmoid(z);
            }

            return hidden;
        }

        public double Logit(double[] row)
        {
            var hidden = HiddenActivations(Prepare(row));
            var logit = OutputBias;

            for (var h = 0; h < HiddenUnits; h++)
            {
                logit += outputWeights[h] * hidden[h];
            }

            return logit;
        }

        public double BoundedOutput(double[] row)
        {
            return Sigmoid(Logit(row));
        }

        public double PredictedValue(double[] row)
        {
            return Rescale(BoundedOutput(row));
        }

        public double Rescale(double boundedOutput)
        {
            return (boundedOutput - CompressedLow) / CompressedSpan * TargetSpan + TargetLow;
        }

        public double[] PredictedValues(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                result[i] = PredictedValue(rows[i]);
            }

            return result;
        }

        public double[] PredictedValueGradient(double[] row)
        {
            var prepared = Prepare(row);
            var hidden = HiddenActivations(prepared);
            var gradient = new double[InputCount];
            var logit = OutputBias;

            for (var h = 0; h < HiddenUnits; h++)
            {
                logit += outputWeights[h] * hidden[h];
            }

            var bounded = Sigmoid(logit);
            var outputScale = bounded * (1d - bounded) * (TargetSpan / CompressedSpan);

            for (var j = 0; j < InputCount; j++)
            {
                var sum = 0d;

                for (var h = 0; h < HiddenUnits; h++)
                {
                    sum += outputWeights[h] * hidden[h] * (1d - hidden[h]) * inputWeights[h][j];
                }

                var scale = Standardise
                    ? standardDeviations[j] > 0 ? 1d / standardDeviations[j] : 0d
                    : 1d;

                gradient[j] = outputScale * sum * scale;
            }

            return gradient;
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