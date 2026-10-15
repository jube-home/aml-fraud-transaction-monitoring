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

namespace Jube.Analytics.Calibration
{
    public readonly record struct BetaCalibrator(double A, double B, double C, double Epsilon)
    {
        public double Apply(double score)
        {
            if (double.IsNaN(score))
            {
                return double.NaN;
            }

            var bounded = Math.Min(Math.Max(score, Epsilon), 1d - Epsilon);
            var eta = A * Math.Log(bounded) + B * Math.Log(1d - bounded) + C;

            return Numerics.Sigmoid(eta);
        }

        public double[] Apply(double[] scores)
        {
            ArgumentNullException.ThrowIfNull(scores);
            var result = new double[scores.Length];

            for (var i = 0; i < scores.Length; i++)
            {
                result[i] = Apply(scores[i]);
            }

            return result;
        }

        public double[] ToPrimitives()
        {
            return [A, B, C, Epsilon];
        }

        public static BetaCalibrator FromPrimitives(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (values.Length != 4 || values.Any(v => !Numerics.IsFinite(v)))
            {
                throw new ArgumentException("A beta calibrator snapshot holds four finite values.");
            }

            if (values[3] <= 0 || values[3] >= 0.5)
            {
                throw new ArgumentException("The epsilon must be between 0 and 0.5.");
            }

            return new BetaCalibrator(values[0], values[1], values[2], values[3]);
        }
    }
}