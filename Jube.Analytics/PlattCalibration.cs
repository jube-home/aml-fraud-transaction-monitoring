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

using Jube.Analytics.Models;

namespace Jube.Analytics
{
    public static class PlattCalibration
    {
        public static Calibrator Fit(double[] scores, int[] labels, double[]? weights = null,
            CalibrationInput mode = CalibrationInput.RawScore, double epsilon = 1e-6, int maxIterations = 200,
            double tolerance = 1e-10, CancellationToken cancellationToken = default)
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

            var x = new double[rawScores.Length];

            for (var i = 0; i < x.Length; i++)
            {
                if (!Numerics.IsFinite(rawScores[i]))
                {
                    throw new ArgumentException("Scores must be finite.");
                }

                if (mode == CalibrationInput.LogitOfBoundedScore)
                {
                    if (rawScores[i] < 0 || rawScores[i] > 1)
                    {
                        throw new ArgumentException("Bounded scores must be between 0 and 1.");
                    }

                    var bounded = Math.Min(Math.Max(rawScores[i], epsilon), 1d - epsilon);
                    x[i] = Math.Log(bounded / (1d - bounded));
                }
                else
                {
                    x[i] = rawScores[i];
                }
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

            var weighted = Enumerable.Range(0, x.Length).Where(i => w[i] > 0).ToArray();

            if (weighted.Select(i => x[i]).Distinct().Count() < 2)
            {
                throw new InvalidOperationException("Calibration needs scores that are not all equal.");
            }

            var totalWeight = weightPositive + weightNegative;
            var mean = weighted.Sum(i => w[i] * x[i]) / totalWeight;
            var variance = weighted.Sum(i => w[i] * (x[i] - mean) * (x[i] - mean)) / totalWeight;
            var scale = Math.Sqrt(variance);

            var high = (weightPositive + 1d) / (weightPositive + 2d);
            var low = 1d / (weightNegative + 2d);
            var z = new double[x.Length];
            var t = new double[x.Length];

            for (var i = 0; i < x.Length; i++)
            {
                z[i] = (x[i] - mean) / scale;
                t[i] = y[i] == 1 ? high : low;
            }

            double Objective(double a, double b)
            {
                var sum = 0d;

                for (var i = 0; i < z.Length; i++)
                {
                    var eta = a * z[i] + b;
                    sum += w[i] * (Numerics.Softplus(eta) - t[i] * eta);
                }

                return sum;
            }

            var slope = 0d;
            var intercept = Math.Log((weightPositive + 1d) / (weightNegative + 1d));
            var current = Objective(slope, intercept);

            for (var iteration = 0; iteration < maxIterations; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                double ga = 0, gb = 0, haa = 1e-12, hab = 0, hbb = 1e-12;

                for (var i = 0; i < z.Length; i++)
                {
                    var p = Numerics.Sigmoid(slope * z[i] + intercept);
                    var d = w[i] * (p - t[i]);
                    var h = w[i] * p * (1d - p);
                    ga += d * z[i];
                    gb += d;
                    haa += h * z[i] * z[i];
                    hab += h * z[i];
                    hbb += h;
                }

                if (Math.Max(Math.Abs(ga), Math.Abs(gb)) / totalWeight < tolerance)
                {
                    break;
                }

                var determinant = haa * hbb - hab * hab;
                var da = -(hbb * ga - hab * gb) / determinant;
                var db = -(-hab * ga + haa * gb) / determinant;
                var directional = ga * da + gb * db;

                var step = 1d;
                var accepted = false;

                while (step >= 1e-10)
                {
                    var next = Objective(slope + step * da, intercept + step * db);

                    if (next < current + 1e-4 * step * directional)
                    {
                        slope += step * da;
                        intercept += step * db;
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

            return new Calibrator(slope / scale, intercept - slope * mean / scale, mode, epsilon);
        }
    }
}