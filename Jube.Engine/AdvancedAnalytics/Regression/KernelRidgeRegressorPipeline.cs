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
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    internal sealed class KernelRidgeRegressorPipeline
    {
        private readonly double[] alpha;
        private readonly double[] fill;
        private readonly double laplacianSigma;
        private readonly double[] means;
        private readonly double[] standardDeviations;
        private readonly double[][] supportRows;

        public KernelRidgeRegressorPipeline(double[] fill, double[] means, double[] standardDeviations,
            double[][] supportRows, double[] alpha, double mean, KernelRidgeRegressorKernel kernel, double gamma,
            int degree)
        {
            if (supportRows.Length != alpha.Length)
            {
                throw new ArgumentException("There must be exactly one coefficient per support row.");
            }

            if (supportRows.Length == 0)
            {
                throw new ArgumentException("A kernel ridge regressor needs at least one support row.");
            }

            if (!double.IsFinite(gamma) || gamma < 0)
            {
                throw new ArgumentException("Gamma must be finite and not negative.");
            }

            this.fill = (double[])fill.Clone();
            this.means = (double[])means.Clone();
            this.standardDeviations = (double[])standardDeviations.Clone();
            this.supportRows = [.. supportRows.Select(r => (double[])r.Clone())];
            this.alpha = (double[])alpha.Clone();
            Mean = mean;
            Kernel = kernel;
            Gamma = gamma;
            laplacianSigma = gamma > 0 ? Math.Sqrt(1d / (2d * gamma)) : 0d;
            Degree = degree;
        }

        public int InputCount => fill.Length;
        public int SupportRowCount => supportRows.Length;
        public KernelRidgeRegressorKernel Kernel { get; }
        public double Gamma { get; }
        public int Degree { get; }
        public double Mean { get; }
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public double[][] SupportRows => [.. supportRows.Select(r => (double[])r.Clone())];
        public double[] Alpha => (double[])alpha.Clone();

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

        public double Predict(double[] row)
        {
            var prepared = Prepare(row);
            var sum = Mean;

            for (var i = 0; i < supportRows.Length; i++)
            {
                sum += alpha[i] * KernelValue(prepared, supportRows[i]);
            }

            return sum;
        }

        private double KernelValue(double[] x, double[] support)
        {
            switch (Kernel)
            {
                case KernelRidgeRegressorKernel.Polynomial:
                {
                    var dot = 0d;

                    for (var j = 0; j < x.Length; j++)
                    {
                        dot += x[j] * support[j];
                    }

                    return Math.Pow(dot + 1d, Degree);
                }
                case KernelRidgeRegressorKernel.Sigmoid:
                {
                    var dot = 0d;

                    for (var j = 0; j < x.Length; j++)
                    {
                        dot += x[j] * support[j];
                    }

                    return Math.Tanh(Gamma * dot);
                }
                case KernelRidgeRegressorKernel.Laplacian:
                {
                    var sumSquared = 0d;

                    for (var j = 0; j < x.Length; j++)
                    {
                        var d = x[j] - support[j];
                        sumSquared += d * d;
                    }

                    var distance = Math.Sqrt(sumSquared);

                    return laplacianSigma > 0 ? Math.Exp(-distance / laplacianSigma) : distance == 0 ? 1d : 0d;
                }
                case KernelRidgeRegressorKernel.Linear:
                {
                    var dot = 0d;

                    for (var j = 0; j < x.Length; j++)
                    {
                        dot += x[j] * support[j];
                    }

                    return dot;
                }
                default:
                {
                    var sumSquared = 0d;

                    for (var j = 0; j < x.Length; j++)
                    {
                        var d = x[j] - support[j];
                        sumSquared += d * d;
                    }

                    return Math.Exp(-Gamma * sumSquared);
                }
            }
        }
    }
}