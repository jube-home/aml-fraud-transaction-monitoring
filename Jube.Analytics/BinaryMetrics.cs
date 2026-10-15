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
    public static class BinaryMetrics
    {
        public static ConfusionMatrix Confusion(double[] scores, int[] labels, double threshold)
        {
            if (double.IsNaN(threshold))
            {
                throw new ArgumentOutOfRangeException(nameof(threshold));
            }

            var (s, y, _) = Numerics.CleanPairs(scores, labels, null);
            int tp = 0, fp = 0, tn = 0, fn = 0;

            for (var i = 0; i < s.Length; i++)
            {
                var predicted = s[i] >= threshold;

                if (y[i] == 1)
                {
                    if (predicted)
                    {
                        tp++;
                    }
                    else
                    {
                        fn++;
                    }
                }
                else if (predicted)
                {
                    fp++;
                }
                else
                {
                    tn++;
                }
            }

            return new ConfusionMatrix(tp, fp, tn, fn);
        }

        public static double RocAuc(double[] scores, int[] labels)
        {
            var (s, y, _) = Numerics.CleanPairs(scores, labels, null);
            var positives = y.Count(l => l == 1);
            var negatives = y.Length - positives;

            if (positives == 0 || negatives == 0)
            {
                return double.NaN;
            }

            var ranks = Numerics.AverageRanks(s);
            var sum = 0d;

            for (var i = 0; i < s.Length; i++)
            {
                if (y[i] == 1)
                {
                    sum += ranks[i];
                }
            }

            return (sum - positives * (positives + 1d) / 2d) / ((double)positives * negatives);
        }

        public static double Gini(double[] scores, int[] labels)
        {
            return 2d * RocAuc(scores, labels) - 1d;
        }

        public static AucResult RocAucWithStandardError(double[] scores, int[] labels)
        {
            var (s, y, _) = Numerics.CleanPairs(scores, labels, null);
            var positiveIndex = Enumerable.Range(0, s.Length).Where(i => y[i] == 1).ToArray();
            var negativeIndex = Enumerable.Range(0, s.Length).Where(i => y[i] == 0).ToArray();
            var m = positiveIndex.Length;
            var n = negativeIndex.Length;

            if (m == 0 || n == 0)
            {
                return new AucResult(double.NaN, double.NaN);
            }

            var all = Numerics.AverageRanks(s);
            var positiveRanks = Numerics.AverageRanks(positiveIndex.Select(i => s[i]).ToArray());
            var negativeRanks = Numerics.AverageRanks(negativeIndex.Select(i => s[i]).ToArray());

            var v10 = new double[m];

            for (var k = 0; k < m; k++)
            {
                v10[k] = (all[positiveIndex[k]] - positiveRanks[k]) / n;
            }

            var v01 = new double[n];

            for (var k = 0; k < n; k++)
            {
                v01[k] = 1d - (all[negativeIndex[k]] - negativeRanks[k]) / m;
            }

            var auc = v10.Average();

            if (m < 2 || n < 2)
            {
                return new AucResult(auc, double.NaN);
            }

            var s10 = v10.Sum(v => (v - auc) * (v - auc)) / (m - 1);
            var s01 = v01.Sum(v => (v - auc) * (v - auc)) / (n - 1);

            return new AucResult(auc, Math.Sqrt(s10 / m + s01 / n));
        }

        public static RocPoint[] RocCurve(double[] scores, int[] labels)
        {
            var (s, y, _) = Numerics.CleanPairs(scores, labels, null);
            var positives = y.Count(l => l == 1);
            var negatives = y.Length - positives;

            if (positives == 0 || negatives == 0)
            {
                throw new InvalidOperationException("A ROC curve needs both classes.");
            }

            var order = Enumerable.Range(0, s.Length).OrderByDescending(i => s[i]).ToArray();
            var points = new List<RocPoint> { new(double.PositiveInfinity, 0d, 0d) };
            int tp = 0, fp = 0;
            var index = 0;

            while (index < order.Length)
            {
                var threshold = s[order[index]];

                while (index < order.Length && s[order[index]].Equals(threshold))
                {
                    if (y[order[index]] == 1)
                    {
                        tp++;
                    }
                    else
                    {
                        fp++;
                    }

                    index++;
                }

                points.Add(new RocPoint(threshold, fp / (double)negatives, tp / (double)positives));
            }

            return points.ToArray();
        }

        public static double KolmogorovSmirnov(double[] scores, int[] labels)
        {
            return RocCurve(scores, labels).Max(p => Math.Abs(p.TruePositiveRate - p.FalsePositiveRate));
        }

        public static PrecisionRecallPoint[] PrecisionRecallCurve(double[] scores, int[] labels)
        {
            var (s, y, _) = Numerics.CleanPairs(scores, labels, null);
            var positives = y.Count(l => l == 1);

            if (positives == 0)
            {
                throw new InvalidOperationException("A precision recall curve needs at least one positive.");
            }

            var order = Enumerable.Range(0, s.Length).OrderByDescending(i => s[i]).ToArray();
            var points = new List<PrecisionRecallPoint>();
            int tp = 0, seen = 0;
            var index = 0;

            while (index < order.Length)
            {
                var threshold = s[order[index]];

                while (index < order.Length && s[order[index]].Equals(threshold))
                {
                    if (y[order[index]] == 1)
                    {
                        tp++;
                    }

                    seen++;
                    index++;
                }

                points.Add(new PrecisionRecallPoint(threshold, tp / (double)seen, tp / (double)positives));
            }

            return points.ToArray();
        }

        public static double AveragePrecision(double[] scores, int[] labels)
        {
            var (_, y, _) = Numerics.CleanPairs(scores, labels, null);

            if (y.All(l => l == 0))
            {
                return double.NaN;
            }

            var previousRecall = 0d;
            var sum = 0d;

            foreach (var point in PrecisionRecallCurve(scores, labels))
            {
                sum += (point.Recall - previousRecall) * point.Precision;
                previousRecall = point.Recall;
            }

            return sum;
        }

        public static double LogLoss(double[] probabilities, int[] labels, double[]? weights = null,
            double epsilon = 1e-15)
        {
            var (p, y, w) = CleanProbabilities(probabilities, labels, weights);

            if (epsilon <= 0 || epsilon >= 0.5)
            {
                throw new ArgumentOutOfRangeException(nameof(epsilon));
            }

            double sum = 0, total = 0;

            for (var i = 0; i < p.Length; i++)
            {
                var clipped = Math.Min(Math.Max(p[i], epsilon), 1d - epsilon);
                sum -= w[i] * (y[i] == 1 ? Math.Log(clipped) : Math.Log(1d - clipped));
                total += w[i];
            }

            return total <= 0 ? double.NaN : sum / total;
        }

        public static double BrierScore(double[] probabilities, int[] labels, double[]? weights = null)
        {
            var (p, y, w) = CleanProbabilities(probabilities, labels, weights);
            double sum = 0, total = 0;

            for (var i = 0; i < p.Length; i++)
            {
                sum += w[i] * (p[i] - y[i]) * (p[i] - y[i]);
                total += w[i];
            }

            return total <= 0 ? double.NaN : sum / total;
        }

        public static ReliabilityBin[] Reliability(double[] probabilities, int[] labels, int bins = 10)
        {
            if (bins < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bins));
            }

            var (p, y, _) = CleanProbabilities(probabilities, labels, null);
            var counts = new int[bins];
            var sumPredicted = new double[bins];
            var sumObserved = new double[bins];

            for (var i = 0; i < p.Length; i++)
            {
                var bin = Math.Min(bins - 1, (int)Math.Floor(p[i] * bins));
                counts[bin]++;
                sumPredicted[bin] += p[i];
                sumObserved[bin] += y[i];
            }

            var result = new ReliabilityBin[bins];

            for (var b = 0; b < bins; b++)
            {
                result[b] = new ReliabilityBin(b / (double)bins, (b + 1) / (double)bins, counts[b],
                    counts[b] == 0 ? double.NaN : sumPredicted[b] / counts[b],
                    counts[b] == 0 ? double.NaN : sumObserved[b] / counts[b]);
            }

            return result;
        }

        public static double ExpectedCalibrationError(double[] probabilities, int[] labels, int bins = 10)
        {
            var reliability = Reliability(probabilities, labels, bins);
            var total = reliability.Sum(b => b.Count);

            return reliability.Where(b => b.Count > 0)
                .Sum(b => b.Count / (double)total * Math.Abs(b.ObservedRate - b.MeanPredicted));
        }

        private static (double[] P, int[] Y, double[] W) CleanProbabilities(double[] probabilities, int[] labels,
            double[]? weights)
        {
            var cleaned = Numerics.CleanPairs(probabilities, labels, weights);

            if (cleaned.Scores.Any(v => v < 0 || v > 1))
            {
                throw new ArgumentException("Probabilities must be between 0 and 1.");
            }

            return cleaned;
        }
    }
}