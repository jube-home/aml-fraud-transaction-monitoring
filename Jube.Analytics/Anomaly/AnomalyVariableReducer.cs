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
    public static class AnomalyVariableReducer
    {
        public static ReducerModel Fit(double[][] data, IReadOnlyList<string>? names, ReductionOptions options,
            int seed,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            AnomalyInput.Validate(data);
            options.Validate();
            var n = data.Length;
            var d = data[0].Length;

            if (names != null && names.Count != d)
            {
                throw new ArgumentException($"{names.Count} names were supplied for {d} variables.");
            }

            string Name(int j)
            {
                return names?[j] ?? $"V{j}";
            }

            var reasons = new string?[d];
            var details = new string?[d];
            var fill = new double[d];
            var missing = new double[d];

            for (var j = 0; j < d; j++)
            {
                var finite = new List<double>(n);

                for (var i = 0; i < n; i++)
                {
                    if (Numerics.IsFinite(data[i][j]))
                    {
                        finite.Add(data[i][j]);
                    }
                }

                missing[j] = (n - finite.Count) / (double)n;

                if (finite.Count > 0)
                {
                    var sorted = finite.ToArray();
                    Array.Sort(sorted);
                    fill[j] = Numerics.Median(sorted);
                }
            }

            var filled = new double[n][];

            for (var i = 0; i < n; i++)
            {
                filled[i] = new double[d];

                for (var j = 0; j < d; j++)
                {
                    filled[i][j] = Numerics.IsFinite(data[i][j]) ? data[i][j] : fill[j];
                }
            }

            if (options.Mode == ReductionMode.Standard)
            {
                for (var j = 0; j < d; j++)
                {
                    if (missing[j] > options.MissingRateThreshold)
                    {
                        reasons[j] = "MissingRate";
                        details[j] = $"missing rate {missing[j]:0.####} exceeds {options.MissingRateThreshold:0.####}";
                    }
                }

                for (var j = 0; j < d; j++)
                {
                    if (reasons[j] != null)
                    {
                        continue;
                    }

                    var counts = new Dictionary<double, int>();

                    for (var i = 0; i < n; i++)
                    {
                        counts[filled[i][j]] = counts.GetValueOrDefault(filled[i][j]) + 1;
                    }

                    if (counts.Count == 1)
                    {
                        reasons[j] = "Constant";
                        details[j] = $"single value {filled[0][j]:R}";
                        continue;
                    }

                    var ordered = counts.Values.OrderByDescending(v => v).ToArray();
                    var ratio = ordered[0] / (double)ordered[1];
                    var percentUnique = 100d * counts.Count / n;

                    if (ratio > options.FrequencyRatioCutoff && percentUnique < options.UniqueValuePercentCutoff)
                    {
                        reasons[j] = "NearZeroVariance";

                        details[j] =
                            $"frequency ratio {ratio:0.###} exceeds {options.FrequencyRatioCutoff:0.###} and unique values {percentUnique:0.###}% is below {options.UniqueValuePercentCutoff:0.###}%";
                    }
                }

                if (options.DropZeroMedianAbsoluteDeviation)
                {
                    for (var j = 0; j < d; j++)
                    {
                        if (reasons[j] != null)
                        {
                            continue;
                        }

                        var sorted = filled.Select(r => r[j]).OrderBy(v => v).ToArray();
                        var median = Numerics.Median(sorted);
                        var deviations = sorted.Select(v => Math.Abs(v - median)).OrderBy(v => v).ToArray();

                        if (Numerics.Median(deviations) == 0d)
                        {
                            reasons[j] = "ZeroMedianAbsoluteDeviation";
                            details[j] = "more than half of the rows equal the median";
                        }
                    }
                }

                token.ThrowIfCancellationRequested();

                if (options.CorrelationThreshold is { } threshold)
                {
                    GroupCorrelated(filled, reasons, details, Name, threshold, options, seed, token);
                }
            }

            var kept = Enumerable.Range(0, d).Where(j => reasons[j] == null).ToArray();

            if (kept.Length == 0)
            {
                throw new ArgumentException(
                    "The reduction removed every variable; relax the thresholds or disable it.");
            }

            var centre = new double[kept.Length];
            var scale = new double[kept.Length];

            for (var c = 0; c < kept.Length; c++)
            {
                var column = filled.Select(r => r[kept[c]]).ToArray();
                (centre[c], scale[c]) = Scale(column, options.Scaling);
            }

            var keptFill = kept.Select(j => fill[j]).ToArray();
            double[][] projection = [];
            var projectionMean = new double[kept.Length];

            if (options.Projection != ReductionProjection.None && kept.Length >= 2)
            {
                var scaled = new double[n][];

                for (var i = 0; i < n; i++)
                {
                    scaled[i] = new double[kept.Length];

                    for (var c = 0; c < kept.Length; c++)
                    {
                        scaled[i][c] = (filled[i][kept[c]] - centre[c]) / scale[c];
                    }
                }

                projectionMean = DenseLinearAlgebra.Mean(scaled);

                var (values, vectors) = DenseLinearAlgebra.SymmetricEigen(
                    DenseLinearAlgebra.Covariance(scaled, projectionMean));

                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = Math.Max(values[i], 0d);
                }

                int k;

                if (options.Projection == ReductionProjection.ParallelAnalysis)
                {
                    var pca = new PcaDetector(retention: PcaRetention.ParallelAnalysis);
                    pca.Fit(scaled, seed, token);
                    k = pca.RetainedComponents;
                }
                else
                {
                    var total = values.Sum();
                    var cumulative = 0d;
                    k = 0;

                    while (k < values.Length && cumulative < options.ExplainedVariance * total)
                    {
                        cumulative += values[k];
                        k++;
                    }
                }

                k = Math.Clamp(k, 1, Math.Min(kept.Length, n - 1));
                projection = vectors.Take(k).ToArray();
            }

            var decisions = new List<VariableDecision>(d);

            for (var j = 0; j < d; j++)
            {
                decisions.Add(reasons[j] == null
                    ? new VariableDecision(j, Name(j), true, "Kept", details[j] ?? string.Empty)
                    : new VariableDecision(j, Name(j), false, reasons[j]!, details[j] ?? string.Empty));
            }

            return new ReducerModel(d, kept, keptFill, centre, scale, projection, projectionMean, decisions, options);
        }

        private static (double Centre, double Scale) Scale(double[] column, ReductionScaling scaling)
        {
            if (scaling == ReductionScaling.None)
            {
                return (0d, 1d);
            }

            if (scaling == ReductionScaling.ZScore)
            {
                var mean = column.Average();
                var sd = Math.Sqrt(column.Sum(v => (v - mean) * (v - mean)) / column.Length);

                return (mean, sd > 0 ? sd : 1d);
            }

            var sorted = (double[])column.Clone();
            Array.Sort(sorted);
            var median = Numerics.Median(sorted);
            var deviations = sorted.Select(v => Math.Abs(v - median)).OrderBy(v => v).ToArray();
            var mad = RobustScaler.NormalConsistency * Numerics.Median(deviations);

            if (mad > 0)
            {
                return (median, mad);
            }

            var iqr = (Numerics.Quantile(sorted, 0.75) - Numerics.Quantile(sorted, 0.25)) / 1.349;

            if (iqr > 0)
            {
                return (median, iqr);
            }

            var m = sorted.Average();
            var s = Math.Sqrt(sorted.Sum(v => (v - m) * (v - m)) / sorted.Length);

            return (median, s > 0 ? s : 1d);
        }

        private static void GroupCorrelated(double[][] filled, string?[] reasons, string?[] details,
            Func<int, string> name,
            double threshold, ReductionOptions options, int seed, CancellationToken token)
        {
            var alive = Enumerable.Range(0, filled[0].Length).Where(j => reasons[j] == null).ToArray();

            if (alive.Length < 2)
            {
                return;
            }

            var sample = AnomalyInput.Subsample(filled, options.CorrelationRowCap, AnomalyInput.DeriveSeed(seed, 41));
            var m = alive.Length;
            var x = new double[m][];

            for (var a = 0; a < m; a++)
            {
                var column = sample.Select(r => r[alive[a]]).ToArray();
                var mean = column.Average();
                var sd = Math.Sqrt(column.Sum(v => (v - mean) * (v - mean)));
                x[a] = column.Select(v => sd > 0 ? (v - mean) / sd : 0d).ToArray();
            }

            var rho = new double[m, m];

            Parallel.For(0, m, AnomalyInput.Options(token), a =>
            {
                for (var b = a; b < m; b++)
                {
                    var sum = 0d;

                    for (var i = 0; i < x[a].Length; i++)
                    {
                        sum += x[a][i] * x[b][i];
                    }

                    rho[a, b] = sum;
                    rho[b, a] = sum;
                }
            });

            var groups = options.Grouping == ReductionGrouping.VariableClustering
                ? VariableClustering(rho, options.VariableClusteringEigenvalue)
                : CompleteLinkage(rho, 1d - threshold);

            foreach (var group in groups.Where(g => g.Count > 1))
            {
                var representative = group
                    .OrderByDescending(a => group.Where(b => b != a).Average(b => Math.Abs(rho[a, b])))
                    .ThenBy(a => a)
                    .First();

                foreach (var a in group.Where(a => a != representative))
                {
                    var duplicate = IdenticalColumns(filled, alive[a], alive[representative]);
                    reasons[alive[a]] = duplicate ? "Duplicate" : "Correlated";

                    details[alive[a]] = duplicate
                        ? $"identical to {name(alive[representative])}"
                        : $"|rho| {Math.Abs(rho[a, representative]):0.####} with kept representative {name(alive[representative])}";
                }
            }
        }

        private static bool IdenticalColumns(double[][] data, int a, int b)
        {
            for (var i = 0; i < data.Length; i++)
            {
                if (Math.Abs(data[i][a] - data[i][b]) > 0.0001)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<List<int>> CompleteLinkage(double[,] rho, double cutHeight)
        {
            var m = rho.GetLength(0);
            var distance = new double[m, m];

            for (var a = 0; a < m; a++)
            {
                for (var b = 0; b < m; b++)
                {
                    distance[a, b] = a == b ? 0d : 1d - Math.Abs(rho[a, b]);
                }
            }

            var linkage = (double[,])distance.Clone();
            var active = Enumerable.Range(0, m).ToList();
            var members = Enumerable.Range(0, m).ToDictionary(i => i, i => new List<int> { i });

            while (active.Count > 1)
            {
                var best = double.PositiveInfinity;
                var bestA = -1;
                var bestB = -1;

                for (var ai = 0; ai < active.Count; ai++)
                {
                    for (var bi = ai + 1; bi < active.Count; bi++)
                    {
                        var l = linkage[active[ai], active[bi]];

                        if (l < best)
                        {
                            best = l;
                            bestA = active[ai];
                            bestB = active[bi];
                        }
                    }
                }

                if (best > cutHeight + 1e-12)
                {
                    break;
                }

                foreach (var other in active)
                {
                    if (other == bestA || other == bestB)
                    {
                        continue;
                    }

                    var merged = Math.Max(linkage[bestA, other], linkage[bestB, other]);
                    linkage[bestA, other] = merged;
                    linkage[other, bestA] = merged;
                }

                members[bestA].AddRange(members[bestB]);
                members.Remove(bestB);
                active.Remove(bestB);
            }

            return active.Select(a => members[a].OrderBy(v => v).ToList()).ToList();
        }

        private static List<List<int>> VariableClustering(double[,] rho, double eigenvalueThreshold)
        {
            var m = rho.GetLength(0);
            var clusters = new List<List<int>> { Enumerable.Range(0, m).ToList() };
            var done = false;
            var guard = 0;

            while (!done && guard++ < m)
            {
                done = true;
                var next = new List<List<int>>();

                foreach (var cluster in clusters)
                {
                    if (cluster.Count < 2)
                    {
                        next.Add(cluster);
                        continue;
                    }

                    var sub = new double[cluster.Count, cluster.Count];

                    for (var a = 0; a < cluster.Count; a++)
                    {
                        for (var b = 0; b < cluster.Count; b++)
                        {
                            sub[a, b] = rho[cluster[a], cluster[b]];
                        }
                    }

                    var (values, _) = DenseLinearAlgebra.SymmetricEigen(sub);

                    if (values[1] <= eigenvalueThreshold)
                    {
                        next.Add(cluster);
                        continue;
                    }

                    var seedA = 0;
                    var seedB = 1;
                    var weakest = double.PositiveInfinity;

                    for (var a = 0; a < cluster.Count; a++)
                    {
                        for (var b = a + 1; b < cluster.Count; b++)
                        {
                            var r = Math.Abs(sub[a, b]);

                            if (r < weakest)
                            {
                                weakest = r;
                                seedA = a;
                                seedB = b;
                            }
                        }
                    }

                    var first = new List<int>();
                    var second = new List<int>();

                    for (var a = 0; a < cluster.Count; a++)
                    {
                        if (a == seedA || (a != seedB && Math.Abs(sub[a, seedA]) >= Math.Abs(sub[a, seedB])))
                        {
                            first.Add(cluster[a]);
                        }
                        else
                        {
                            second.Add(cluster[a]);
                        }
                    }

                    if (first.Count == 0 || second.Count == 0)
                    {
                        next.Add(cluster);
                        continue;
                    }

                    for (var iteration = 0; iteration < 10; iteration++)
                    {
                        var pair = new[] { first, second };
                        var r2 = new double[2][];

                        for (var g = 0; g < 2; g++)
                        {
                            r2[g] = cluster.Select(v => ExplainedByFirstComponent(rho, pair[g], v)).ToArray();
                        }

                        var newFirst = new List<int>();
                        var newSecond = new List<int>();

                        for (var a = 0; a < cluster.Count; a++)
                        {
                            if (r2[0][a] >= r2[1][a])
                            {
                                newFirst.Add(cluster[a]);
                            }
                            else
                            {
                                newSecond.Add(cluster[a]);
                            }
                        }

                        if (newFirst.Count == 0 || newSecond.Count == 0)
                        {
                            break;
                        }

                        var moved = !newFirst.SequenceEqual(first);
                        first = newFirst;
                        second = newSecond;

                        if (!moved)
                        {
                            break;
                        }
                    }

                    next.Add(first);
                    next.Add(second);
                    done = false;
                }

                clusters = next;
            }

            return clusters.Select(c => c.OrderBy(v => v).ToList()).ToList();
        }

        private static double ExplainedByFirstComponent(double[,] rho, List<int> cluster, int variable)
        {
            if (cluster.Count == 1)
            {
                return cluster[0] == variable ? 1d : rho[variable, cluster[0]] * rho[variable, cluster[0]];
            }

            var sub = new double[cluster.Count, cluster.Count];

            for (var a = 0; a < cluster.Count; a++)
            {
                for (var b = 0; b < cluster.Count; b++)
                {
                    sub[a, b] = rho[cluster[a], cluster[b]];
                }
            }

            var (values, vectors) = DenseLinearAlgebra.SymmetricEigen(sub);
            var covariance = 0d;

            for (var a = 0; a < cluster.Count; a++)
            {
                covariance += vectors[0][a] * rho[variable, cluster[a]];
            }

            return covariance * covariance / Math.Max(values[0], 1e-12);
        }
    }
}