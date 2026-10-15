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

using System.Text.Json;
using Jube.Analytics.Calibration.Models;

namespace Jube.Analytics.Calibration
{
    public sealed class IsotonicCalibrator
    {
        private static readonly JsonSerializerOptions serialiser = new() { PropertyNameCaseInsensitive = true };

        private IsotonicCalibrator(IReadOnlyList<IsotonicPoint> points, double epsilon)
        {
            Points = points;
            Epsilon = epsilon;
        }

        public IReadOnlyList<IsotonicPoint> Points { get; }
        public double Epsilon { get; }

        public static IsotonicCalibrator Fit(double[] scores, int[] labels, double[]? weights = null,
            double epsilon = 1e-6)
        {
            var (x, y, w) = Numerics.CleanPairs(scores, labels, weights);

            if (x.Any(v => !Numerics.IsFinite(v)))
            {
                throw new ArgumentException("Scores must be finite.");
            }

            if (x.Length == 0)
            {
                throw new InvalidOperationException("Isotonic calibration needs at least one finite, weighted row.");
            }

            if (x.Select(v => v).Distinct().Count() < 2)
            {
                throw new InvalidOperationException("Isotonic calibration needs scores that are not all equal.");
            }

            var order = Enumerable.Range(0, x.Length).OrderBy(i => x[i]).ToArray();

            var sumY = new List<double>();
            var sumW = new List<double>();
            var minX = new List<double>();
            var maxX = new List<double>();

            foreach (var i in order)
            {
                sumY.Add(y[i] * w[i]);
                sumW.Add(w[i]);
                minX.Add(x[i]);
                maxX.Add(x[i]);

                while (sumY.Count > 1 && sumY[^2] / sumW[^2] > sumY[^1] / sumW[^1])
                {
                    var last = sumY.Count - 1;
                    sumY[last - 1] += sumY[last];
                    sumW[last - 1] += sumW[last];
                    maxX[last - 1] = maxX[last];
                    sumY.RemoveAt(last);
                    sumW.RemoveAt(last);
                    minX.RemoveAt(last);
                    maxX.RemoveAt(last);
                }
            }

            var points = new List<IsotonicPoint>(sumY.Count);

            for (var k = 0; k < sumY.Count; k++)
            {
                var value = Math.Clamp(sumY[k] / sumW[k], epsilon, 1d - epsilon);
                points.Add(new IsotonicPoint((minX[k] + maxX[k]) / 2d, value));
            }

            return new IsotonicCalibrator(Dedupe(points), epsilon);
        }

        public double Apply(double score)
        {
            if (!double.IsFinite(score))
            {
                return double.NaN;
            }

            if (Points.Count == 1 || score <= Points[0].X)
            {
                return Points[0].Y;
            }

            if (score >= Points[^1].X)
            {
                return Points[^1].Y;
            }

            var lo = 0;
            var hi = Points.Count - 1;

            while (hi - lo > 1)
            {
                var mid = (lo + hi) / 2;

                if (Points[mid].X <= score)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            var a = Points[lo];
            var b = Points[hi];

            if (Math.Abs(b.X - a.X) < 0.0001)
            {
                return a.Y;
            }

            var t = (score - a.X) / (b.X - a.X);

            return a.Y + t * (b.Y - a.Y);
        }

        public double[] Apply(double[] scores)
        {
            ArgumentNullException.ThrowIfNull(scores);
            var result = new double[scores.Length];

            for (var i = 0; i < scores.Length; i++)
            {
                result[i] = Apply(scores[i]);
            }

            return result;
        }

        public IsotonicCalibratorState ToState()
        {
            return new IsotonicCalibratorState
            {
                Epsilon = Epsilon, X = Points.Select(p => p.X).ToList(), Y = Points.Select(p => p.Y).ToList()
            };
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(ToState(), serialiser);
        }

        public static IsotonicCalibrator FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            IsotonicCalibratorState? state;

            try
            {
                state = JsonSerializer.Deserialize<IsotonicCalibratorState>(json, serialiser);
            }
            catch (JsonException ex)
            {
                throw new FormatException("The isotonic calibrator is not valid JSON.", ex);
            }

            return FromState(state ?? throw new FormatException("The isotonic calibrator is empty."));
        }

        public static IsotonicCalibrator FromState(IsotonicCalibratorState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (state.Version != 1)
            {
                throw new NotSupportedException($"Isotonic calibrator version {state.Version} is not supported.");
            }

            if (state.X == null || state.Y == null || state.X.Count == 0 || state.X.Count != state.Y.Count)
            {
                throw new FormatException("The isotonic calibrator has mismatched or empty points.");
            }

            var points = new List<IsotonicPoint>(state.X.Count);

            for (var i = 0; i < state.X.Count; i++)
            {
                if (!double.IsFinite(state.X[i]) || !double.IsFinite(state.Y[i]) || state.Y[i] < 0 || state.Y[i] > 1)
                {
                    throw new FormatException("The isotonic calibrator has an invalid point.");
                }

                if (i > 0 && state.X[i] <= state.X[i - 1])
                {
                    throw new FormatException("The isotonic calibrator points must be strictly increasing in x.");
                }

                points.Add(new IsotonicPoint(state.X[i], state.Y[i]));
            }

            if (state.Epsilon <= 0 || state.Epsilon >= 0.5)
            {
                throw new FormatException("The isotonic calibrator epsilon must be between 0 and 0.5.");
            }

            return new IsotonicCalibrator(points, state.Epsilon);
        }

        private static List<IsotonicPoint> Dedupe(List<IsotonicPoint> points)
        {
            var result = new List<IsotonicPoint>(points.Count);

            foreach (var p in points)
            {
                if (result.Count > 0 && Math.Abs(result[^1].X - p.X) < 0.0001)
                {
                    result[^1] = p;
                    continue;
                }

                result.Add(p);
            }

            return result;
        }
    }
}