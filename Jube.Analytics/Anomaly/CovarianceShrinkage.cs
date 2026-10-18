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
    public static class CovarianceShrinkage
    {
        public static double[,] Apply(double[][] data, double[] mean, double[,] s, CovarianceRegularisation mode,
            double ridge)
        {
            var d = mean.Length;
            var trace = 0d;

            for (var i = 0; i < d; i++)
            {
                trace += s[i, i];
            }

            var mu = trace / d;
            var result = (double[,])s.Clone();

            switch (mode)
            {
                case CovarianceRegularisation.None:
                    return result;
                case CovarianceRegularisation.Ridge:
                    for (var i = 0; i < d; i++)
                    {
                        result[i, i] += ridge * Math.Max(mu, 1e-12);
                    }

                    return result;
                default:
                    var delta = LedoitWolfIntensity(data, mean, s, mu);

                    for (var i = 0; i < d; i++)
                    {
                        for (var j = 0; j < d; j++)
                        {
                            result[i, j] = (1d - delta) * s[i, j];
                        }

                        result[i, i] += delta * mu;
                    }

                    return result;
            }
        }

        public static double LedoitWolfIntensity(double[][] data, double[] mean, double[,] s, double mu)
        {
            var n = data.Length;
            var d = mean.Length;
            var frobeniusS = 0d;
            var d2 = 0d;

            for (var i = 0; i < d; i++)
            {
                for (var j = 0; j < d; j++)
                {
                    frobeniusS += s[i, j] * s[i, j];
                    var diff = s[i, j] - (i == j ? mu : 0d);
                    d2 += diff * diff;
                }
            }

            d2 /= d;
            var b2 = 0d;
            var x = new double[d];

            foreach (var row in data)
            {
                var norm = 0d;

                for (var j = 0; j < d; j++)
                {
                    x[j] = row[j] - mean[j];
                    norm += x[j] * x[j];
                }

                var quadratic = 0d;

                for (var i = 0; i < d; i++)
                {
                    var acc = 0d;

                    for (var j = 0; j < d; j++)
                    {
                        acc += s[i, j] * x[j];
                    }

                    quadratic += x[i] * acc;
                }

                b2 += norm * norm - 2d * quadratic + frobeniusS;
            }

            b2 /= (double)n * n * d;
            b2 = Math.Min(b2, d2);

            return d2 <= 0d ? 1d : b2 / d2;
        }
    }
}