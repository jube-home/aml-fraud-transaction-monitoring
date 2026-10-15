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

using Accord.Math;
using Accord.Statistics;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal static class CorrelationMath
    {
        public const int MaximumKendallRows = 30_000;

        public static (double[] X, double[] Y, int Dropped) PairwiseComplete(double[]? x, double[]? y, string xName,
            string yName, bool ignoreMissing)
        {
            RegressionInput.RequireSameLength(x, y, xName, yName);
            var keptX = new List<double>(x!.Length);
            var keptY = new List<double>(x.Length);

            for (var i = 0; i < x.Length; i++)
            {
                if (double.IsInfinity(x[i]) || double.IsInfinity(y![i]))
                {
                    throw new ArgumentException(
                        $"Pair {i} contains an infinite value. Remove or cap infinite values first.", xName);
                }

                if (double.IsNaN(x[i]) || double.IsNaN(y[i]))
                {
                    if (!ignoreMissing)
                    {
                        throw new ArgumentException(
                            $"Pair {i} has a missing (NaN) value. Set ignoreMissing to true to drop incomplete pairs, or fill the gap first.",
                            xName);
                    }

                    continue;
                }

                keptX.Add(x[i]);
                keptY.Add(y[i]);
            }

            return (keptX.ToArray(), keptY.ToArray(), x.Length - keptX.Count);
        }

        public static void RequirePairs(int n, int minimum, string what)
        {
            if (n < minimum)
            {
                throw new ArgumentException(
                    $"Only {n} complete pair(s) remain but {what} needs at least {minimum}. Supply more observations.",
                    "x");
            }
        }

        public static double Pearson(double[] x, double[] y)
        {
            var scaleX = x.Max(Math.Abs);
            var scaleY = y.Max(Math.Abs);

            if (!(scaleX > 0) || !(scaleY > 0))
            {
                return double.NaN;
            }

            var sx = x.Select(v => v / scaleX).ToArray();
            var sy = y.Select(v => v / scaleY).ToArray();
            var sdx = sx.StandardDeviation();
            var sdy = sy.StandardDeviation();

            if (!(sdx > 1e-15) || !(sdy > 1e-15))
            {
                return double.NaN;
            }

            var r = sx.Covariance(sy) / (sdx * sdy);

            return Math.Max(-1, Math.Min(1, r));
        }

        public static double Spearman(double[] x, double[] y)
        {
            return Pearson(x.Rank(), y.Rank());
        }

        public static double TStatistic(double r, int n)
        {
            var denominator = 1 - r * r;

            return denominator <= 0
                ? r > 0 ? double.PositiveInfinity : double.NegativeInfinity
                : r * Math.Sqrt((n - 2d) / denominator);
        }

        public static double PValueFromT(double r, int n)
        {
            if (double.IsNaN(r))
            {
                return double.NaN;
            }

            return RegressionStats.TwoSidedTp(TStatistic(r, n), n - 2d);
        }

        public static (double Lower, double Upper) FisherInterval(double r, double standardError, double level)
        {
            if (double.IsNaN(r) || double.IsNaN(standardError) || double.IsInfinity(standardError))
            {
                return (double.NaN, double.NaN);
            }

            if (Math.Abs(r) >= 1)
            {
                return (r, r);
            }

            var z = Special.Atanh(r);
            var quantile = RegressionStats.NormalQuantile(0.5 + level / 2);

            return (Math.Tanh(z - quantile * standardError), Math.Tanh(z + quantile * standardError));
        }

        public static double SpearmanStandardError(double rho, int n)
        {
            return n > 3 ? Math.Sqrt((1 + rho * rho / 2) / (n - 3d)) : double.NaN;
        }

        public static (double Coefficient, double PValue) Coefficient(CorrelationMethod method, double[] x, double[] y)
        {
            switch (method)
            {
                case CorrelationMethod.Pearson:
                {
                    var r = Pearson(x, y);

                    return (r, PValueFromT(r, x.Length));
                }
                case CorrelationMethod.Spearman:
                {
                    var r = Spearman(x, y);

                    return (r, PValueFromT(r, x.Length));
                }
                case CorrelationMethod.Kendall:
                {
                    var counts = Kendall(x, y);
                    var tau = TauB(counts);

                    return (tau, KendallP(counts));
                }
                default:
                    throw new ArgumentException("Unknown method. Use Pearson, Spearman or Kendall.", "method");
            }
        }

        public static KendallCounts Kendall(double[] x, double[] y)
        {
            var n = x.Length;

            if (n > MaximumKendallRows)
            {
                throw new ArgumentException(
                    $"Kendall tau needs every pair of rows and is limited to {MaximumKendallRows} rows; {n} were supplied. Use Spearman for larger data or sample the rows.",
                    "x");
            }

            long concordant = 0;
            long discordant = 0;
            long tiedX = 0;
            long tiedY = 0;
            long tiedBoth = 0;

            for (var i = 0; i < n - 1; i++)
            {
                var xi = x[i];
                var yi = y[i];

                for (var j = i + 1; j < n; j++)
                {
                    var dx = xi.CompareTo(x[j]);
                    var dy = yi.CompareTo(y[j]);

                    if (dx == 0 && dy == 0)
                    {
                        tiedBoth++;
                    }
                    else if (dx == 0)
                    {
                        tiedX++;
                    }
                    else if (dy == 0)
                    {
                        tiedY++;
                    }
                    else if (dx == dy)
                    {
                        concordant++;
                    }
                    else
                    {
                        discordant++;
                    }
                }
            }

            var groupsX = TieGroups(x);
            var groupsY = TieGroups(y);
            double nn = n;
            var v0 = nn * (nn - 1) * (2 * nn + 5);
            var vt = groupsX.Sum(t => t * (t - 1d) * (2 * t + 5d));
            var vu = groupsY.Sum(t => t * (t - 1d) * (2 * t + 5d));
            var v1 = groupsX.Sum(t => t * (t - 1d)) * groupsY.Sum(t => t * (t - 1d));
            var v2 = groupsX.Sum(t => t * (t - 1d) * (t - 2d)) * groupsY.Sum(t => t * (t - 1d) * (t - 2d));
            var variance = (v0 - vt - vu) / 18d;

            if (n > 1)
            {
                variance += v1 / (2 * nn * (nn - 1));
            }

            if (n > 2)
            {
                variance += v2 / (9 * nn * (nn - 1) * (nn - 2));
            }

            return new KendallCounts
            {
                Concordant = concordant,
                Discordant = discordant,
                TiedX = tiedX,
                TiedY = tiedY,
                TiedBoth = tiedBoth,
                N = n,
                VarianceOfS = variance,
                DistinctX = groupsX.Count,
                DistinctY = groupsY.Count
            };
        }

        public static List<int> TieGroups(double[] values)
        {
            var sorted = values.OrderBy(v => v).ToArray();
            var groups = new List<int>();
            var run = 1;

            for (var i = 1; i <= sorted.Length; i++)
            {
                if (i < sorted.Length && sorted[i] == sorted[i - 1])
                {
                    run++;
                }
                else
                {
                    groups.Add(run);
                    run = 1;
                }
            }

            return groups;
        }

        public static double TauA(KendallCounts c)
        {
            return c.Pairs > 0 ? c.S / c.Pairs : double.NaN;
        }

        public static double TauB(KendallCounts c)
        {
            var n1 = c.TiedX + c.TiedBoth;
            var n2 = c.TiedY + c.TiedBoth;
            var denominator = Math.Sqrt((c.Pairs - n1) * (c.Pairs - n2));

            return denominator > 0 ? c.S / denominator : double.NaN;
        }

        public static double TauC(KendallCounts c)
        {
            var m = Math.Min(c.DistinctX, c.DistinctY);

            if (m < 2)
            {
                return double.NaN;
            }

            return 2 * c.S / ((double)c.N * c.N * (m - 1d) / m);
        }

        public static double KendallZ(KendallCounts c)
        {
            return c.VarianceOfS > 0 ? c.S / Math.Sqrt(c.VarianceOfS) : double.NaN;
        }

        public static double KendallP(KendallCounts c)
        {
            var z = KendallZ(c);

            return double.IsNaN(z) ? double.NaN : RegressionStats.NormalTwoSided(z);
        }

        public static string Describe(double coefficient, string method)
        {
            if (double.IsNaN(coefficient))
            {
                return $"{method} is undefined because at least one variable is constant (no variation).";
            }

            return
                $"{char.ToUpperInvariant(RegressionStats.DescribeStrength(coefficient)[0])}{RegressionStats.DescribeStrength(coefficient).Substring(1)} ({method} {coefficient:0.####}).";
        }
    }
}