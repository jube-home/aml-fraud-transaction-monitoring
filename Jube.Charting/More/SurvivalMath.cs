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
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public static class SurvivalMath
    {
        public static SurvivalCurve KaplanMeier(IReadOnlyList<double> times, IReadOnlyList<bool> events,
            double confidence = 0.95, ConfidenceMethod method = ConfidenceMethod.LogLog)
        {
            if (times.Count != events.Count)
            {
                throw new ChartSpecificationException("Survival times and event flags must have the same length.");
            }

            if (!(confidence is > 0 and < 1))
            {
                throw new ChartSpecificationException("The confidence level must be strictly between 0 and 1.");
            }

            ChartLimits.Observations(times.Count);
            var pairs = new List<(double Time, bool Event)>(times.Count);

            for (var i = 0; i < times.Count; i++)
            {
                if (double.IsFinite(times[i]))
                {
                    if (times[i] < 0)
                    {
                        throw new ChartSpecificationException("Survival times must not be negative.");
                    }

                    pairs.Add((times[i], events[i]));
                }
            }

            pairs.Sort((a, b) => a.Time.CompareTo(b.Time));
            var z = ChartStats.NormalQuantile(0.5 + confidence / 2);
            var steps = new List<SurvivalStep> { new(0, 1, 1, 1, 0, pairs.Count, 0, 0) };
            var censorTimes = new List<double>();
            var survival = 1.0;
            var greenwood = 0.0;
            var atRisk = pairs.Count;
            var eventTotal = 0;
            var i2 = 0;
            var dead = false;

            while (i2 < pairs.Count)
            {
                var t = pairs[i2].Time;
                var d = 0;
                var c = 0;
                var j = i2;

                while (j < pairs.Count && Math.Abs(pairs[j].Time - t) < 0.0001)
                {
                    if (pairs[j].Event)
                    {
                        d++;
                    }
                    else
                    {
                        c++;
                        censorTimes.Add(t);
                    }

                    j++;
                }

                if (d > 0)
                {
                    eventTotal += d;
                    survival *= 1.0 - (double)d / atRisk;

                    if (atRisk > d && !dead)
                    {
                        greenwood += d / ((double)atRisk * (atRisk - d));
                    }
                    else
                    {
                        dead = true;
                    }

                    var (lo, hi, se) = Interval(survival, greenwood, dead, z, method);
                    steps.Add(new SurvivalStep(t, survival, lo, hi, se, atRisk, d, c));
                }

                atRisk -= d + c;
                i2 = j;
            }

            return new SurvivalCurve(steps, censorTimes, [.. pairs.Select(p => p.Time)], pairs.Count, eventTotal,
                confidence, method);
        }

        private static (double Lower, double Upper, double StandardError) Interval(double s, double greenwood,
            bool dead,
            double z, ConfidenceMethod method)
        {
            if (s <= 0 || dead)
            {
                return (0, 0, double.NaN);
            }

            var seLog = Math.Sqrt(greenwood);
            var se = s * seLog;

            switch (method)
            {
                case ConfidenceMethod.None:
                    return (s, s, se);
                case ConfidenceMethod.Linear:
                    return (Math.Max(0, s - z * se), Math.Min(1, s + z * se), se);
                case ConfidenceMethod.Log:
                    return (Math.Max(0, s * Math.Exp(-z * seLog)), Math.Min(1, s * Math.Exp(z * seLog)), se);
                default:
                    if (s >= 1)
                    {
                        return (1, 1, 0);
                    }

                    var sigma = seLog / Math.Abs(Math.Log(s));

                    return (Math.Pow(s, Math.Exp(z * sigma)), Math.Pow(s, Math.Exp(-z * sigma)), se);
            }
        }

        public static LogRankResult LogRank(IReadOnlyList<IReadOnlyList<double>> times,
            IReadOnlyList<IReadOnlyList<bool>> events)
        {
            var k = times.Count;

            if (k < 2)
            {
                throw new ChartSpecificationException("A log-rank test needs at least two groups.");
            }

            var all = new SortedSet<double>();
            var groupTimes = new List<double[]>();
            var groupEvents = new List<bool[]>();

            for (var g = 0; g < k; g++)
            {
                if (times[g].Count != events[g].Count)
                {
                    throw new ChartSpecificationException("Survival times and event flags must have the same length.");
                }

                var pairs = Enumerable.Range(0, times[g].Count).Where(i => double.IsFinite(times[g][i]))
                    .OrderBy(i => times[g][i]).ToArray();

                groupTimes.Add([.. pairs.Select(i => times[g][i])]);
                groupEvents.Add([.. pairs.Select(i => events[g][i])]);

                foreach (var i in pairs.Where(i => events[g][i]))
                {
                    all.Add(times[g][i]);
                }
            }

            var dim = k - 1;
            var oMinusE = new double[dim];
            var covariance = new double[dim, dim];
            var pointers = new int[k];

            foreach (var t in all)
            {
                var atRisk = new double[k];
                var deaths = new double[k];

                for (var g = 0; g < k; g++)
                {
                    while (pointers[g] < groupTimes[g].Length && groupTimes[g][pointers[g]] < t)
                    {
                        pointers[g]++;
                    }

                    atRisk[g] = groupTimes[g].Length - pointers[g];
                    var p = pointers[g];

                    while (p < groupTimes[g].Length && Math.Abs(groupTimes[g][p] - t) < 0.0001)
                    {
                        if (groupEvents[g][p])
                        {
                            deaths[g]++;
                        }

                        p++;
                    }
                }

                var n = atRisk.Sum();
                var d = deaths.Sum();

                if (n < 2 || d == 0)
                {
                    continue;
                }

                for (var a = 0; a < dim; a++)
                {
                    oMinusE[a] += deaths[a] - d * atRisk[a] / n;

                    for (var b = 0; b < dim; b++)
                    {
                        var term = d * (atRisk[a] / n) * ((a == b ? 1 : 0) - atRisk[b] / n) * (n - d) / (n - 1);
                        covariance[a, b] += term;
                    }
                }
            }

            var statistic = QuadraticForm(oMinusE, covariance);

            return new LogRankResult(statistic, dim, ChartStats.ChiSquareSurvival(statistic, dim));
        }

        private static double QuadraticForm(double[] vector, double[,] matrix)
        {
            var n = vector.Length;
            var a = new double[n, n + 1];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    a[i, j] = matrix[i, j];
                }

                a[i, n] = vector[i];
            }

            for (var col = 0; col < n; col++)
            {
                var pivot = col;

                for (var r = col + 1; r < n; r++)
                {
                    if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col]))
                    {
                        pivot = r;
                    }
                }

                if (Math.Abs(a[pivot, col]) < 1e-12)
                {
                    return 0;
                }

                if (pivot != col)
                {
                    for (var j = 0; j <= n; j++)
                    {
                        (a[col, j], a[pivot, j]) = (a[pivot, j], a[col, j]);
                    }
                }

                for (var r = 0; r < n; r++)
                {
                    if (r == col)
                    {
                        continue;
                    }

                    var f = a[r, col] / a[col, col];

                    for (var j = col; j <= n; j++)
                    {
                        a[r, j] -= f * a[col, j];
                    }
                }
            }

            var sum = 0.0;

            for (var i = 0; i < n; i++)
            {
                sum += vector[i] * (a[i, n] / a[i, i]);
            }

            return sum;
        }
    }
}