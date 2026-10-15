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

namespace Jube.Charting.More.Models
{
    public sealed record RegressionFit(
        IReadOnlyList<double> Coefficients,
        double RSquared,
        double ResidualStandardError,
        int DegreesOfFreedom,
        // ReSharper disable once NotAccessedPositionalProperty.Global
        int Count,
        double Confidence,
        double CenterX,
        double ScaleX,
        double[,] InverseGram)
    {
        public double TCritical => DegreesOfFreedom > 0
            ? ChartStats.StudentTQuantile(0.5 + Confidence / 2, DegreesOfFreedom)
            : double.NaN;

        public double Predict(double x)
        {
            var u = (x - CenterX) / ScaleX;
            var value = 0.0;

            for (var i = Coefficients.Count - 1; i >= 0; i--)
            {
                value = value * u + Coefficients[i];
            }

            return value;
        }

        public double LeverageTerm(double x)
        {
            var u = (x - CenterX) / ScaleX;
            var n = Coefficients.Count;
            var basis = new double[n];
            basis[0] = 1;

            for (var i = 1; i < n; i++)
            {
                basis[i] = basis[i - 1] * u;
            }

            var sum = 0.0;

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    sum += basis[i] * InverseGram[i, j] * basis[j];
                }
            }

            return sum;
        }

        public IReadOnlyList<double> RawCoefficients()
        {
            var n = Coefficients.Count;
            var result = new double[n];
            var power = new double[] { 1 };

            for (var k = 0; k < n; k++)
            {
                for (var j = 0; j < power.Length; j++)
                {
                    result[j] += Coefficients[k] * power[j] / Math.Pow(ScaleX, k);
                }

                var next = new double[power.Length + 1];

                for (var j = 0; j < power.Length; j++)
                {
                    next[j + 1] += power[j];
                    next[j] -= CenterX * power[j];
                }

                power = next;
            }

            return result;
        }

        public (double Lower, double Upper) MeanInterval(double x)
        {
            var half = TCritical * ResidualStandardError * Math.Sqrt(LeverageTerm(x));
            var y = Predict(x);

            return (y - half, y + half);
        }

        public (double Lower, double Upper) PredictionInterval(double x)
        {
            var half = TCritical * ResidualStandardError * Math.Sqrt(1 + LeverageTerm(x));
            var y = Predict(x);

            return (y - half, y + half);
        }
    }
}