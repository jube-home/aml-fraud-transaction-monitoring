// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Jube, 2026
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
using System.Collections.Generic;

namespace Accord.MachineLearning.LatentTrees
{
    [Serializable]
    public class LatentTreeLearning : ParallelLearningBase,
        ISupervisedLearning<LatentTreeClassifier, double[], int>
    {
        public LatentTreeLearning()
        {
            Options = new LatentTreeOptions();
            MaxIterations = 100;
            Tolerance = 1e-4;
            History = new double[0];
        }

        public LatentTreeOptions Options { get; set; }

        public int MaxIterations { get; set; }

        public double Tolerance { get; set; }

        public int Iterations { get; private set; }

        public double LogLikelihood { get; private set; }

        public double[] History { get; private set; }

        public LatentTreeClassifier Learn(double[][] x, int[] y, double[] weights = null)
        {
            var options = Options;
            if (options == null)
            {
                throw new InvalidOperationException("The options must not be null.");
            }

            Validate(x, y, weights, options);
            Token.ThrowIfCancellationRequested();

            var n = x.Length;
            var p = x[0].Length;
            var classCount = 0;
            for (var i = 0; i < n; i++)
            {
                if (y[i] + 1 > classCount)
                {
                    classCount = y[i] + 1;
                }
            }

            if (classCount < 2)
            {
                throw new ArgumentException("At least two classes are required.", "y");
            }

            var w = new double[n];
            double total = 0;
            for (var i = 0; i < n; i++)
            {
                w[i] = weights == null ? 1.0 : weights[i];
                total += w[i];
            }

            if (!(total > 0))
            {
                throw new ArgumentException("The sum of the weights must be positive.", "weights");
            }

            var edges = new double[p][];
            var missing = new bool[p];
            var featureStates = new int[p];
            var columns = new int[p][];
            for (var f = 0; f < p; f++)
            {
                Token.ThrowIfCancellationRequested();
                edges[f] = Edges(x, w, f, options.Bins);
                if (options.Missing == MissingValueHandling.State)
                {
                    for (var i = 0; i < n; i++)
                    {
                        if (double.IsNaN(x[i][f]) && w[i] > 0)
                        {
                            missing[f] = true;
                            break;
                        }
                    }
                }

                featureStates[f] = edges[f].Length + 1 + (missing[f] ? 1 : 0);
                columns[f] = new int[n];
                for (var i = 0; i < n; i++)
                {
                    columns[f][i] = LatentTreeClassifier.Bin(edges[f], missing[f], x[i][f]);
                }
            }

            var prior = new double[classCount];
            for (var i = 0; i < n; i++)
            {
                prior[y[i]] += w[i];
            }

            double priorSum = 0;
            for (var c = 0; c < classCount; c++)
            {
                prior[c] += options.Smoothing;
                priorSum += prior[c];
            }

            for (var c = 0; c < classCount; c++)
            {
                prior[c] /= priorSum;
            }

            var search = new LatentTreeStructureSearch(columns, featureStates, y, w, prior, options, Token);
            var groups = search.Run();
            Token.ThrowIfCancellationRequested();

            var parents = new List<int> { -1 };
            var states = new List<int> { classCount };
            var nodeColumns = new List<int[]> { null };
            var featureNodes = new int[p];
            foreach (var g in groups)
            {
                Flatten(g, 0, parents, states, nodeColumns, featureNodes, columns);
            }

            var shape = new LatentTreeShape(parents.ToArray(), states.ToArray());
            var fit = LatentTreeFitter.Fit(shape, nodeColumns.ToArray(), y, w, prior, options.Smoothing,
                MaxIterations, Tolerance, options.Seed, System.Math.Max(1, options.Restarts), Token);

            Iterations = fit.Iterations;
            LogLikelihood = fit.LogLikelihood;
            History = fit.History;

            return new LatentTreeClassifier(parents.ToArray(), states.ToArray(), fit.Conditionals,
                featureNodes, edges, missing);
        }

        private static void Flatten(LatentGroup group, int parent, List<int> parents, List<int> states,
            List<int[]> nodeColumns, int[] featureNodes, int[][] columns)
        {
            var index = parents.Count;
            parents.Add(parent);
            states.Add(group.States);
            nodeColumns.Add(group.Feature >= 0 ? columns[group.Feature] : null);
            if (group.Feature >= 0)
            {
                featureNodes[group.Feature] = index;
            }

            foreach (var c in group.Children)
            {
                Flatten(c, index, parents, states, nodeColumns, featureNodes, columns);
            }
        }

        private static void Validate(double[][] x, int[] y, double[] weights, LatentTreeOptions o)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (x.Length == 0)
            {
                throw new ArgumentException("At least one training sample is required.", "x");
            }

            if (x.Length != y.Length)
            {
                throw new DimensionMismatchException("y", "The number of outputs must match the number of inputs.");
            }

            if (weights != null && weights.Length != x.Length)
            {
                throw new DimensionMismatchException("weights",
                    "The number of weights must match the number of inputs.");
            }

            if (x[0] == null)
            {
                throw new ArgumentNullException("x");
            }

            var p = x[0].Length;
            if (p == 0)
            {
                throw new ArgumentException("Samples must have at least one feature.", "x");
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] == null)
                {
                    throw new ArgumentNullException("x");
                }

                if (x[i].Length != p)
                {
                    throw new DimensionMismatchException("x", "All samples must have the same number of features.");
                }

                if (y[i] < 0)
                {
                    throw new ArgumentOutOfRangeException("y", "Class labels must be non-negative.");
                }

                if (weights != null && (double.IsNaN(weights[i]) || double.IsInfinity(weights[i]) || weights[i] < 0))
                {
                    throw new ArgumentOutOfRangeException("weights", "Weights must be finite and non-negative.");
                }
            }

            if (o.Bins < 2)
            {
                throw new ArgumentOutOfRangeException("o", "Bins must be at least 2.");
            }

            if (o.MaxLatentStates < 2)
            {
                throw new ArgumentOutOfRangeException("o", "MaxLatentStates must be at least 2.");
            }

            if (o.MaxDepth < 0)
            {
                throw new ArgumentOutOfRangeException("o", "MaxDepth must not be negative.");
            }

            if (!(o.Smoothing > 0))
            {
                throw new ArgumentOutOfRangeException("o", "Smoothing must be positive.");
            }

            if (o.LocalMaxIterations < 1)
            {
                throw new ArgumentOutOfRangeException("o", "LocalMaxIterations must be at least 1.");
            }
        }

        private static double[] Edges(double[][] x, double[] w, int feature, int bins)
        {
            var values = new List<KeyValuePair<double, double>>();
            double total = 0;
            for (var i = 0; i < x.Length; i++)
            {
                var v = x[i][feature];
                if (double.IsNaN(v) || !(w[i] > 0))
                {
                    continue;
                }

                values.Add(new KeyValuePair<double, double>(v, w[i]));
                total += w[i];
            }

            if (values.Count == 0)
            {
                return new double[0];
            }

            values.Sort((a, b) => a.Key.CompareTo(b.Key));
            var max = values[values.Count - 1].Key;

            var edges = new List<double>();
            double cumulative = 0;
            var k = 1;
            for (var i = 0; i < values.Count && k < bins; i++)
            {
                cumulative += values[i].Value;
                var crossed = false;
                while (k < bins && cumulative >= total * k / bins - 1e-12 * total)
                {
                    crossed = true;
                    k++;
                }

                if (crossed)
                {
                    var v = values[i].Key;
                    if (v < max && (edges.Count == 0 || v > edges[edges.Count - 1]))
                    {
                        edges.Add(v);
                    }
                }
            }

            return edges.ToArray();
        }
    }
}