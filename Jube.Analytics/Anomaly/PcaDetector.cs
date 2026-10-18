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

using Jube.Analytics.Anomaly.Models;

namespace Jube.Analytics.Anomaly
{
    public sealed class PcaDetector(
        PcaScoreMode mode = PcaScoreMode.Combined,
        PcaRetention retention = PcaRetention.ExplainedVariance,
        double explainedVariance = 0.95,
        int fixedComponents = 0,
        int parallelAnalysisPermutations = 20,
        int parallelAnalysisRowCap = 2000)
        : AnomalyDetectorBase
    {
        private double[][] components = [];
        private double[] eigenvalues = [];
        private double hotellingScale = 1d;
        private double[] mean = [];
        private double reconstructionScale = 1d;
        public override string Name => "Pca";
        public PcaScoreMode Mode { get; } = mode;
        public PcaRetention Retention { get; } = retention;

        public double ExplainedVariance { get; } = explainedVariance is > 0 and <= 1
            ? explainedVariance
            : throw new ArgumentOutOfRangeException(nameof(explainedVariance));

        public int FixedComponents { get; } = fixedComponents;
        public int RetainedComponents => components.Length;

        protected override void FitCore(double[][] data, int seed, CancellationToken token)
        {
            var d = data[0].Length;

            if (d < 2)
            {
                throw new ArgumentException("PCA needs at least two variables.");
            }

            mean = DenseLinearAlgebra.Mean(data);
            var covariance = DenseLinearAlgebra.Covariance(data, mean);
            token.ThrowIfCancellationRequested();
            var (values, vectors) = DenseLinearAlgebra.SymmetricEigen(covariance);

            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Math.Max(values[i], 0d);
            }

            var limit = Math.Min(d - 1, data.Length - 1);
            int k;

            if (FixedComponents > 0)
            {
                k = Math.Min(FixedComponents, limit);
            }
            else if (Retention == PcaRetention.ParallelAnalysis)
            {
                k = ParallelAnalysisCount(data, values, seed, token);
            }
            else
            {
                var total = values.Sum();
                var cumulative = 0d;
                k = 0;

                while (k < values.Length && cumulative < ExplainedVariance * total)
                {
                    cumulative += values[k];
                    k++;
                }
            }

            k = Math.Clamp(k, 1, limit);
            components = vectors.Take(k).ToArray();
            eigenvalues = values.Take(k).Select(v => Math.Max(v, 1e-12)).ToArray();
            var reconstruction = new double[data.Length];
            var hotelling = new double[data.Length];

            for (var i = 0; i < data.Length; i++)
            {
                (reconstruction[i], hotelling[i]) = Decompose(data[i]);
            }

            reconstructionScale = Math.Max(reconstruction.Average(), 1e-12);
            hotellingScale = Math.Max(hotelling.Average(), 1e-12);
        }

        private int ParallelAnalysisCount(double[][] data, double[] observed, int seed, CancellationToken token)
        {
            var sample = AnomalyInput.Subsample(data, parallelAnalysisRowCap, AnomalyInput.DeriveSeed(seed, 3));
            var d = data[0].Length;
            var thresholds = new double[parallelAnalysisPermutations][];

            Parallel.For(0, parallelAnalysisPermutations, AnomalyInput.Options(token), p =>
            {
                var rng = new Random(AnomalyInput.DeriveSeed(seed, 1000 + p));
                var shuffled = new double[sample.Length][];

                for (var i = 0; i < sample.Length; i++)
                {
                    shuffled[i] = new double[d];
                }

                for (var j = 0; j < d; j++)
                {
                    var column = sample.Select(r => r[j]).ToArray();

                    for (var i = column.Length - 1; i > 0; i--)
                    {
                        var swap = rng.Next(i + 1);
                        (column[i], column[swap]) = (column[swap], column[i]);
                    }

                    for (var i = 0; i < sample.Length; i++)
                    {
                        shuffled[i][j] = column[i];
                    }
                }

                var m = DenseLinearAlgebra.Mean(shuffled);
                var (values, _) = DenseLinearAlgebra.SymmetricEigen(DenseLinearAlgebra.Covariance(shuffled, m));
                thresholds[p] = values;
            });

            var k = 0;

            for (var i = 0; i < d; i++)
            {
                var ordered = thresholds.Select(t => t[i]).OrderBy(v => v).ToArray();
                var cut = ordered[Math.Min(ordered.Length - 1, (int)Math.Ceiling(0.95 * ordered.Length) - 1)];

                if (observed[i] > cut)
                {
                    k++;
                }
                else
                {
                    break;
                }
            }

            return k;
        }

        private (double Reconstruction, double Hotelling) Decompose(double[] x)
        {
            var norm = 0d;

            for (var j = 0; j < x.Length; j++)
            {
                norm += (x[j] - mean[j]) * (x[j] - mean[j]);
            }

            var captured = 0d;
            var t2 = 0d;

            for (var c = 0; c < components.Length; c++)
            {
                var projection = 0d;

                for (var j = 0; j < x.Length; j++)
                {
                    projection += (x[j] - mean[j]) * components[c][j];
                }

                captured += projection * projection;
                t2 += projection * projection / eigenvalues[c];
            }

            return (Math.Max(norm - captured, 0d), t2);
        }

        protected override double ScoreCore(double[] row)
        {
            var (reconstruction, hotelling) = Decompose(row);

            return Mode switch
            {
                PcaScoreMode.Reconstruction => reconstruction,
                PcaScoreMode.Hotelling => hotelling,
                _ => reconstruction / reconstructionScale + hotelling / hotellingScale
            };
        }
    }
}