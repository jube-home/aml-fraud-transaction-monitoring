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

using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    internal static class SurvivalModelMath
    {
        public static ConcordanceResult Concordance(double[] times, int[] events, double[] risk, int skipped)
        {
            double concordant = 0;
            double discordant = 0;
            double tied = 0;
            var n = times.Length;

            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    int shorter;
                    int longer;

                    if (times[i] < times[j])
                    {
                        if (events[i] != 1)
                        {
                            continue;
                        }

                        shorter = i;
                        longer = j;
                    }
                    else if (times[j] < times[i])
                    {
                        if (events[j] != 1)
                        {
                            continue;
                        }

                        shorter = j;
                        longer = i;
                    }
                    else
                    {
                        if (events[i] == events[j])
                        {
                            continue;
                        }

                        shorter = events[i] == 1 ? i : j;
                        longer = shorter == i ? j : i;
                    }

                    if (risk[shorter] > risk[longer])
                    {
                        concordant++;
                    }
                    else if (risk[shorter] < risk[longer])
                    {
                        discordant++;
                    }
                    else
                    {
                        tied++;
                    }
                }
            }

            var comparable = concordant + discordant + tied;
            var index = comparable > 0d ? (concordant + 0.5 * tied) / comparable : double.NaN;

            return new ConcordanceResult
            {
                ConcordanceIndex = index,
                SomersD = 2d * index - 1d,
                ConcordantPairs = concordant,
                DiscordantPairs = discordant,
                TiedRiskPairs = tied,
                ComparablePairs = comparable,
                Subjects = n,
                Events = events.Sum(),
                SkippedRows = skipped
            };
        }

        public static double[] DenseRanks(double[] times)
        {
            var distinct = times.Distinct().OrderBy(v => v).ToArray();
            var lookup = new Dictionary<double, double>();

            for (var i = 0; i < distinct.Length; i++)
            {
                lookup[distinct[i]] = i + 1d;
            }

            return times.Select(t => lookup[t]).ToArray();
        }

        public static (double[] Times, double[] Hazard) Breslow(double[] times, int[] events, double[] relativeRisk)
        {
            var order = Enumerable.Range(0, times.Length).OrderByDescending(i => times[i]).ToArray();
            var distinct = new List<double>();
            var deaths = new List<double>();
            var denominators = new List<double>();
            var running = 0d;
            var position = 0;

            while (position < order.Length)
            {
                var time = times[order[position]];
                var d = 0;

                while (position < order.Length && Math.Abs(times[order[position]] - time) < 0.0001)
                {
                    running += relativeRisk[order[position]];
                    d += events[order[position]];
                    position++;
                }

                if (d > 0)
                {
                    distinct.Add(time);
                    deaths.Add(d);
                    denominators.Add(running);
                }
            }

            distinct.Reverse();
            deaths.Reverse();
            denominators.Reverse();
            var cumulative = new double[distinct.Count];
            var total = 0d;

            for (var i = 0; i < distinct.Count; i++)
            {
                total += deaths[i] / denominators[i];
                cumulative[i] = total;
            }

            return (distinct.ToArray(), cumulative);
        }
    }
}