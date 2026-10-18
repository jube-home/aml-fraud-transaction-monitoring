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
    internal static class MutualInformationCore
    {
        public static MutualInformationResult Estimate(double[] x, double[] y, int bins,
            MutualInformationBinning binning,
            bool biasCorrect, int dropped)
        {
            var n = x.Length;

            if (bins != 0 && (bins < 2 || bins > 200))
            {
                throw new ArgumentException("bins must be 0 (automatic) or between 2 and 200.", nameof(bins));
            }

            if (!Enum.IsDefined(typeof(MutualInformationBinning), binning))
            {
                throw new ArgumentException("Unknown binning. Use EqualWidth or EqualFrequency.", nameof(binning));
            }

            var k = bins == 0 ? Math.Max(2, Math.Min(20, (int)Math.Floor(Math.Sqrt(n / 5d)))) : bins;
            var bx = Assign(x, k, binning, out var kx);
            var by = Assign(y, k, binning, out var ky);
            var joint = new double[kx, ky];
            var px = new double[kx];
            var py = new double[ky];

            for (var i = 0; i < n; i++)
            {
                joint[bx[i], by[i]] += 1d / n;
                px[bx[i]] += 1d / n;
                py[by[i]] += 1d / n;
            }

            var nzx = px.Where(v => v > 0).ToArray();
            var nzy = py.Where(v => v > 0).ToArray();
            var nzj = joint.Cast<double>().Where(v => v > 0).ToArray();
            var hx = nzx.Entropy();
            var hy = nzy.Entropy();
            var hxy = nzj.Entropy();
            var mi = Math.Max(0, hx + hy - hxy);
            var g = 2 * n * mi;
            var df = (nzx.Length - 1d) * (nzy.Length - 1d);
            var reported = mi;

            if (biasCorrect)
            {
                reported = Math.Max(0, mi - (nzj.Length - nzx.Length - nzy.Length + 1d) / (2d * n));
            }

            return new MutualInformationResult
            {
                MutualInformationNats = reported,
                MutualInformationBits = reported / Math.Log(2),
                NormalisedMutualInformation = hx > 0 && hy > 0 ? reported / Math.Sqrt(hx * hy) : double.NaN,
                EntropyXBits = hx / Math.Log(2),
                EntropyYBits = hy / Math.Log(2),
                JointEntropyBits = hxy / Math.Log(2),
                BinsX = nzx.Length,
                BinsY = nzy.Length,
                PairCount = n,
                MissingPairsDropped = dropped,
                GStatistic = g,
                DegreesOfFreedom = df,
                PValue = df >= 1 ? RegressionStats.ChiSquareUpperTail(g, df) : double.NaN,
                BiasCorrected = biasCorrect
            };
        }

        private static int[] Assign(double[] values, int k, MutualInformationBinning binning, out int used)
        {
            var n = values.Length;
            var result = new int[n];
            var min = values.Min();
            var max = values.Max();

            if (!(max > min))
            {
                used = 1;

                return result;
            }

            if (binning == MutualInformationBinning.EqualWidth)
            {
                var width = (max - min) / k;

                for (var i = 0; i < n; i++)
                {
                    result[i] = Math.Min(k - 1, (int)Math.Floor((values[i] - min) / width));
                }
            }
            else
            {
                var ranks = values.Rank();

                for (var i = 0; i < n; i++)
                {
                    result[i] = Math.Min(k - 1, (int)Math.Floor((ranks[i] - 0.5) / n * k));
                }
            }

            used = k;

            return result;
        }
    }
}