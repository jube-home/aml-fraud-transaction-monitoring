// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Jube Holdings Limited, 2026
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Math;

namespace Accord.MachineLearning.Bayes
{
    [Serializable]
    public class TreeAugmentedNaiveBayesLearning :
        ISupervisedLearning<TreeAugmentedNaiveBayes, double[], int>,
        IParallel
    {
        private const double MaximumCorrelationSquared = 1 - 1e-12;
        private const int MinimumObservations = 3;

        public TreeAugmentedNaiveBayesLearning()
        {
            ParallelOptions = new ParallelOptions();
            Smoothing = 1;
            VarianceFloor = 1e-4;
            Ridge = 1e-6;
            MinimumInformation = 0;
            Root = TreeRootSelection.FirstInput;
        }

        public TreeAugmentedNaiveBayes Model { get; set; }

        public double Smoothing { get; set; }

        public double VarianceFloor { get; set; }

        public double Ridge { get; set; }

        public double MinimumInformation { get; set; }

        public TreeRootSelection Root { get; set; }

        public ParallelOptions ParallelOptions { get; set; }

        public CancellationToken Token
        {
            get => ParallelOptions.CancellationToken;
            set => ParallelOptions.CancellationToken = value;
        }

        public TreeAugmentedNaiveBayes Learn(double[][] x, int[] y, double[] weights = null)
        {
            Tools.CheckArgs(x, y, weights, () =>
            {
                if (Model == null)
                {
                    Model = new TreeAugmentedNaiveBayes(y.Max() + 1, x[0].Length);
                }

                return Model;
            }, allowNaN: true);

            if (Smoothing < 0 || double.IsNaN(Smoothing) || double.IsInfinity(Smoothing))
            {
                throw new InvalidOperationException("The Smoothing property should be a finite non-negative number.");
            }

            if (VarianceFloor < 0 || double.IsNaN(VarianceFloor) || double.IsInfinity(VarianceFloor))
            {
                throw new InvalidOperationException(
                    "The VarianceFloor property should be a finite non-negative number.");
            }

            if (Ridge < 0 || double.IsNaN(Ridge) || double.IsInfinity(Ridge))
            {
                throw new InvalidOperationException("The Ridge property should be a finite non-negative number.");
            }

            var classes = Model.NumberOfOutputs;
            var inputs = Model.NumberOfInputs;

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i].Length != inputs)
                {
                    throw new DimensionMismatchException("x",
                        "The input vector at index {0} has {1} elements, but {2} were expected.".Format(i, x[i].Length,
                            inputs));
                }

                if (y[i] >= classes)
                {
                    throw new ArgumentOutOfRangeException("y",
                        "The class label at index {0} is {1}, but the model has only {2} classes.".Format(i, y[i],
                            classes));
                }

                for (var j = 0; j < inputs; j++)
                {
                    if (double.IsInfinity(x[i][j]))
                    {
                        throw new ArgumentException(
                            "The input vector at index {0} contains an infinity value at position {1}, which is not supported by this algorithm."
                                .Format(i, j), "x");
                    }
                }
            }

            var w = new double[x.Length];
            double total = 0;
            for (var i = 0; i < w.Length; i++)
            {
                w[i] = weights == null ? 1.0 : weights[i];
                if (w[i] < 0 || double.IsNaN(w[i]) || double.IsInfinity(w[i]))
                {
                    throw new ArgumentOutOfRangeException("weights",
                        "The weight at index {0} should be a finite non-negative number.".Format(i));
                }

                total += w[i];
            }

            if (!(total > 0))
            {
                throw new ArgumentException("The sum of the weights should be positive.", "weights");
            }

            Token.ThrowIfCancellationRequested();

            var classWeights = new double[classes];
            for (var i = 0; i < x.Length; i++)
            {
                classWeights[y[i]] += w[i];
            }

            var priors = new double[classes];
            for (var c = 0; c < classes; c++)
            {
                priors[c] = (classWeights[c] + Smoothing) / (total + classes * Smoothing);
            }

            var information = InformationMatrix(x, y, w, classes, inputs);

            Token.ThrowIfCancellationRequested();

            var parents = Tree(information, inputs);

            Token.ThrowIfCancellationRequested();

            var intercepts = new double[classes][];
            var slopes = new double[classes][];
            var variances = new double[classes][];
            for (var c = 0; c < classes; c++)
            {
                intercepts[c] = new double[inputs];
                slopes[c] = new double[inputs];
                variances[c] = new double[inputs];
            }

            Estimate(x, y, w, classes, inputs, parents, intercepts, slopes, variances);

            Token.ThrowIfCancellationRequested();

            Model.Priors = priors;
            Model.Parents = parents;
            Model.Intercepts = intercepts;
            Model.Slopes = slopes;
            Model.Variances = variances;
            Model.MutualInformation = information;

            return Model;
        }

        private double[][] InformationMatrix(double[][] x, int[] y, double[] w, int classes, int inputs)
        {
            var information = new double[inputs][];
            for (var i = 0; i < inputs; i++)
            {
                information[i] = new double[inputs];
            }

            Action<int> row = i =>
            {
                Token.ThrowIfCancellationRequested();

                var m = new Moments(classes);
                for (var j = i + 1; j < inputs; j++)
                {
                    m.Compute(x, y, w, i, j);
                    var value = ConditionalInformation(m);
                    information[i][j] = value;
                    information[j][i] = value;
                }
            };

            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                for (var i = 0; i < inputs; i++)
                {
                    row(i);
                }
            }
            else
            {
                Parallel.For(0, inputs, ParallelOptions, row);
            }

            return information;
        }

        private static double ConditionalInformation(Moments m)
        {
            double total = 0;
            for (var c = 0; c < m.W.Length; c++)
            {
                total += m.W[c];
            }

            if (!(total > 0))
            {
                return 0;
            }

            double information = 0;
            for (var c = 0; c < m.W.Length; c++)
            {
                if (m.N[c] < MinimumObservations || !(m.W[c] > 0) || !(m.Sii[c] > 0) || !(m.Sjj[c] > 0))
                {
                    continue;
                }

                var rho2 = m.Sij[c] * m.Sij[c] / (m.Sii[c] * m.Sjj[c]);
                if (!(rho2 > 0))
                {
                    continue;
                }

                if (rho2 > MaximumCorrelationSquared)
                {
                    rho2 = MaximumCorrelationSquared;
                }

                information += -0.5 * (m.W[c] / total) * System.Math.Log(1 - rho2);
            }

            return System.Math.Max(0, information);
        }

        private int[] Tree(double[][] information, int inputs)
        {
            var parents = new int[inputs];
            for (var i = 0; i < inputs; i++)
            {
                parents[i] = -1;
            }

            if (inputs == 1)
            {
                return parents;
            }

            var root = 0;
            if (Root == TreeRootSelection.HighestInformation)
            {
                var best = double.NegativeInfinity;
                for (var i = 0; i < inputs; i++)
                {
                    double sum = 0;
                    for (var j = 0; j < inputs; j++)
                    {
                        sum += information[i][j];
                    }

                    if (sum > best)
                    {
                        best = sum;
                        root = i;
                    }
                }
            }

            var inTree = new bool[inputs];
            var weight = new double[inputs];
            var from = new int[inputs];

            inTree[root] = true;
            for (var j = 0; j < inputs; j++)
            {
                weight[j] = information[root][j];
                from[j] = root;
            }

            for (var step = 1; step < inputs; step++)
            {
                Token.ThrowIfCancellationRequested();

                var next = -1;
                for (var j = 0; j < inputs; j++)
                {
                    if (!inTree[j] && (next < 0 || weight[j] > weight[next]))
                    {
                        next = j;
                    }
                }

                inTree[next] = true;
                if (!(weight[next] < MinimumInformation))
                {
                    parents[next] = from[next];
                }

                for (var j = 0; j < inputs; j++)
                {
                    if (!inTree[j] && information[next][j] > weight[j])
                    {
                        weight[j] = information[next][j];
                        from[j] = next;
                    }
                }
            }

            return parents;
        }

        private void Estimate(double[][] x, int[] y, double[] w, int classes, int inputs, int[] parents,
            double[][] intercepts, double[][] slopes, double[][] variances)
        {
            Action<int> feature = i =>
            {
                Token.ThrowIfCancellationRequested();

                var marginal = new Moments(classes);
                marginal.Compute(x, y, w, i, i);

                double totalWeight = 0;
                double totalMean = 0;
                for (var c = 0; c < classes; c++)
                {
                    totalWeight += marginal.W[c];
                    totalMean += marginal.W[c] * marginal.Mi[c];
                }

                var globalMean = totalWeight > 0 ? totalMean / totalWeight : 0;

                double globalScatter = 0;
                for (var c = 0; c < classes; c++)
                {
                    if (marginal.W[c] > 0)
                    {
                        var d = marginal.Mi[c] - globalMean;
                        globalScatter += marginal.Sii[c] + marginal.W[c] * d * d;
                    }
                }

                var globalVariance = totalWeight > 0 ? globalScatter / totalWeight : 0;
                if (!(globalVariance > 0))
                {
                    globalVariance = 1;
                }

                var floor = VarianceFloor * globalVariance;
                if (!(floor > 0))
                {
                    floor = 1e-12;
                }

                var conditional = marginal;
                if (parents[i] >= 0)
                {
                    conditional = new Moments(classes);
                    conditional.Compute(x, y, w, i, parents[i]);
                }

                for (var c = 0; c < classes; c++)
                {
                    if (!(conditional.W[c] > 0) || conditional.N[c] < 1)
                    {
                        intercepts[c][i] = globalMean;
                        slopes[c][i] = 0;
                        variances[c][i] = System.Math.Max(globalVariance, floor);
                        continue;
                    }

                    double b = 0;
                    if (parents[i] >= 0 && conditional.N[c] >= MinimumObservations && conditional.Sjj[c] > 0)
                    {
                        b = conditional.Sij[c] / (conditional.Sjj[c] * (1 + Ridge));
                    }

                    var a = conditional.Mi[c] - b * conditional.Mj[c];
                    var residual = conditional.Sii[c] - 2 * b * conditional.Sij[c] + b * b * conditional.Sjj[c];
                    var variance = System.Math.Max(0, residual) / conditional.W[c];

                    intercepts[c][i] = a;
                    slopes[c][i] = b;
                    variances[c][i] = System.Math.Max(variance, floor);
                }
            };

            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                for (var i = 0; i < inputs; i++)
                {
                    feature(i);
                }
            }
            else
            {
                Parallel.For(0, inputs, ParallelOptions, feature);
            }
        }

        private sealed class Moments
        {
            public readonly double[] Mi;
            public readonly double[] Mj;
            public readonly int[] N;
            public readonly double[] Sii;
            public readonly double[] Sij;
            public readonly double[] Sjj;
            public readonly double[] W;

            public Moments(int classes)
            {
                W = new double[classes];
                Mi = new double[classes];
                Mj = new double[classes];
                Sii = new double[classes];
                Sjj = new double[classes];
                Sij = new double[classes];
                N = new int[classes];
            }

            public void Compute(double[][] x, int[] y, double[] w, int i, int j)
            {
                Array.Clear(W, 0, W.Length);
                Array.Clear(Mi, 0, Mi.Length);
                Array.Clear(Mj, 0, Mj.Length);
                Array.Clear(Sii, 0, Sii.Length);
                Array.Clear(Sjj, 0, Sjj.Length);
                Array.Clear(Sij, 0, Sij.Length);
                Array.Clear(N, 0, N.Length);

                for (var r = 0; r < x.Length; r++)
                {
                    var xi = x[r][i];
                    var xj = x[r][j];
                    if (w[r] <= 0 || double.IsNaN(xi) || double.IsNaN(xj))
                    {
                        continue;
                    }

                    var c = y[r];
                    W[c] += w[r];
                    Mi[c] += w[r] * xi;
                    Mj[c] += w[r] * xj;
                    N[c]++;
                }

                for (var c = 0; c < W.Length; c++)
                {
                    if (W[c] > 0)
                    {
                        Mi[c] /= W[c];
                        Mj[c] /= W[c];
                    }
                }

                for (var r = 0; r < x.Length; r++)
                {
                    var xi = x[r][i];
                    var xj = x[r][j];
                    if (w[r] <= 0 || double.IsNaN(xi) || double.IsNaN(xj))
                    {
                        continue;
                    }

                    var c = y[r];
                    var di = xi - Mi[c];
                    var dj = xj - Mj[c];
                    Sii[c] += w[r] * di * di;
                    Sjj[c] += w[r] * dj * dj;
                    Sij[c] += w[r] * di * dj;
                }
            }
        }
    }
}