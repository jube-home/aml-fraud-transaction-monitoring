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

using Jube.Charting.Charts.Models;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Charts
{
    public static class CurveMath
    {
        public static RocResult Roc(IReadOnlyList<bool> actual, IReadOnlyList<double> scores,
            IReadOnlyList<double>? weights = null)
        {
            var (order, positives, negatives) = Prepare(actual, scores, weights);

            if (positives <= 0 || negatives <= 0)
            {
                throw new ChartSpecificationException(
                    "A ROC curve needs at least one positive and one negative observation.");
            }

            var fpr = new List<double> { 0 };
            var tpr = new List<double> { 0 };
            var thr = new List<double> { double.PositiveInfinity };
            double tp = 0;
            double fp = 0;
            var i = 0;

            while (i < order.Length)
            {
                var score = scores[order[i]];

                while (i < order.Length && Math.Abs(scores[order[i]] - score) < 0.0001)
                {
                    var weight = weights?[order[i]] ?? 1;

                    if (actual[order[i]])
                    {
                        tp += weight;
                    }
                    else
                    {
                        fp += weight;
                    }

                    i++;
                }

                fpr.Add(fp / negatives);
                tpr.Add(tp / positives);
                thr.Add(score);
            }

            var auc = 0.0;

            for (var k = 1; k < fpr.Count; k++)
            {
                auc += (fpr[k] - fpr[k - 1]) * (tpr[k] + tpr[k - 1]) / 2;
            }

            var keep = Thin(fpr.Count);

            return new RocResult([.. keep.Select(k => fpr[k])], [.. keep.Select(k => tpr[k])],
                [.. keep.Select(k => thr[k])], auc);
        }

        public static double Auc(IReadOnlyList<double> x, IReadOnlyList<double> y)
        {
            var area = 0.0;

            for (var i = 1; i < Math.Min(x.Count, y.Count); i++)
            {
                if (double.IsFinite(x[i]) && double.IsFinite(x[i - 1]) && double.IsFinite(y[i]) &&
                    double.IsFinite(y[i - 1]))
                {
                    area += (x[i] - x[i - 1]) * (y[i] + y[i - 1]) / 2;
                }
            }

            return area;
        }

        public static PrecisionRecallResult PrecisionRecall(IReadOnlyList<bool> actual, IReadOnlyList<double> scores)
        {
            var (order, positives, negatives) = Prepare(actual, scores);

            if (positives == 0)
            {
                throw new ChartSpecificationException(
                    "A precision-recall curve needs at least one positive observation.");
            }

            var recall = new List<double> { 0 };
            var precision = new List<double> { 1 };
            var thr = new List<double> { double.PositiveInfinity };
            double tp = 0;
            double fp = 0;
            var ap = 0.0;
            var previousRecall = 0.0;
            var i = 0;

            while (i < order.Length)
            {
                var score = scores[order[i]];

                while (i < order.Length && Math.Abs(scores[order[i]] - score) < 0.0001)
                {
                    if (actual[order[i]])
                    {
                        tp++;
                    }
                    else
                    {
                        fp++;
                    }

                    i++;
                }

                var r = tp / positives;
                var p = tp / (tp + fp);
                ap += (r - previousRecall) * p;
                previousRecall = r;
                recall.Add(r);
                precision.Add(p);
                thr.Add(score);
            }

            var keep = Thin(recall.Count);

            return new PrecisionRecallResult([.. keep.Select(k => recall[k])],
                [.. keep.Select(k => precision[k])],
                [.. keep.Select(k => thr[k])], ap, positives / (positives + negatives));
        }

        public static CalibrationResult Calibration(IReadOnlyList<bool> actual, IReadOnlyList<double> probabilities,
            int bins = 10, bool quantile = false)
        {
            if (actual.Count != probabilities.Count)
            {
                throw new ChartSpecificationException("Outcomes and probabilities must have the same length.");
            }

            ChartLimits.Observations(actual.Count);
            bins = Math.Clamp(bins, 2, 50);
            var idx = Enumerable.Range(0, actual.Count).Where(i => double.IsFinite(probabilities[i])).ToArray();

            if (idx.Length == 0)
            {
                throw new ChartSpecificationException("Calibration needs at least one finite probability.");
            }

            var sorted = idx.Select(i => probabilities[i]).OrderBy(v => v).ToArray();
            var edges = new double[bins + 1];

            if (quantile)
            {
                for (var b = 0; b <= bins; b++)
                {
                    edges[b] = sorted[
                        Math.Min((int)Math.Round((double)b / bins * (sorted.Length - 1)), sorted.Length - 1)];
                }

                edges[0] = Math.Min(edges[0], 0);
                edges[bins] = Math.Max(edges[bins], 1);
            }
            else
            {
                for (var b = 0; b <= bins; b++)
                {
                    edges[b] = (double)b / bins;
                }
            }

            var counts = new double[bins];
            var sumP = new double[bins];
            var sumY = new double[bins];

            foreach (var i in idx)
            {
                var p = probabilities[i];
                var bin = Array.BinarySearch(edges, p);
                bin = bin >= 0 ? bin : ~bin - 1;
                bin = Math.Clamp(bin, 0, bins - 1);
                counts[bin]++;
                sumP[bin] += p;
                sumY[bin] += actual[i] ? 1 : 0;
            }

            var mean = new List<double>();
            var frac = new List<double>();
            var keptCounts = new List<double>();
            var ece = 0.0;

            for (var b = 0; b < bins; b++)
            {
                if (counts[b] == 0)
                {
                    mean.Add(double.NaN);
                    frac.Add(double.NaN);
                    keptCounts.Add(0);
                    continue;
                }

                var m = sumP[b] / counts[b];
                var f = sumY[b] / counts[b];
                mean.Add(m);
                frac.Add(f);
                keptCounts.Add(counts[b]);
                ece += counts[b] / idx.Length * Math.Abs(m - f);
            }

            return new CalibrationResult([.. mean], [.. frac], [.. keptCounts], edges, ece);
        }

        private const int MaxCurvePoints = 2000;

        private static (int[] Order, double Positives, double Negatives) Prepare(IReadOnlyList<bool> actual,
            IReadOnlyList<double> scores, IReadOnlyList<double>? weights = null)
        {
            if (actual.Count != scores.Count)
            {
                throw new ChartSpecificationException("Outcomes and scores must have the same length.");
            }

            if (weights is not null && weights.Count != scores.Count)
            {
                throw new ChartSpecificationException("Weights must have one value per observation.");
            }

            ChartLimits.Observations(actual.Count);

            var idx = Enumerable.Range(0, actual.Count)
                .Where(i => double.IsFinite(scores[i]) && (weights is null || double.IsFinite(weights[i])))
                .ToArray();

            Array.Sort(idx, (a, b) => scores[b].CompareTo(scores[a]));
            double positives = 0;
            double negatives = 0;

            foreach (var i in idx)
            {
                var weight = weights?[i] ?? 1;

                if (actual[i])
                {
                    positives += weight;
                }
                else
                {
                    negatives += weight;
                }
            }

            return (idx, positives, negatives);
        }

        private static int[] Thin(int count)
        {
            if (count <= MaxCurvePoints)
            {
                return [.. Enumerable.Range(0, count)];
            }

            var keep = new SortedSet<int> { 0, count - 1 };

            for (var i = 1; i < MaxCurvePoints - 1; i++)
            {
                keep.Add((int)Math.Round((double)i * (count - 1) / (MaxCurvePoints - 1)));
            }

            return [.. keep];
        }
    }
}