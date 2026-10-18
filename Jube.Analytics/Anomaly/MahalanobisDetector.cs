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
    public sealed class MahalanobisDetector(
        CovarianceRegularisation regularisation = CovarianceRegularisation.LedoitWolf,
        bool robust = false,
        double ridge = 0.1,
        int robustRowCap = 2000,
        double supportFraction = 0.75)
        : AnomalyDetectorBase
    {
        private double[] centre = [];
        private double[,] factor = new double[0, 0];
        public override string Name => Robust ? "RobustMahalanobis" : "Mahalanobis";
        public CovarianceRegularisation Regularisation { get; } = regularisation;
        public bool Robust { get; } = robust;
        public double Ridge { get; } = ridge > 0 ? ridge : throw new ArgumentOutOfRangeException(nameof(ridge));
        public int RobustRowCap { get; } = robustRowCap;

        public double SupportFraction { get; } =
            supportFraction is > 0.5 and <= 1
                ? supportFraction
                : throw new ArgumentOutOfRangeException(nameof(supportFraction));

        protected override void FitCore(double[][] data, int seed, CancellationToken token)
        {
            var d = data[0].Length;

            if (Regularisation == CovarianceRegularisation.None && d >= data.Length)
            {
                throw new ArgumentException(
                    $"The covariance of {d} variables cannot be inverted from {data.Length} rows without regularisation.");
            }

            if (!Robust)
            {
                (centre, factor) = Estimate(data, token);

                return;
            }

            var sample = AnomalyInput.Subsample(data, RobustRowCap, AnomalyInput.DeriveSeed(seed, 7));

            var h = Math.Clamp((int)Math.Ceiling(SupportFraction * sample.Length), Math.Min(sample.Length, 2),
                sample.Length);

            var rng = new Random(AnomalyInput.DeriveSeed(seed, 11));
            var bestLogDet = double.PositiveInfinity;
            double[] bestCentre = [];
            var bestFactor = new double[0, 0];
            var start = Math.Min(sample.Length, Math.Max(d + 1, 2));

            for (var attempt = 0; attempt < 6; attempt++)
            {
                token.ThrowIfCancellationRequested();
                var subset = AnomalyInput.Subsample(sample, start, rng.Next());
                var (c, l) = Estimate(subset, token);
                var previous = double.PositiveInfinity;

                for (var step = 0; step < 12; step++)
                {
                    var distances = new double[sample.Length];

                    for (var i = 0; i < sample.Length; i++)
                    {
                        distances[i] = DenseLinearAlgebra.SquaredMahalanobis(l, sample[i], c);
                    }

                    var order = Enumerable.Range(0, sample.Length).OrderBy(i => distances[i]).Take(h).ToArray();
                    var chosen = order.Select(i => sample[i]).ToArray();
                    (c, l) = Estimate(chosen, token);
                    var logDet = DenseLinearAlgebra.LogDeterminant(l);

                    if (Math.Abs(previous - logDet) < 1e-9)
                    {
                        break;
                    }

                    previous = logDet;
                }

                var final = DenseLinearAlgebra.LogDeterminant(l);

                if (final < bestLogDet)
                {
                    bestLogDet = final;
                    bestCentre = c;
                    bestFactor = l;
                }
            }

            centre = bestCentre;
            factor = bestFactor;
        }

        private (double[], double[,]) Estimate(double[][] rows, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var mean = DenseLinearAlgebra.Mean(rows);
            var s = DenseLinearAlgebra.Covariance(rows, mean);

            var shrunk = CovarianceShrinkage.Apply(rows, mean, s,
                Regularisation == CovarianceRegularisation.None && Robust
                    ? CovarianceRegularisation.LedoitWolf
                    : Regularisation, Ridge);

            var l = DenseLinearAlgebra.Cholesky(shrunk);

            if (l == null)
            {
                var d = mean.Length;
                var trace = 0d;

                for (var i = 0; i < d; i++)
                {
                    trace += shrunk[i, i];
                }

                for (var i = 0; i < d; i++)
                {
                    shrunk[i, i] += 1e-6 * Math.Max(trace / d, 1e-12);
                }

                l = DenseLinearAlgebra.Cholesky(shrunk) ??
                    throw new ArgumentException("The covariance matrix is singular and could not be regularised.");
            }

            return (mean, l);
        }

        protected override double ScoreCore(double[] row)
        {
            return DenseLinearAlgebra.SquaredMahalanobis(factor, row, centre);
        }
    }
}