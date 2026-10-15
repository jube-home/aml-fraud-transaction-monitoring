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

using Jube.Charting.Exceptions;
using Jube.Charting.Performance.Models;

namespace Jube.Charting.Performance
{
    public static class PerformanceMath
    {
        public static GainsResult Gains(IReadOnlyList<bool> actual, IReadOnlyList<double> scores,
            IReadOnlyList<double>? weights = null, int maxPoints = 1000)
        {
            var (s, y, w) = Clean(actual, scores, weights);
            double positives = 0;
            double negatives = 0;

            for (var i = 0; i < y.Length; i++)
            {
                var weight = w?[i] ?? 1;

                if (y[i])
                {
                    positives += weight;
                }
                else
                {
                    negatives += weight;
                }
            }

            if (s.Length == 0 || positives <= 0 || negatives <= 0)
            {
                return new GainsResult([], [], [], positives, negatives);
            }

            var groups = Groups(s, y, w);
            var population = new List<double> { 0 };
            var captured = new List<double> { 0 };
            var thresholds = new List<double> { double.PositiveInfinity };
            var total = positives + negatives;
            double seen = 0;
            double found = 0;

            foreach (var g in groups)
            {
                seen += g.Positives + g.Negatives;
                found += g.Positives;
                population.Add(seen / total);
                captured.Add(found / positives);
                thresholds.Add(g.Score);
            }

            Thin(population, captured, thresholds, maxPoints);

            return new GainsResult([.. population], [.. captured], [.. thresholds], positives,
                negatives);
        }

        public static IReadOnlyList<DecileRow> Deciles(IReadOnlyList<bool> actual, IReadOnlyList<double> scores,
            int bins = 10, IReadOnlyList<double>? weights = null)
        {
            var (s, y, w) = Clean(actual, scores, weights);
            double positives = 0;
            double total = 0;

            for (var i = 0; i < y.Length; i++)
            {
                var weight = w?[i] ?? 1;
                total += weight;

                if (y[i])
                {
                    positives += weight;
                }
            }

            if (s.Length == 0 || positives <= 0 || positives >= total || bins < 1)
            {
                return [];
            }

            var groups = Groups(s, y, w);
            var baseRate = positives / total;
            var rows = new List<DecileRow>();
            var previous = 0d;

            for (var k = 1; k <= bins; k++)
            {
                var upTo = k == bins ? total : total * k / bins;
                var cumulative = PositivesWithin(groups, upTo);
                var inDecile = cumulative - previous;
                var size = upTo - total * (k - 1) / bins;

                rows.Add(new DecileRow(k, size / total, inDecile,
                    size > 0 ? inDecile / size / baseRate : double.NaN,
                    cumulative / upTo / baseRate,
                    cumulative / positives));

                previous = cumulative;
            }

            return rows;
        }

        public static KsResult Ks(IReadOnlyList<bool> actual, IReadOnlyList<double> scores, int maxPoints = 600,
            IReadOnlyList<double>? weights = null)
        {
            var (s, y, w) = Clean(actual, scores, weights);
            double positives = 0;
            double negatives = 0;

            for (var i = 0; i < y.Length; i++)
            {
                var weight = w?[i] ?? 1;

                if (y[i])
                {
                    positives += weight;
                }
                else
                {
                    negatives += weight;
                }
            }

            if (s.Length == 0 || positives <= 0 || negatives <= 0)
            {
                return new KsResult([], [], [], double.NaN, double.NaN, double.NaN, double.NaN);
            }

            var groups = Groups(s, y, w).OrderBy(g => g.Score).ToList();
            var xs = new List<double>();
            var pos = new List<double>();
            var neg = new List<double>();
            double p = 0;
            double n = 0;
            var best = -1;
            var bestGap = double.NegativeInfinity;

            foreach (var g in groups)
            {
                p += g.Positives;
                n += g.Negatives;
                var pc = p / positives;
                var nc = n / negatives;
                xs.Add(g.Score);
                pos.Add(pc);
                neg.Add(nc);

                if (Math.Abs(nc - pc) > bestGap)
                {
                    bestGap = Math.Abs(nc - pc);
                    best = xs.Count - 1;
                }
            }

            var keep = Indices(xs.Count, maxPoints, best);

            return new KsResult([.. keep.Select(i => xs[i])], [.. keep.Select(i => pos[i])],
                [.. keep.Select(i => neg[i])], bestGap, xs[best], pos[best], neg[best]);
        }

        public static SweepResult Sweep(IReadOnlyList<bool> actual, IReadOnlyList<double> scores, double from,
            double to, int steps = 100)
        {
            var (s, y, _) = Clean(actual, scores);
            var pos = s.Where((_, i) => y[i]).OrderBy(v => v).ToArray();
            var neg = s.Where((_, i) => !y[i]).OrderBy(v => v).ToArray();

            if (pos.Length == 0 || neg.Length == 0 || steps < 1 || !(to > from))
            {
                return new SweepResult([], -1);
            }

            var points = new SweepPoint[steps + 1];
            var bestIndex = 0;
            var bestF1 = double.NegativeInfinity;

            for (var i = 0; i <= steps; i++)
            {
                var t = from + (to - from) * i / steps;
                double tp = pos.Length - LowerBound(pos, t);
                double fp = neg.Length - LowerBound(neg, t);
                var fn = pos.Length - tp;
                var tn = neg.Length - fp;
                var predicted = tp + fp;
                var precision = predicted > 0 ? tp / predicted : double.NaN;
                var recall = tp / pos.Length;
                var f1 = 2 * tp + fp + fn > 0 ? 2 * tp / (2 * tp + fp + fn) : 0;
                var denominator = Math.Sqrt((tp + fp) * (tp + fn) * (tn + fp) * (tn + fn));
                var mcc = denominator > 0 ? (tp * tn - fp * fn) / denominator : 0;
                points[i] = new SweepPoint(t, precision, recall, f1, mcc, tn / neg.Length);

                if (f1 > bestF1)
                {
                    bestF1 = f1;
                    bestIndex = i;
                }
            }

            return new SweepResult(points, bestIndex);
        }

        public static DistributionResult Distribution(IReadOnlyList<bool> actual, IReadOnlyList<double> scores,
            double from, double to, int bins = 20, IReadOnlyList<double>? weights = null)
        {
            var (s, y, w) = Clean(actual, scores, weights);

            if (bins < 1 || !double.IsFinite(from) || !double.IsFinite(to) || !(to > from))
            {
                return new DistributionResult([], [], [], 0, 0);
            }

            var edges = Enumerable.Range(0, bins + 1).Select(i => from + (to - from) * i / bins).ToArray();
            var pos = new double[bins];
            var neg = new double[bins];

            for (var i = 0; i < s.Length; i++)
            {
                var bin = (int)Math.Floor((s[i] - from) / (to - from) * bins);
                bin = Math.Clamp(bin, 0, bins - 1);
                var weight = w?[i] ?? 1;

                if (y[i])
                {
                    pos[bin] += weight;
                }
                else
                {
                    neg[bin] += weight;
                }
            }

            return new DistributionResult(edges, pos, neg, pos.Sum(), neg.Sum());
        }

        private static (double[] Scores, bool[] Actual, double[]? Weights) Clean(IReadOnlyList<bool> actual,
            IReadOnlyList<double> scores, IReadOnlyList<double>? weights = null)
        {
            if (actual.Count != scores.Count)
            {
                throw new ChartSpecificationException("Actual outcomes and scores must have the same length.");
            }

            if (weights is not null && weights.Count != scores.Count)
            {
                throw new ChartSpecificationException("Weights must have one value per observation.");
            }

            ChartLimits.Observations(actual.Count);
            var s = new List<double>(scores.Count);
            var y = new List<bool>(scores.Count);
            var w = weights is null ? null : new List<double>(scores.Count);

            for (var i = 0; i < scores.Count; i++)
            {
                if (double.IsFinite(scores[i]) && (weights is null || double.IsFinite(weights[i])))
                {
                    s.Add(scores[i]);
                    y.Add(actual[i]);
                    w?.Add(weights![i]);
                }
            }

            return ([.. s], [.. y], w?.ToArray());
        }

        private static List<Group> Groups(double[] s, bool[] y, double[]? weights = null)
        {
            var order = Enumerable.Range(0, s.Length).OrderByDescending(i => s[i]).ToArray();
            var groups = new List<Group>();
            var index = 0;

            while (index < order.Length)
            {
                var score = s[order[index]];
                double p = 0;
                double n = 0;

                while (index < order.Length && Math.Abs(s[order[index]] - score) < 0.0001)
                {
                    var weight = weights?[order[index]] ?? 1;

                    if (y[order[index]])
                    {
                        p += weight;
                    }
                    else
                    {
                        n += weight;
                    }

                    index++;
                }

                groups.Add(new Group(score, p, n));
            }

            return groups;
        }

        private static double PositivesWithin(List<Group> groups, double upTo)
        {
            double seen = 0;
            double found = 0;

            foreach (var g in groups)
            {
                var size = g.Positives + g.Negatives;

                if (seen + size <= upTo + 1e-9)
                {
                    seen += size;
                    found += g.Positives;
                    continue;
                }

                found += g.Positives * (upTo - seen) / size;
                break;
            }

            return found;
        }

        private static int LowerBound(double[] sorted, double value)
        {
            int lo = 0, hi = sorted.Length;

            while (lo < hi)
            {
                var mid = (lo + hi) >>> 1;

                if (sorted[mid] < value)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }

        private static List<int> Indices(int count, int maxPoints, int mustKeep)
        {
            if (count <= maxPoints)
            {
                return [.. Enumerable.Range(0, count)];
            }

            var keep = new SortedSet<int> { 0, count - 1, mustKeep };

            for (var i = 0; i < maxPoints; i++)
            {
                keep.Add((int)((long)i * (count - 1) / (maxPoints - 1)));
            }

            return [.. keep];
        }

        private static void Thin(List<double> x, List<double> y, List<double> t, int maxPoints)
        {
            if (x.Count <= maxPoints)
            {
                return;
            }

            var keep = Indices(x.Count, maxPoints, 0);
            var nx = keep.Select(i => x[i]).ToList();
            var ny = keep.Select(i => y[i]).ToList();
            var nt = keep.Select(i => t[i]).ToList();
            x.Clear();
            y.Clear();
            t.Clear();
            x.AddRange(nx);
            y.AddRange(ny);
            t.AddRange(nt);
        }

        private readonly record struct Group(double Score, double Positives, double Negatives);
    }
}