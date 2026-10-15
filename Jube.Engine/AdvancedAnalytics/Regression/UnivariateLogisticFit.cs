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

using System;
using System.Linq;

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    internal readonly record struct UnivariateLogisticFit(
        bool Success,
        double Intercept,
        double Slope,
        double SlopeStandardError);

    internal static class UnivariateLogistic
    {
        public static UnivariateLogisticFit Fit(double[] x, int[] y, double[] weights = null,
            int maximumIterations = 100, double tolerance = 1e-10, bool standardise = true)
        {
            var n = x.Length;
            var mean = 0d;
            var scale = 1d;

            if (standardise)
            {
                mean = x.Average();
                var variance = x.Sum(v => (v - mean) * (v - mean)) / n;
                scale = variance > 0 ? Math.Sqrt(variance) : 1d;
            }

            var z = new double[n];

            for (var i = 0; i < n; i++)
            {
                z[i] = (x[i] - mean) / scale;
            }

            double a;
            var b = 0d;
            var totalWeight = weights?.Sum() ?? n;
            var positives = 0d;

            for (var i = 0; i < n; i++)
            {
                positives += (weights?[i] ?? 1d) * y[i];
            }

            if (positives <= 0 || positives >= totalWeight)
            {
                return default;
            }

            a = Math.Log(positives / (totalWeight - positives));

            var current = Objective(z, y, weights, a, b);
            var converged = false;
            double haa, hab, hbb;

            for (var iteration = 0; iteration < maximumIterations; iteration++)
            {
                double ga = 0, gb = 0;
                haa = 1e-12;
                hab = 0;
                hbb = 1e-12;

                for (var i = 0; i < n; i++)
                {
                    var w = weights?[i] ?? 1d;
                    var p = LogisticRegressionPipeline.Sigmoid(a + b * z[i]);
                    var d = w * (p - y[i]);
                    var h = w * p * (1d - p);
                    ga += d;
                    gb += d * z[i];
                    haa += h;
                    hab += h * z[i];
                    hbb += h * z[i] * z[i];
                }

                var determinant = haa * hbb - hab * hab;

                if (!(determinant > 0))
                {
                    return default;
                }

                var da = (hbb * ga - hab * gb) / determinant;
                var db = (-hab * ga + haa * gb) / determinant;
                var step = 1d;
                var accepted = false;

                while (step > 1e-12)
                {
                    var next = Objective(z, y, weights, a - step * da, b - step * db);

                    if (next <= current + 1e-12 * Math.Abs(current))
                    {
                        a -= step * da;
                        b -= step * db;
                        current = next;
                        accepted = true;
                        break;
                    }

                    step /= 2d;
                }

                if (!accepted)
                {
                    converged = Math.Max(Math.Abs(ga), Math.Abs(gb)) / totalWeight < 1e-8;
                    break;
                }

                if (Math.Max(Math.Abs(da), Math.Abs(db)) * step < tolerance)
                {
                    converged = true;
                    break;
                }
            }

            if (!converged)
            {
                return default;
            }

            haa = 1e-12;
            hab = 0;
            hbb = 1e-12;

            for (var i = 0; i < n; i++)
            {
                var w = weights?[i] ?? 1d;
                var p = LogisticRegressionPipeline.Sigmoid(a + b * z[i]);
                var h = w * p * (1d - p);
                haa += h;
                hab += h * z[i];
                hbb += h * z[i] * z[i];
            }

            var det = haa * hbb - hab * hab;
            var varianceOfSlope = haa / det;

            if (!(varianceOfSlope > 0) || !double.IsFinite(a) || !double.IsFinite(b))
            {
                return default;
            }

            var slope = b / scale;
            var intercept = a - b * mean / scale;

            return new UnivariateLogisticFit(true, intercept, slope, Math.Sqrt(varianceOfSlope) / scale);
        }

        private static double Objective(double[] z, int[] y, double[] weights, double a, double b)
        {
            var sum = 0d;

            for (var i = 0; i < z.Length; i++)
            {
                var eta = a + b * z[i];
                var softplus = eta > 0 ? eta + Math.Log(1d + Math.Exp(-eta)) : Math.Log(1d + Math.Exp(eta));
                sum += (weights?[i] ?? 1d) * (softplus - y[i] * eta);
            }

            return sum;
        }
    }
}