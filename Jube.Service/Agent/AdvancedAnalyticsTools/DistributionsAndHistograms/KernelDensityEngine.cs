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
using Accord.Statistics.Distributions.DensityKernels;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal static class KernelDensityEngine
    {
        public static double Bandwidth(double[] values, BandwidthRule rule, double? manual)
        {
            var n = values.Length;

            if (rule == BandwidthRule.Manual)
            {
                if (!manual.HasValue || !(manual.Value > 0) || !double.IsFinite(manual.Value))
                {
                    throw new ArgumentException(
                        "bandwidth must be a positive number when bandwidthRule is Manual.", "bandwidth");
                }

                return manual.Value;
            }

            var sd = n > 1 ? values.StandardDeviation() : 0.0;

            double h;

            switch (rule)
            {
                case BandwidthRule.SilvermanRobust:
                    var scale = Math.Min(sd, HistogramEngine.InterquartileRange(values) / 1.34);

                    if (!(scale > 0))
                    {
                        scale = sd;
                    }

                    h = 0.9 * scale * Math.Pow(n, -0.2);
                    break;
                case BandwidthRule.Oversmoothed:
                    h = 1.144 * sd * Math.Pow(n, -0.2);
                    break;
                default:
                    h = n > 1 ? EmpiricalDistribution.SmoothingRule(values) : 0.0;
                    break;
            }

            if (!(h > 0) || !double.IsFinite(h))
            {
                throw new ArgumentException(
                    "The values have no spread (all identical or a single value), so the automatic bandwidth is 0. Give bandwidthRule Manual and a positive bandwidth.",
                    "values");
            }

            return h;
        }

        public static double[] Evaluate(double[] values, double[] points, KernelKind kernel, double bandwidth)
        {
            var result = new double[points.Length];
            var n = values.Length;

            if (kernel == KernelKind.Gaussian)
            {
                var distribution = new EmpiricalDistribution(values, bandwidth);

                for (var i = 0; i < points.Length; i++)
                {
                    result[i] = distribution.ProbabilityDensityFunction(points[i]);
                }

                return result;
            }

            IDensityKernel k = kernel == KernelKind.Epanechnikov
                ? new EpanechnikovKernel(1)
                : new UniformKernel();

            var buffer = new double[1];

            for (var i = 0; i < points.Length; i++)
            {
                var sum = 0.0;

                for (var j = 0; j < n; j++)
                {
                    buffer[0] = (points[i] - values[j]) / bandwidth;
                    sum += k.Function(buffer);
                }

                result[i] = sum / (n * bandwidth);
            }

            return result;
        }

        public static (double[] Locations, double[] Densities) Peaks(double[] x, double[] density)
        {
            var locations = new List<double>();
            var densities = new List<double>();

            for (var i = 0; i < x.Length; i++)
            {
                var left = i == 0 ? double.NegativeInfinity : density[i - 1];
                var right = i == x.Length - 1 ? double.NegativeInfinity : density[i + 1];

                if (density[i] > left && density[i] >= right && density[i] > 0)
                {
                    locations.Add(x[i]);
                    densities.Add(density[i]);
                }
            }

            return (locations.ToArray(), densities.ToArray());
        }
    }
}