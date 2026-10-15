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

namespace Jube.Analytics.Calibration
{
    public static class BetaCalibration
    {
        public static BetaCalibrator Fit(double[] scores, int[] labels, double[]? weights = null,
            double epsilon = 1e-6, int maxIterations = 200, double tolerance = 1e-10,
            CancellationToken cancellationToken = default)
        {
            if (epsilon <= 0 || epsilon >= 0.5)
            {
                throw new ArgumentOutOfRangeException(nameof(epsilon), "The epsilon must be between 0 and 0.5.");
            }

            if (maxIterations < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIterations));
            }

            var (rawScores, y, w) = Numerics.CleanPairs(scores, labels, weights);
            var n = rawScores.Length;

            if (n == 0)
            {
                throw new InvalidOperationException("Beta calibration needs at least one finite row.");
            }

            var f1 = new double[n];
            var f2 = new double[n];

            for (var i = 0; i < n; i++)
            {
                if (!Numerics.IsFinite(rawScores[i]))
                {
                    throw new ArgumentException("Scores must be finite.");
                }

                if (rawScores[i] < 0 || rawScores[i] > 1)
                {
                    throw new ArgumentException("Beta calibration needs scores bounded between 0 and 1.");
                }

                var bounded = Math.Min(Math.Max(rawScores[i], epsilon), 1d - epsilon);
                f1[i] = Math.Log(bounded);
                f2[i] = Math.Log(1d - bounded);
            }

            double weightPositive = 0, weightNegative = 0;

            for (var i = 0; i < y.Length; i++)
            {
                if (y[i] == 1)
                {
                    weightPositive += w[i];
                }
                else
                {
                    weightNegative += w[i];
                }
            }

            if (weightPositive <= 0 || weightNegative <= 0)
            {
                throw new InvalidOperationException(
                    "Calibration needs at least one positive and one negative row with a positive weight.");
            }

            var high = (weightPositive + 1d) / (weightPositive + 2d);
            var low = 1d / (weightNegative + 2d);
            var t = new double[n];

            for (var i = 0; i < n; i++)
            {
                t[i] = y[i] == 1 ? high : low;
            }

            double Objective(double a, double b, double c)
            {
                var sum = 0d;

                for (var i = 0; i < n; i++)
                {
                    var eta = a * f1[i] + b * f2[i] + c;
                    sum += w[i] * (Numerics.Softplus(eta) - t[i] * eta);
                }

                return sum;
            }

            var a0 = 0d;
            var b0 = 0d;
            var c0 = Math.Log((weightPositive + 1d) / (weightNegative + 1d));
            var current = Objective(a0, b0, c0);
            var totalWeight = weightPositive + weightNegative;

            for (var iteration = 0; iteration < maxIterations; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                double ga = 0, gb = 0, gc = 0;
                double haa = 1e-12, hab = 0, hac = 0, hbb = 1e-12, hbc = 0, hcc = 1e-12;

                for (var i = 0; i < n; i++)
                {
                    var eta = a0 * f1[i] + b0 * f2[i] + c0;
                    var p = Numerics.Sigmoid(eta);
                    var d = w[i] * (p - t[i]);
                    var h = w[i] * p * (1d - p);
                    ga += d * f1[i];
                    gb += d * f2[i];
                    gc += d;
                    haa += h * f1[i] * f1[i];
                    hab += h * f1[i] * f2[i];
                    hac += h * f1[i];
                    hbb += h * f2[i] * f2[i];
                    hbc += h * f2[i];
                    hcc += h;
                }

                if (Math.Max(Math.Abs(ga), Math.Max(Math.Abs(gb), Math.Abs(gc))) / totalWeight < tolerance)
                {
                    break;
                }

                if (!SolveSymmetric3(haa, hab, hac, hbb, hbc, hcc, -ga, -gb, -gc, out var da, out var db, out var dc))
                {
                    break;
                }

                var directional = ga * da + gb * db + gc * dc;
                var step = 1d;
                var accepted = false;

                while (step >= 1e-10)
                {
                    var next = Objective(a0 + step * da, b0 + step * db, c0 + step * dc);

                    if (next < current + 1e-4 * step * directional)
                    {
                        a0 += step * da;
                        b0 += step * db;
                        c0 += step * dc;
                        current = next;
                        accepted = true;
                        break;
                    }

                    step /= 2d;
                }

                if (!accepted)
                {
                    break;
                }
            }

            return new BetaCalibrator(a0, b0, c0, epsilon);
        }

        private static bool SolveSymmetric3(double haa, double hab, double hac, double hbb, double hbc, double hcc,
            double ga, double gb, double gc, out double x0, out double x1, out double x2)
        {
            var m = new[,]
            {
                { haa, hab, hac, ga },
                { hab, hbb, hbc, gb },
                { hac, hbc, hcc, gc }
            };

            for (var col = 0; col < 3; col++)
            {
                var pivot = col;

                for (var row = col + 1; row < 3; row++)
                {
                    if (Math.Abs(m[row, col]) > Math.Abs(m[pivot, col]))
                    {
                        pivot = row;
                    }
                }

                if (Math.Abs(m[pivot, col]) < 1e-14)
                {
                    x0 = x1 = x2 = 0;

                    return false;
                }

                if (pivot != col)
                {
                    for (var c = 0; c < 4; c++)
                    {
                        (m[pivot, c], m[col, c]) = (m[col, c], m[pivot, c]);
                    }
                }

                for (var row = col + 1; row < 3; row++)
                {
                    var factor = m[row, col] / m[col, col];

                    for (var c = col; c < 4; c++)
                    {
                        m[row, c] -= factor * m[col, c];
                    }
                }
            }

            x2 = m[2, 3] / m[2, 2];
            x1 = (m[1, 3] - m[1, 2] * x2) / m[1, 1];
            x0 = (m[0, 3] - m[0, 1] * x1 - m[0, 2] * x2) / m[0, 0];

            return double.IsFinite(x0) && double.IsFinite(x1) && double.IsFinite(x2);
        }
    }
}