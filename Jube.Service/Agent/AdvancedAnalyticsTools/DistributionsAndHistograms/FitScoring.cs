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

using Accord.Statistics;
using Accord.Statistics.Distributions;
using Accord.Statistics.Testing;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal static class FitScoring
    {
        private const int TimeoutMilliseconds = 5000;

        public static double? RunWithTimeout(Func<double> f)
        {
            try
            {
                var result = double.NaN;

                var task = Task.Run(() =>
                {
                    try
                    {
                        result = f();
                    }
                    catch (Exception)
                    {
                        result = double.NaN;
                    }
                });

#pragma warning disable VSTHRD002

                if (!task.Wait(TimeoutMilliseconds))
#pragma warning restore VSTHRD002
                {
                    return null;
                }

                return double.IsFinite(result) ? result : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static (double? Statistic, double? PValue) KolmogorovSmirnov(double[] data, UnivariateModel model)
        {
            var p = double.NaN;

            var statistic = RunWithTimeout(() =>
            {
                var test = new KolmogorovSmirnovTest(data, (IDistribution<double>)model.Raw);
                p = test.PValue;

                return test.Statistic;
            });

            return (statistic, double.IsFinite(p) ? p : null);
        }

        public static double? AndersonDarling(double[] data, UnivariateModel model)
        {
            if (model.Discrete)
            {
                return null;
            }

            return RunWithTimeout(() =>
                new AndersonDarlingTest(data, (IUnivariateDistribution<double>)model.Raw).Statistic);
        }

        public static double? ChiSquare(double[] data, UnivariateModel model)
        {
            if (model.Discrete || data.Length < 20)
            {
                return null;
            }

            return RunWithTimeout(() =>
                new ChiSquareTest(data, (IUnivariateDistribution<double>)model.Raw).Statistic);
        }

        public static double[] Positions(int n, PlottingPosition kind)
        {
            var result = new double[n];

            for (var i = 1; i <= n; i++)
            {
                result[i - 1] = kind switch
                {
                    PlottingPosition.Blom => (i - 0.375) / (n + 0.25),
                    PlottingPosition.Weibull => i / (n + 1.0),
                    PlottingPosition.Benard => (i - 0.3) / (n + 0.4),
                    _ => (i - 0.5) / n
                };
            }

            return result;
        }

        public static int[] Ranks(double?[] values)
        {
            var order = Enumerable.Range(0, values.Length)
                .OrderBy(i => values[i].HasValue ? 0 : 1)
                .ThenBy(i => values[i] ?? 0.0)
                .ToArray();

            var ranks = new int[values.Length];

            for (var r = 0; r < order.Length; r++)
            {
                ranks[order[r]] = r + 1;
            }

            return ranks;
        }

        public static double Correlation(double[] x, double[] y)
        {
            if (x.Length < 2)
            {
                return double.NaN;
            }

            var sx = x.StandardDeviation();
            var sy = y.StandardDeviation();

            if (sx == 0 || sy == 0)
            {
                return double.NaN;
            }

            return x.Covariance(y) / (sx * sy);
        }
    }
}