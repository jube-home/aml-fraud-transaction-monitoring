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
    public enum SurvivalIntervalMethod
    {
        LogLog = 0,
        Log = 1,
        Plain = 2
    }

    public enum LogRankWeighting
    {
        LogRank = 0,
        GehanBreslow = 1,
        TaroneWare = 2,
        PetoPeto = 3,
        FlemingHarrington = 4
    }

    internal static class SurvivalMath
    {
        public static List<SurvivalStep> Steps(double[] times, int[] events)
        {
            var order = Enumerable.Range(0, times.Length).OrderBy(i => times[i]).ToArray();
            var result = new List<SurvivalStep>();
            var atRisk = times.Length;
            var survival = 1d;
            var greenwood = 0d;
            var hazard = 0d;
            var hazardVariance = 0d;
            var index = 0;

            while (index < order.Length)
            {
                var time = times[order[index]];
                var deaths = 0;
                var censored = 0;

                while (index < order.Length && Math.Abs(times[order[index]] - time) < 0.0001)
                {
                    if (events[order[index]] == 1)
                    {
                        deaths++;
                    }
                    else
                    {
                        censored++;
                    }

                    index++;
                }

                if (deaths > 0)
                {
                    survival *= 1d - deaths / (double)atRisk;

                    greenwood = atRisk > deaths
                        ? greenwood + deaths / ((double)atRisk * (atRisk - deaths))
                        : double.NaN;

                    hazard += deaths / (double)atRisk;
                    hazardVariance += deaths / ((double)atRisk * atRisk);
                }

                result.Add(new SurvivalStep
                {
                    Time = time,
                    AtRisk = atRisk,
                    Events = deaths,
                    Censored = censored,
                    Survival = survival,
                    GreenwoodSum = greenwood,
                    CumulativeHazard = hazard,
                    HazardVariance = hazardVariance
                });

                atRisk -= deaths + censored;
            }

            return result;
        }

        public static (double Lower, double Upper) Interval(double survival, double greenwood, double z,
            SurvivalIntervalMethod method)
        {
            if (double.IsNaN(greenwood))
            {
                return (double.NaN, double.NaN);
            }

            if (survival >= 1d)
            {
                return (1d, 1d);
            }

            if (survival <= 0d)
            {
                return (0d, 0d);
            }

            var root = Math.Sqrt(greenwood);

            switch (method)
            {
                case SurvivalIntervalMethod.Plain:
                    return (Math.Max(0d, survival - z * survival * root), Math.Min(1d, survival + z * survival * root));
                case SurvivalIntervalMethod.Log:
                    return (Math.Max(0d, survival * Math.Exp(-z * root)), Math.Min(1d, survival * Math.Exp(z * root)));
                default:
                {
                    if (greenwood == 0d)
                    {
                        return (survival, survival);
                    }

                    var exponent = z * root / Math.Abs(Math.Log(survival));

                    return (Math.Pow(survival, Math.Exp(exponent)), Math.Pow(survival, Math.Exp(-exponent)));
                }
            }
        }

        public static double Quantile(List<SurvivalStep> steps, double survivalLevel)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i].Survival <= survivalLevel + 1e-12)
                {
                    if (Math.Abs(steps[i].Survival - survivalLevel) <= 1e-12)
                    {
                        for (var j = i + 1; j < steps.Count; j++)
                        {
                            if (steps[j].Survival < survivalLevel - 1e-12)
                            {
                                return (steps[i].Time + steps[j].Time) / 2d;
                            }
                        }
                    }

                    return steps[i].Time;
                }
            }

            return double.NaN;
        }

        public static double BoundQuantile(List<SurvivalStep> steps, double survivalLevel, double z,
            SurvivalIntervalMethod method, bool lowerBound)
        {
            foreach (var step in steps)
            {
                if (step.Events == 0 && step.Survival >= 1d)
                {
                    continue;
                }

                var band = Interval(step.Survival, step.GreenwoodSum, z, method);

                if (double.IsNaN(band.Lower))
                {
                    continue;
                }

                if (lowerBound && band.Lower <= survivalLevel + 1e-12)
                {
                    return step.Time;
                }

                if (!lowerBound && band.Upper <= survivalLevel + 1e-12)
                {
                    return step.Time;
                }
            }

            return double.NaN;
        }

        public static double SurvivalAt(List<SurvivalStep> steps, double time)
        {
            var value = 1d;

            foreach (var step in steps)
            {
                if (step.Time <= time)
                {
                    value = step.Survival;
                }
                else
                {
                    break;
                }
            }

            return value;
        }

        public static SurvivalStep? StepAt(List<SurvivalStep> steps, double time)
        {
            SurvivalStep? found = null;

            foreach (var step in steps)
            {
                if (step.Time <= time)
                {
                    found = step;
                }
                else
                {
                    break;
                }
            }

            return found;
        }

        public static (double Area, double Variance) RestrictedMean(List<SurvivalStep> steps, double horizon)
        {
            var cumulative = new List<(SurvivalStep Step, double Area)>();
            var area = 0d;
            var previousTime = 0d;
            var previousSurvival = 1d;

            foreach (var step in steps)
            {
                if (step.Time > horizon)
                {
                    break;
                }

                area += previousSurvival * (step.Time - previousTime);
                previousTime = step.Time;
                previousSurvival = step.Survival;
                cumulative.Add((step, area));
            }

            var total = area + previousSurvival * (horizon - previousTime);
            var variance = 0d;

            foreach (var (step, areaToStep) in cumulative)
            {
                if (step.Events == 0 || step.AtRisk == step.Events)
                {
                    continue;
                }

                var tail = total - areaToStep;
                variance += tail * tail * step.Events / ((double)step.AtRisk * (step.AtRisk - step.Events));
            }

            return (total, variance);
        }

        public static LogRankTestResult LogRank(double[] times, int[] events, string[] groups,
            LogRankWeighting weighting,
            double fhP, double fhQ, double level, int skipped)
        {
            var names = groups.Distinct().OrderBy(g => g, StringComparer.Ordinal).ToArray();
            var k = names.Length;

            if (k < 2)
            {
                throw new ArgumentException("A log-rank test needs at least two distinct groups.", nameof(groups));
            }

            var index = names.Select((name, position) => (name, position)).ToDictionary(t => t.name, t => t.position);
            var atRisk = new double[k];
            var subjects = new int[k];

            for (var i = 0; i < groups.Length; i++)
            {
                atRisk[index[groups[i]]]++;
                subjects[index[groups[i]]]++;
            }

            var order = Enumerable.Range(0, times.Length).OrderBy(i => times[i]).ToArray();
            var observed = new double[k];
            var expected = new double[k];
            var score = new double[k];
            var covariance = new double[k, k];
            var pooledSurvival = 1d;
            var petoSurvival = 1d;
            var position = 0;

            while (position < order.Length)
            {
                var time = times[order[position]];
                var d = new double[k];
                var c = new double[k];

                while (position < order.Length && Math.Abs(times[order[position]] - time) < 0.0001)
                {
                    var g = index[groups[order[position]]];

                    if (events[order[position]] == 1)
                    {
                        d[g]++;
                    }
                    else
                    {
                        c[g]++;
                    }

                    position++;
                }

                var n = atRisk.Sum();
                var deaths = d.Sum();

                if (deaths > 0d)
                {
                    var previousSurvival = pooledSurvival;
                    pooledSurvival *= 1d - deaths / n;
                    petoSurvival *= 1d - deaths / (n + 1d);
                    double weight;

                    switch (weighting)
                    {
                        case LogRankWeighting.GehanBreslow:
                            weight = n;
                            break;
                        case LogRankWeighting.TaroneWare:
                            weight = Math.Sqrt(n);
                            break;
                        case LogRankWeighting.PetoPeto:
                            weight = petoSurvival;
                            break;
                        case LogRankWeighting.FlemingHarrington:
                            weight = Math.Pow(previousSurvival, fhP) * Math.Pow(1d - previousSurvival, fhQ);
                            break;
                        default:
                            weight = 1d;
                            break;
                    }

                    for (var a = 0; a < k; a++)
                    {
                        var e = deaths * atRisk[a] / n;
                        observed[a] += d[a];
                        expected[a] += e;
                        score[a] += weight * (d[a] - e);
                    }

                    if (n > 1d)
                    {
                        var factor = weight * weight * deaths * (n - deaths) / (n - 1d);

                        for (var a = 0; a < k; a++)
                        {
                            for (var b = 0; b < k; b++)
                            {
                                covariance[a, b] += factor * (atRisk[a] / n) * ((a == b ? 1d : 0d) - atRisk[b] / n);
                            }
                        }
                    }
                }

                for (var a = 0; a < k; a++)
                {
                    atRisk[a] -= d[a] + c[a];
                }
            }

            var df = k - 1;
            double chi;

            try
            {
                var sub = new double[df][];

                for (var a = 0; a < df; a++)
                {
                    sub[a] = new double[df];

                    for (var b = 0; b < df; b++)
                    {
                        sub[a][b] = covariance[a, b];
                    }
                }

                var head = score.Take(df).ToArray();
                var solved = CategoricalLibrary.SolveLinearSystem(sub, head);
                chi = head.Zip(solved, (x, y) => x * y).Sum();

                if (double.IsInfinity(chi) || chi < -1e-8)
                {
                    chi = double.NaN;
                }
                else
                {
                    chi = Math.Max(0d, chi);
                }
            }
            catch (Exception)
            {
                chi = double.NaN;
            }

            var hr = double.NaN;
            var hrLower = double.NaN;
            var hrUpper = double.NaN;

            if (k == 2 && weighting == LogRankWeighting.LogRank && covariance[0, 0] > 0d)
            {
                var logHr = (observed[0] - expected[0]) / covariance[0, 0];
                var se = 1d / Math.Sqrt(covariance[0, 0]);
                var z = CategoricalGuards.TwoSidedZ(level);
                hr = Math.Exp(logHr);
                hrLower = Math.Exp(logHr - z * se);
                hrUpper = Math.Exp(logHr + z * se);
            }

            var rows = new List<LogRankGroupRow>();

            for (var a = 0; a < k; a++)
            {
                rows.Add(new LogRankGroupRow
                {
                    Group = names[a],
                    Subjects = subjects[a],
                    Observed = (int)observed[a],
                    Expected = expected[a],
                    ObservedMinusExpected = observed[a] - expected[a],
                    ObservedOverExpected = expected[a] > 0d ? observed[a] / expected[a] : double.NaN,
                    WeightedObservedMinusExpected = score[a]
                });
            }

            return new LogRankTestResult
            {
                Weighting = weighting == LogRankWeighting.FlemingHarrington
                    ? $"FlemingHarrington(p={fhP}, q={fhQ})"
                    : weighting.ToString(),
                Groups = rows.ToArray(),
                ChiSquare = chi,
                DegreesOfFreedom = df,
                PValue = CategoricalLibrary.ChiSquareSurvival(chi, df),
                HazardRatioFirstVersusSecond = hr,
                HazardRatioLower = hrLower,
                HazardRatioUpper = hrUpper,
                SkippedRows = skipped
            };
        }
    }
}