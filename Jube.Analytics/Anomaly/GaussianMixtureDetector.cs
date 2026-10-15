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

namespace Jube.Analytics.Anomaly
{
    public sealed class GaussianMixtureDetector(
        int components = 3,
        bool fullCovariance = false,
        int iterations = 60,
        int rowCap = 10000) : AnomalyDetectorBase
    {
        private double[] constants = [];
        private double[][,] factors = [];
        private double[][] means = [];
        private double[][] variances = [];
        private double[] weight = [];
        public override string Name => FullCovariance ? "GaussianMixtureFull" : "GaussianMixtureDiagonal";

        public int Components { get; } =
            components > 0 ? components : throw new ArgumentOutOfRangeException(nameof(components));

        public bool FullCovariance { get; } = fullCovariance;

        protected override void FitCore(double[][] all, int seed, CancellationToken token)
        {
            var data = AnomalyInput.Subsample(all, rowCap, AnomalyInput.DeriveSeed(seed, 1));
            var n = data.Length;
            var d = data[0].Length;
            var k = Components;
            var needed = FullCovariance ? k * (d + 1) : 2 * k;

            if (n < needed)
            {
                throw new ArgumentException(
                    $"A {(FullCovariance ? "full" : "diagonal")} mixture of {k} components over {d} variables needs at least {needed} rows and {n} were supplied.");
            }

            var rng = new Random(AnomalyInput.DeriveSeed(seed, 2));
            means = new double[k][];
            var first = rng.Next(n);
            means[0] = (double[])data[first].Clone();
            var nearest = data.Select(r => DenseLinearAlgebra.SquaredDistance(r, means[0])).ToArray();

            for (var c = 1; c < k; c++)
            {
                var total = nearest.Sum();
                var target = rng.NextDouble() * total;
                var pick = 0;
                var acc = 0d;

                for (var i = 0; i < n; i++)
                {
                    acc += nearest[i];

                    if (acc >= target)
                    {
                        pick = i;
                        break;
                    }
                }

                means[c] = (double[])data[pick].Clone();

                for (var i = 0; i < n; i++)
                {
                    nearest[i] = Math.Min(nearest[i], DenseLinearAlgebra.SquaredDistance(data[i], means[c]));
                }
            }

            var globalMean = DenseLinearAlgebra.Mean(data);
            var globalVariance = new double[d];

            foreach (var row in data)
            {
                for (var j = 0; j < d; j++)
                {
                    globalVariance[j] += (row[j] - globalMean[j]) * (row[j] - globalMean[j]) / n;
                }
            }

            var floor = globalVariance.Select(v => Math.Max(v * 1e-3, 1e-9)).ToArray();
            weight = Enumerable.Repeat(1d / k, k).ToArray();

            variances = Enumerable.Range(0, k)
                .Select(_ => globalVariance.Select((v, j) => Math.Max(v, floor[j])).ToArray())
                .ToArray();

            factors = new double[k][,];

            for (var c = 0; c < k; c++)
            {
                factors[c] = DiagonalFactor(variances[c]);
            }

            constants = new double[k];
            Refresh(k, d);
            var resp = new double[n][];

            for (var i = 0; i < n; i++)
            {
                resp[i] = new double[k];
            }

            var previous = double.NegativeInfinity;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                token.ThrowIfCancellationRequested();
                var likelihood = 0d;

                for (var i = 0; i < n; i++)
                {
                    var logs = new double[k];

                    for (var c = 0; c < k; c++)
                    {
                        logs[c] = Math.Log(weight[c]) + ComponentLogDensity(c, data[i]);
                    }

                    var max = logs.Max();
                    var sum = 0d;

                    for (var c = 0; c < k; c++)
                    {
                        sum += Math.Exp(logs[c] - max);
                    }

                    likelihood += max + Math.Log(sum);

                    for (var c = 0; c < k; c++)
                    {
                        resp[i][c] = Math.Exp(logs[c] - max) / sum;
                    }
                }

                for (var c = 0; c < k; c++)
                {
                    var mass = 0d;

                    for (var i = 0; i < n; i++)
                    {
                        mass += resp[i][c];
                    }

                    mass = Math.Max(mass, 1e-9);
                    weight[c] = mass / n;
                    var m = new double[d];

                    for (var i = 0; i < n; i++)
                    {
                        for (var j = 0; j < d; j++)
                        {
                            m[j] += resp[i][c] * data[i][j];
                        }
                    }

                    for (var j = 0; j < d; j++)
                    {
                        m[j] /= mass;
                    }

                    means[c] = m;
                    var v = new double[d];

                    for (var i = 0; i < n; i++)
                    {
                        for (var j = 0; j < d; j++)
                        {
                            v[j] += resp[i][c] * (data[i][j] - m[j]) * (data[i][j] - m[j]);
                        }
                    }

                    for (var j = 0; j < d; j++)
                    {
                        variances[c][j] = Math.Max(v[j] / mass, floor[j]);
                    }

                    if (FullCovariance)
                    {
                        var s = new double[d, d];

                        for (var i = 0; i < n; i++)
                        {
                            var r = resp[i][c];

                            for (var a = 0; a < d; a++)
                            {
                                var ca = r * (data[i][a] - m[a]);

                                for (var b = a; b < d; b++)
                                {
                                    s[a, b] += ca * (data[i][b] - m[b]);
                                }
                            }
                        }

                        for (var a = 0; a < d; a++)
                        {
                            for (var b = a; b < d; b++)
                            {
                                s[a, b] /= mass;
                                s[b, a] = s[a, b];
                            }

                            s[a, a] = Math.Max(s[a, a], floor[a]) + 1e-3 * globalVariance.Average();
                        }

                        factors[c] = DenseLinearAlgebra.Cholesky(s) ??
                                     throw new ArgumentException("A mixture component covariance is singular.");
                    }
                }

                Refresh(k, d);

                if (Math.Abs(likelihood - previous) < 1e-6 * Math.Abs(likelihood))
                {
                    break;
                }

                previous = likelihood;
            }
        }

        private void Refresh(int k, int d)
        {
            for (var c = 0; c < k; c++)
            {
                if (FullCovariance)
                {
                    constants[c] = -0.5 * (d * Math.Log(2d * Math.PI) + DenseLinearAlgebra.LogDeterminant(factors[c]));
                }
                else
                {
                    constants[c] = -0.5 * (d * Math.Log(2d * Math.PI) + variances[c].Sum(Math.Log));
                }
            }
        }

        private static double[,] DiagonalFactor(double[] variance)
        {
            var l = new double[variance.Length, variance.Length];

            for (var i = 0; i < variance.Length; i++)
            {
                l[i, i] = Math.Sqrt(variance[i]);
            }

            return l;
        }

        private double ComponentLogDensity(int c, double[] x)
        {
            if (FullCovariance)
            {
                return constants[c] - 0.5 * DenseLinearAlgebra.SquaredMahalanobis(factors[c], x, means[c]);
            }

            var q = 0d;

            for (var j = 0; j < x.Length; j++)
            {
                var diff = x[j] - means[c][j];
                q += diff * diff / variances[c][j];
            }

            return constants[c] - 0.5 * q;
        }

        protected override double ScoreCore(double[] row)
        {
            var k = weight.Length;
            var logs = new double[k];

            for (var c = 0; c < k; c++)
            {
                logs[c] = Math.Log(weight[c]) + ComponentLogDensity(c, row);
            }

            var max = logs.Max();
            var sum = 0d;

            for (var c = 0; c < k; c++)
            {
                sum += Math.Exp(logs[c] - max);
            }

            return -(max + Math.Log(sum));
        }
    }
}