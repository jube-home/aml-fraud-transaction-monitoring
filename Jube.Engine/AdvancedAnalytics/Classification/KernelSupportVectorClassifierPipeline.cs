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
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    internal sealed class KernelSupportVectorClassifierPipeline
    {
        private readonly double[] coefficients;
        private readonly double[] fill;
        private readonly double laplacianSigma;
        private readonly double[] means;
        private readonly double[] standardDeviations;
        private readonly double[][] supportVectors;

        public KernelSupportVectorClassifierPipeline(double[] fill, double[] means, double[] standardDeviations,
            double[][] supportVectors, double[] coefficients, double bias, KernelSupportVectorKernel kernel,
            double gamma, int degree, Calibrator? calibrator)
        {
            if (supportVectors.Length != coefficients.Length)
            {
                throw new ArgumentException("There must be exactly one coefficient per support vector.");
            }

            if (supportVectors.Length == 0)
            {
                throw new ArgumentException("A kernel support vector machine needs at least one support vector.");
            }

            if (!double.IsFinite(gamma) || gamma < 0)
            {
                throw new ArgumentException("Gamma must be finite and not negative.");
            }

            this.fill = (double[])fill.Clone();
            this.means = (double[])means.Clone();
            this.standardDeviations = (double[])standardDeviations.Clone();
            this.supportVectors = [.. supportVectors.Select(r => (double[])r.Clone())];
            this.coefficients = (double[])coefficients.Clone();
            Bias = bias;
            Kernel = kernel;
            Gamma = gamma;
            laplacianSigma = gamma > 0 ? Math.Sqrt(1d / (2d * gamma)) : 0d;
            Degree = degree;
            Calibrator = calibrator;
        }

        public int InputCount => fill.Length;
        public int SupportVectorCount => supportVectors.Length;
        public KernelSupportVectorKernel Kernel { get; }
        public double Gamma { get; }
        public int Degree { get; }
        public double Bias { get; }
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public double[][] SupportVectors => [.. supportVectors.Select(r => (double[])r.Clone())];
        public double[] Coefficients => (double[])coefficients.Clone();
        public Calibrator? Calibrator { get; }

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
                prepared[j] = Standardisation.ZScore(value, means[j], standardDeviations[j]);
            }

            return prepared;
        }

        public double DecisionFunction(double[] row)
        {
            var prepared = Prepare(row);
            var sum = Bias;

            for (var i = 0; i < supportVectors.Length; i++)
            {
                sum += coefficients[i] * KernelValue(prepared, supportVectors[i]);
            }

            return sum;
        }

        public double Probability(double[] row)
        {
            var score = DecisionFunction(row);

            return Calibrator is { } calibrator ? calibrator.Apply(score) : score;
        }

        private double KernelValue(double[] x, double[] sv)
        {
            switch (Kernel)
            {
                case KernelSupportVectorKernel.Polynomial:
                {
                    var dot = 0d;

                    for (var j = 0; j < x.Length; j++)
                    {
                        dot += x[j] * sv[j];
                    }

                    return Math.Pow(dot + 1d, Degree);
                }
                case KernelSupportVectorKernel.Sigmoid:
                {
                    var dot = 0d;

                    for (var j = 0; j < x.Length; j++)
                    {
                        dot += x[j] * sv[j];
                    }

                    return Math.Tanh(Gamma * dot);
                }
                case KernelSupportVectorKernel.Laplacian:
                {
                    var sumSquared = 0d;

                    for (var j = 0; j < x.Length; j++)
                    {
                        var d = x[j] - sv[j];
                        sumSquared += d * d;
                    }

                    var distance = Math.Sqrt(sumSquared);

                    return laplacianSigma > 0 ? Math.Exp(-distance / laplacianSigma) : distance == 0 ? 1d : 0d;
                }
                default:
                {
                    var sumSquared = 0d;

                    for (var j = 0; j < x.Length; j++)
                    {
                        var d = x[j] - sv[j];
                        sumSquared += d * d;
                    }

                    return Math.Exp(-Gamma * sumSquared);
                }
            }
        }
    }
}