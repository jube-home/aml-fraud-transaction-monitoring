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

using System.Diagnostics;
using Jube.Analytics.Anomaly.Models;

namespace Jube.Analytics.Anomaly
{
    public static class AnomalyAutoSelector
    {
        public const string RuleText =
            "Fixed candidates in fixed order. Guard: reject a detector that fails to fit, emits non-finite or nearly constant scores, or whose contamination threshold flags more than 3 times the requested fraction because of ties. " +
            "With trusted positives (a classification filter): fit on half of the training rows, score the other half (assumed normal, label 0) together with the positives (label 1), and select the highest ROC AUC, average precision breaking ties. " +
            "Without labels: rank every surviving candidate on bootstrap score stability (mean Spearman correlation across resamples), random-subspace stability, excess-mass area (higher better) and mass-volume area (lower better, Goix 2016, on random 5-variable subspaces with uniform Monte Carlo volume), and select the lowest mean rank, ties broken by candidate order. " +
            "Never touches the supervised test split. Time budget: candidates not reached before the budget expires are recorded as skipped.";

        public static AutoResult Select(AutoRequest request, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            AnomalyInput.Validate(request.Data);

            if (request.Candidates.Count == 0)
            {
                throw new ArgumentException("There are no candidates to evaluate.");
            }

            var clock = Stopwatch.StartNew();

            var rows = AnomalyInput.Subsample(request.Data, request.EvaluationRowCap,
                AnomalyInput.DeriveSeed(request.Seed, 501));

            var d = rows[0].Length;
            var labelled = request.LabelledPositives is { Length: > 0 } && rows.Length >= 20;
            var evaluations = new List<Evaluation>();

            foreach (var candidate in request.Candidates)
            {
                token.ThrowIfCancellationRequested();
                var remaining = request.TimeBudgetSeconds - clock.Elapsed.TotalSeconds;
                var evaluation = new Evaluation(candidate);
                evaluations.Add(evaluation);

                if (remaining <= 0)
                {
                    evaluation.Status = "SkippedTimeBudget";
                    evaluation.Reason = "The time budget was exhausted before this candidate was reached.";
                    continue;
                }

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1d, remaining)));
                var candidateClock = Stopwatch.StartNew();

                try
                {
                    Evaluate(request, candidate, rows, d, labelled, evaluation, timeout.Token);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    evaluation.Status = "TimedOut";
                    evaluation.Reason = "The candidate exceeded the remaining time budget.";
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    evaluation.Status = "Rejected";
                    evaluation.Reason = $"{ex.GetType().Name}: {ex.Message}";
                }

                evaluation.ElapsedMilliseconds = candidateClock.ElapsedMilliseconds;
            }

            var survivors = evaluations.Where(e => e.Status == "Evaluated").ToList();
            Evaluation? winner = null;

            if (survivors.Count > 0)
            {
                if (labelled)
                {
                    foreach (var e in survivors)
                    {
                        e.MeanRank = 0d;
                    }

                    winner = survivors.OrderByDescending(e => e.Auc ?? double.NegativeInfinity)
                        .ThenByDescending(e => e.AveragePrecision ?? double.NegativeInfinity)
                        .ThenBy(evaluations.IndexOf).First();
                }
                else
                {
                    Rank(survivors, e => e.BootstrapStability, true);
                    Rank(survivors, e => e.SubspaceStability, true);
                    Rank(survivors, e => e.ExcessMass, true);
                    Rank(survivors, e => e.MassVolume, false);

                    foreach (var e in survivors)
                    {
                        e.MeanRank = e.RankSum / 4d;
                    }

                    winner = survivors.OrderBy(e => e.MeanRank).ThenBy(e => evaluations.IndexOf(e)).First();
                }
            }

            var result = evaluations.Select(e => new CandidateEvaluation(e.Candidate.Name, e.Status, e.Reason, e.Auc,
                e.AveragePrecision, e.BootstrapStability, e.SubspaceStability, e.ExcessMass, e.MassVolume, e.MeanRank,
                ReferenceEquals(e, winner), e.ElapsedMilliseconds)).ToList();

            var summary = new AutoEvaluation(RuleText, labelled, labelled ? request.LabelledPositives!.Length : 0,
                request.TimeBudgetSeconds, rows.Length, d, result);

            return new AutoResult(winner?.Candidate.Algorithm, summary);
        }

        public static (double ExcessMass, double MassVolume) Curves(double[] testScores, double[] uniformScores)
        {
            var ordered = testScores.OrderByDescending(v => v).ToArray();
            var uniform = uniformScores.OrderByDescending(v => v).ToArray();
            var levels = 200;
            var mass = new double[levels];
            var volume = new double[levels];

            for (var l = 0; l < levels; l++)
            {
                var index = (int)Math.Min(ordered.Length - 1, (long)l * ordered.Length / levels);
                var u = ordered[index];
                mass[l] = (index + 1d) / ordered.Length;
                volume[l] = CountAtLeast(uniform, u) / (double)uniform.Length;
            }

            var tMax = 0d;

            for (var t = 0d; t <= 1000d; t = t == 0d ? 0.001 : t * 1.1)
            {
                var value = 0d;

                for (var l = 0; l < levels; l++)
                {
                    value = Math.Max(value, mass[l] - t * volume[l]);
                }

                tMax = t;

                if (value <= 0.9)
                {
                    break;
                }
            }

            var steps = 100;
            var emArea = 0d;

            for (var s = 0; s < steps; s++)
            {
                var t = tMax * (s + 0.5) / steps;
                var value = 0d;

                for (var l = 0; l < levels; l++)
                {
                    value = Math.Max(value, mass[l] - t * volume[l]);
                }

                emArea += value * tMax / steps;
            }

            var mvArea = 0d;
            var alphaSteps = 50;

            for (var s = 0; s < alphaSteps; s++)
            {
                var alpha = 0.9 + 0.099 * (s + 0.5) / alphaSteps;
                var best = 1d;

                for (var l = 0; l < levels; l++)
                {
                    if (mass[l] >= alpha)
                    {
                        best = Math.Min(best, volume[l]);
                    }
                }

                mvArea += best * 0.099 / alphaSteps;
            }

            return (emArea, mvArea);
        }

        private static void Rank(List<Evaluation> evaluations, Func<Evaluation, double?> value, bool higherIsBetter)
        {
            var ordered = evaluations.OrderBy(e =>
                value(e) is { } v && Numerics.IsFinite(v) ? higherIsBetter ? -v : v : double.PositiveInfinity).ToList();

            for (var i = 0; i < ordered.Count;)
            {
                var j = i;
                var key = value(ordered[i]);

                while (j + 1 < ordered.Count && Equals(value(ordered[j + 1]), key))
                {
                    j++;
                }

                var rank = (i + j) / 2d + 1d;

                for (var k = i; k <= j; k++)
                {
                    ordered[k].RankSum += rank;
                }

                i = j + 1;
            }
        }

        private static void Evaluate(AutoRequest request, AutoCandidate candidate, double[][] rows, int d,
            bool labelled,
            Evaluation evaluation, CancellationToken token)
        {
            var seed = AnomalyInput.DeriveSeed(request.Seed, 600 + (int)candidate.Algorithm);
            var detector = candidate.Factory();
            var rng = new Random(seed);

            if (labelled)
            {
                var order = Enumerable.Range(0, rows.Length).OrderBy(_ => rng.Next()).ToArray();
                var half = rows.Length / 2;
                var fit = order.Take(half).Select(i => rows[i]).ToArray();
                var heldOut = order.Skip(half).Select(i => rows[i]).ToArray();
                detector.Fit(fit, seed, token);
                var positives = request.LabelledPositives!;
                var scoredNormal = AnomalyInput.ScoreAll(detector, heldOut, token);
                var scoredPositive = AnomalyInput.ScoreAll(detector, positives, token);
                var scores = scoredNormal.Concat(scoredPositive).ToArray();
                var labels = new int[scores.Length];

                for (var i = scoredNormal.Length; i < labels.Length; i++)
                {
                    labels[i] = 1;
                }

                if (Guard(scores, request.Contamination) is { } rejection)
                {
                    evaluation.Status = "Rejected";
                    evaluation.Reason = rejection;

                    return;
                }

                evaluation.Auc = BinaryMetrics.RocAuc(scores, labels);
                evaluation.AveragePrecision = BinaryMetrics.AveragePrecision(scores, labels);
                evaluation.Status = "Evaluated";

                return;
            }

            var fitRows = rows.Take(Math.Max(2, rows.Length * 2 / 3)).ToArray();
            detector.Fit(fitRows, seed, token);
            var full = AnomalyInput.ScoreAll(detector, rows, token);

            if (Guard(full, request.Contamination) is { } reason)
            {
                evaluation.Status = "Rejected";
                evaluation.Reason = reason;

                return;
            }

            var testRows = rows.Skip(fitRows.Length).ToArray();
            var boot = new List<double[]>();

            for (var r = 0; r < request.Resamples; r++)
            {
                token.ThrowIfCancellationRequested();
                var sample = new double[fitRows.Length][];

                for (var i = 0; i < sample.Length; i++)
                {
                    sample[i] = fitRows[rng.Next(fitRows.Length)];
                }

                var member = candidate.Factory();
                member.Fit(sample, AnomalyInput.DeriveSeed(seed, r), token);
                boot.Add(AnomalyInput.ScoreAll(member, testRows, token));
            }

            evaluation.BootstrapStability = MeanSpearman(boot);
            var subspaceSize = Math.Max(2, (int)Math.Ceiling(0.75 * d));
            var sub = new List<double[]>();

            if (d >= 2)
            {
                for (var r = 0; r < request.Subspaces; r++)
                {
                    token.ThrowIfCancellationRequested();

                    var columns = Enumerable.Range(0, d).OrderBy(_ => rng.Next()).Take(Math.Min(subspaceSize, d))
                        .ToArray();

                    var member = candidate.Factory();
                    member.Fit(Project(fitRows, columns), AnomalyInput.DeriveSeed(seed, 50 + r), token);
                    sub.Add(AnomalyInput.ScoreAll(member, Project(testRows, columns), token));
                }

                evaluation.SubspaceStability = MeanSpearman(sub);
            }

            var (em, mv) = MassCriteria(candidate, fitRows, testRows, d, request.Subspaces, rng, seed, token);
            evaluation.ExcessMass = em;
            evaluation.MassVolume = mv;
            evaluation.Status = "Evaluated";
        }

        private static string? Guard(double[] scores, double contamination)
        {
            if (scores.Any(s => !Numerics.IsFinite(s)))
            {
                return "Non-finite scores were produced.";
            }

            var distinct = scores.Distinct().Count();

            if (distinct < Math.Max(5, scores.Length / 100))
            {
                return $"Only {distinct} distinct scores were produced.";
            }

            var flagged = AnomalyLabeller.Contamination(scores, Math.Min(0.5, contamination));

            if (flagged.Flagged > 3 * Math.Max(1, (int)Math.Round(contamination * scores.Length)))
            {
                return $"Ties make the threshold flag {flagged.Flagged} rows against {contamination:P1} requested.";
            }

            return null;
        }

        private static double[][] Project(double[][] rows, int[] columns)
        {
            return rows.Select(r => columns.Select(c => r[c]).ToArray()).ToArray();
        }

        private static double MeanSpearman(List<double[]> runs)
        {
            var ranks = runs.Select(Numerics.AverageRanks).ToList();
            var sum = 0d;
            var pairs = 0;

            for (var a = 0; a < ranks.Count; a++)
            {
                for (var b = a + 1; b < ranks.Count; b++)
                {
                    sum += Pearson(ranks[a], ranks[b]);
                    pairs++;
                }
            }

            return pairs == 0 ? double.NaN : sum / pairs;
        }

        private static double Pearson(double[] a, double[] b)
        {
            var ma = a.Average();
            var mb = b.Average();
            double sab = 0, saa = 0, sbb = 0;

            for (var i = 0; i < a.Length; i++)
            {
                sab += (a[i] - ma) * (b[i] - mb);
                saa += (a[i] - ma) * (a[i] - ma);
                sbb += (b[i] - mb) * (b[i] - mb);
            }

            return saa <= 0 || sbb <= 0 ? 0d : sab / Math.Sqrt(saa * sbb);
        }

        private static (double ExcessMass, double MassVolume) MassCriteria(AutoCandidate candidate, double[][] fitRows,
            double[][] testRows, int d, int subspaces, Random rng, int seed, CancellationToken token)
        {
            var size = Math.Min(5, d);
            var emSum = 0d;
            var mvSum = 0d;
            var runs = Math.Max(1, subspaces);

            for (var r = 0; r < runs; r++)
            {
                token.ThrowIfCancellationRequested();
                var columns = Enumerable.Range(0, d).OrderBy(_ => rng.Next()).Take(size).ToArray();
                var fit = Project(fitRows, columns);
                var test = Project(testRows, columns);
                var detector = candidate.Factory();
                detector.Fit(fit, AnomalyInput.DeriveSeed(seed, 900 + r), token);
                var testScores = AnomalyInput.ScoreAll(detector, test, token);
                var lower = new double[size];
                var upper = new double[size];

                for (var j = 0; j < size; j++)
                {
                    lower[j] = test.Min(row => row[j]);
                    upper[j] = test.Max(row => row[j]);
                }

                var uniform = new double[2000][];

                for (var i = 0; i < uniform.Length; i++)
                {
                    uniform[i] = new double[size];

                    for (var j = 0; j < size; j++)
                    {
                        uniform[i][j] = lower[j] + rng.NextDouble() * (upper[j] - lower[j]);
                    }
                }

                var uniformScores = AnomalyInput.ScoreAll(detector, uniform, token);
                var (em, mv) = Curves(testScores, uniformScores);
                emSum += em;
                mvSum += mv;
            }

            return (emSum / runs, mvSum / runs);
        }

        private static int CountAtLeast(double[] descending, double threshold)
        {
            var lo = 0;
            var hi = descending.Length;

            while (lo < hi)
            {
                var mid = (lo + hi) / 2;

                if (descending[mid] >= threshold)
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

        private sealed class Evaluation(AutoCandidate candidate)
        {
            public AutoCandidate Candidate { get; } = candidate;
            public string Status { get; set; } = "Pending";
            public string Reason { get; set; } = string.Empty;
            public double? Auc { get; set; }
            public double? AveragePrecision { get; set; }
            public double? BootstrapStability { get; set; }
            public double? SubspaceStability { get; set; }
            public double? ExcessMass { get; set; }
            public double? MassVolume { get; set; }
            public double? MeanRank { get; set; }
            public double RankSum { get; set; }
            public long ElapsedMilliseconds { get; set; }
        }
    }
}