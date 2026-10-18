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
using System.Collections.Generic;
using System.Threading;

namespace Accord.MachineLearning.LatentTrees
{
    internal sealed class LatentTreeFit
    {
        public double[][][] Conditionals;
        public double[] History;
        public int Iterations;
        public double LogLikelihood;
        public double Objective;
    }

    internal static class LatentTreeFitter
    {
        public static LatentTreeFit Fit(LatentTreeShape shape, int[][] columns, int[] classes, double[] weights,
            double[] classPrior, double smoothing, int maxIterations, double tolerance, int seed, int restarts,
            CancellationToken token)
        {
            var random = false;
            for (var u = 1; u < shape.Count; u++)
            {
                if (shape.Parents[u] != 0 || shape.Children[u].Length > 0)
                {
                    random = true;
                }
            }

            if (!random)
            {
                restarts = 1;
            }

            LatentTreeFit best = null;
            for (var r = 0; r < restarts; r++)
            {
                token.ThrowIfCancellationRequested();
                var fit = FitOnce(shape, columns, classes, weights, classPrior, smoothing, maxIterations, tolerance,
                    unchecked(seed * 7919 + r * 104729 + 13), token);
                if (best == null || fit.Objective > best.Objective)
                {
                    best = fit;
                }
            }

            return best;
        }

        private static LatentTreeFit FitOnce(LatentTreeShape shape, int[][] columns, int[] classes, double[] weights,
            double[] classPrior, double alpha, int maxIterations, double tolerance, int seed, CancellationToken token)
        {
            var n = classes.Length;
            var nodes = shape.Count;
            double total = 0;
            for (var i = 0; i < n; i++)
            {
                total += weights[i];
            }

            var rng = new Random(seed);
            var cpts = new double[nodes][][];
            var counts = new double[nodes][][];
            cpts[0] = new[] { (double[])classPrior.Clone() };
            for (var u = 1; u < nodes; u++)
            {
                var sp = shape.States[shape.Parents[u]];
                var su = shape.States[u];
                cpts[u] = new double[sp][];
                counts[u] = new double[sp][];
                for (var s = 0; s < sp; s++)
                {
                    cpts[u][s] = new double[su];
                    counts[u][s] = new double[su];
                }

                if (shape.Parents[u] == 0 && shape.Children[u].Length == 0)
                {
                    var col = columns[u];
                    for (var i = 0; i < n; i++)
                    {
                        if (col[i] >= 0)
                        {
                            counts[u][classes[i]][col[i]] += weights[i];
                        }
                    }

                    Normalise(cpts[u], counts[u], alpha);
                    Clear(counts[u]);
                }
                else
                {
                    for (var s = 0; s < sp; s++)
                    {
                        double sum = 0;
                        for (var t = 0; t < su; t++)
                        {
                            var v = 0.1 + 1.8 * rng.NextDouble();
                            cpts[u][s][t] = v;
                            sum += v;
                        }

                        for (var t = 0; t < su; t++)
                        {
                            cpts[u][s][t] /= sum;
                        }
                    }
                }
            }

            var ws = new LatentTreeWorkspace(shape);
            var history = new List<double>();
            var previous = double.NegativeInfinity;
            var iterations = 0;

            for (var it = 1; it <= maxIterations; it++)
            {
                token.ThrowIfCancellationRequested();
                for (var u = 1; u < nodes; u++)
                {
                    Clear(counts[u]);
                }

                double ll = 0;
                for (var i = 0; i < n; i++)
                {
                    if ((i & 511) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var w = weights[i];
                    if (w <= 0)
                    {
                        continue;
                    }

                    for (var u = 0; u < nodes; u++)
                    {
                        ws.Observed[u] = columns[u] == null ? -1 : columns[u][i];
                    }

                    var logScale = LatentTreeInference.Upward(shape, cpts, ws);
                    var l0 = ws.Lambda[0][classes[i]];
                    if (double.IsNegativeInfinity(logScale) || !(l0 > 0))
                    {
                        continue;
                    }

                    ll += w * (System.Math.Log(l0) + logScale);
                    LatentTreeInference.Accumulate(shape, cpts, ws, classes[i], w, counts);
                }

                var objective = ll + Penalty(cpts, alpha);
                history.Add(objective);
                iterations = it;
                var converged = it > 1 && System.Math.Abs(objective - previous) < tolerance * total;
                previous = objective;

                for (var u = 1; u < nodes; u++)
                {
                    Normalise(cpts[u], counts[u], alpha);
                }

                if (converged)
                {
                    break;
                }
            }

            double finalLl = 0;
            for (var i = 0; i < n; i++)
            {
                if ((i & 511) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                if (weights[i] <= 0)
                {
                    continue;
                }

                for (var u = 0; u < nodes; u++)
                {
                    ws.Observed[u] = columns[u] == null ? -1 : columns[u][i];
                }

                var logScale = LatentTreeInference.Upward(shape, cpts, ws);
                var l0 = ws.Lambda[0][classes[i]];
                if (double.IsNegativeInfinity(logScale) || !(l0 > 0))
                {
                    continue;
                }

                finalLl += weights[i] * (System.Math.Log(l0) + logScale);
            }

            return new LatentTreeFit
            {
                Conditionals = cpts,
                LogLikelihood = finalLl,
                Objective = finalLl + Penalty(cpts, alpha),
                Iterations = iterations,
                History = history.ToArray()
            };
        }

        private static void Clear(double[][] counts)
        {
            for (var s = 0; s < counts.Length; s++)
            {
                Array.Clear(counts[s], 0, counts[s].Length);
            }
        }

        private static void Normalise(double[][] cpt, double[][] counts, double alpha)
        {
            for (var s = 0; s < cpt.Length; s++)
            {
                var row = cpt[s];
                var c = counts[s];
                double sum = 0;
                for (var t = 0; t < row.Length; t++)
                {
                    sum += c[t] + alpha;
                }

                for (var t = 0; t < row.Length; t++)
                {
                    row[t] = (c[t] + alpha) / sum;
                }
            }
        }

        private static double Penalty(double[][][] cpts, double alpha)
        {
            double sum = 0;
            for (var u = 1; u < cpts.Length; u++)
            {
                for (var s = 0; s < cpts[u].Length; s++)
                {
                    for (var t = 0; t < cpts[u][s].Length; t++)
                    {
                        sum += System.Math.Log(cpts[u][s][t]);
                    }
                }
            }

            return alpha * sum;
        }
    }
}