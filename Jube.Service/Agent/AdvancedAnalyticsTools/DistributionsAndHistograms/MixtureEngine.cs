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

using Accord;
using Accord.Math;
using Accord.Statistics;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal sealed record MixtureFit(
        DistributionEntry Entry,
        double[] Weights,
        double[][] Parameters,
        double LogLikelihood,
        bool Converged,
        int Iterations);

    internal static class MixtureEngine
    {
        public static double[][] Responsibilities(DistributionEntry entry, double[] weights, double[][] parameters,
            double[] points)
        {
            var models = parameters.Select(p => UnivariateModel.Create(entry.Kind, p, "componentParameters")).ToArray();
            var result = new double[points.Length][];

            for (var i = 0; i < points.Length; i++)
            {
                var row = new double[models.Length];
                var sum = 0.0;

                for (var j = 0; j < models.Length; j++)
                {
                    var d = models[j].Pdf(points[i]);
                    row[j] = weights[j] * (double.IsFinite(d) ? d : 0.0);
                    sum += row[j];
                }

                for (var j = 0; j < row.Length; j++)
                {
                    row[j] = sum > 0 ? row[j] / sum : 1.0 / row.Length;
                }

                result[i] = row;
            }

            return result;
        }

        public static double LogLikelihood(DistributionEntry entry, double[] weights, double[][] parameters,
            double[] data)
        {
            var models = parameters.Select(p => UnivariateModel.Create(entry.Kind, p, "componentParameters")).ToArray();
            var total = 0.0;

            foreach (var x in data)
            {
                var density = 0.0;

                for (var j = 0; j < models.Length; j++)
                {
                    var d = models[j].Pdf(x);

                    if (double.IsFinite(d))
                    {
                        density += weights[j] * d;
                    }
                }

                total += density > 0 ? Math.Log(density) : -1e10;
            }

            return total;
        }

        private static UnivariateModel[] InitialComponents(DistributionEntry entry, double[] data, int k, int restart,
            Random random)
        {
            var sorted = (double[])data.Clone();
            Array.Sort(sorted);
            var models = new UnivariateModel[k];

            if (restart == 0)
            {
                for (var j = 0; j < k; j++)
                {
                    var from = (int)((long)j * sorted.Length / k);
                    var to = (int)((long)(j + 1) * sorted.Length / k);
                    var chunk = sorted.Skip(from).Take(Math.Max(to - from, 2)).ToArray();
                    models[j] = FitChunk(entry, chunk, data);
                }

                return models;
            }

            var centers = Enumerable.Range(0, k).Select(_ => sorted[random.Next(sorted.Length)]).OrderBy(c => c)
                .ToArray();

            for (var j = 0; j < k; j++)
            {
                var nearest = sorted.OrderBy(v => Math.Abs(v - centers[j])).Take(Math.Max(sorted.Length / (2 * k), 3))
                    .ToArray();

                models[j] = FitChunk(entry, nearest, data);
            }

            return models;
        }

        private static UnivariateModel FitChunk(DistributionEntry entry, double[] chunk, double[] data)
        {
            try
            {
                return DistributionFitter.Fit(entry, chunk, null).Model;
            }
            catch (ArgumentException)
            {
                return DistributionFitter.Fit(entry, data, null).Model;
            }
        }

        public static MixtureFit Fit(DistributionEntry entry, double[] data, int k, int seed, int maxIterations,
            double tolerance, int restarts)
        {
            MixtureFit? best = null;
            var random = new Random(seed);
            Exception? last = null;

            for (var restart = 0; restart < restarts; restart++)
            {
                try
                {
                    var models = InitialComponents(entry, data, k, restart, random);
                    var pdf = models.Select(m => (IFittableDistribution<double>)m.Raw).ToArray();
                    var coefficients = Enumerable.Repeat(1.0 / k, k).ToArray();

                    var em = new ExpectationMaximization<double>(coefficients, pdf)
                    {
                        ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1 }
                    };

                    var convergence = new RelativeConvergence(maxIterations, tolerance);
                    em.Convergence = convergence;

                    if (entry.Kind == DistributionKind.Normal)
                    {
                        em.InnerOptions = new NormalOptions
                            { Regularization = 1e-9 * Math.Max(data.Variance(), 1e-12) };
                    }

                    em.Compute(data);
                    var parameters = em.Distributions.Select(d => entry.Extract!(d)).ToArray();
                    var weights = em.Coefficients.ToArray();

                    if (parameters.Any(p => p.Any(v => !double.IsFinite(v))) || weights.Any(w => !double.IsFinite(w)))
                    {
                        continue;
                    }

                    var ll = LogLikelihood(entry, weights, parameters, data);

                    if (!double.IsFinite(ll))
                    {
                        continue;
                    }

                    var candidate = new MixtureFit(entry, weights, parameters, ll,
                        convergence.CurrentIteration < maxIterations, convergence.CurrentIteration);

                    if (best == null || candidate.LogLikelihood > best.LogLikelihood)
                    {
                        best = candidate;
                    }
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or AggregateException
                                               or ConvergenceException or NotSupportedException or OverflowException)
                {
                    last = ex;
                }
            }

            if (best == null)
            {
                throw new ArgumentException(
                    $"The mixture could not be fitted{(last != null ? ": " + last.Message : ".")} Try fewer components, more values or a different seed.",
                    "components");
            }

            var order = Enumerable.Range(0, k).OrderBy(i => best.Parameters[i][0]).ToArray();

            return best with
            {
                Weights = order.Select(i => best.Weights[i]).ToArray(),
                Parameters = order.Select(i => best.Parameters[i]).ToArray()
            };
        }

        public static int[] Assign(double[][] responsibilities)
        {
            return responsibilities.Select(r =>
            {
                var index = 0;

                for (var j = 1; j < r.Length; j++)
                {
                    if (r[j] > r[index])
                    {
                        index = j;
                    }
                }

                return index;
            }).ToArray();
        }
    }
}