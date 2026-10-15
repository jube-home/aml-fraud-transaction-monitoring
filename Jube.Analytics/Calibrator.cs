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

using Jube.Analytics.Models;

namespace Jube.Analytics
{
    public readonly record struct Calibrator(double Slope, double Intercept, CalibrationInput Mode, double Epsilon)
    {
        public bool IsIncreasing => Slope > 0;

        public double Apply(double score)
        {
            if (double.IsNaN(score))
            {
                return double.NaN;
            }

            return Numerics.Sigmoid(Slope * Input(score) + Intercept);
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

        public double Apply(double score, double trainingPrior, double targetRate)
        {
            return PriorShift.Apply(Apply(score), trainingPrior, targetRate);
        }

        public double[] Apply(double[] scores, double trainingPrior, double targetRate)
        {
            return PriorShift.Apply(Apply(scores), trainingPrior, targetRate);
        }

        public double[] ToPrimitives()
        {
            return [Slope, Intercept, (double)Mode, Epsilon];
        }

        public static Calibrator FromPrimitives(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (values.Length != 4 || values.Any(v => !Numerics.IsFinite(v)))
            {
                throw new ArgumentException("A calibrator snapshot holds four finite values.");
            }

            if (values[2] != 0 && Math.Abs(values[2] - 1) > 0.0001)
            {
                throw new ArgumentException("The calibration mode must be 0 or 1.");
            }

            if (values[3] <= 0 || values[3] >= 0.5)
            {
                throw new ArgumentException("The epsilon must be between 0 and 0.5.");
            }

            return new Calibrator(values[0], values[1], (CalibrationInput)(int)values[2], values[3]);
        }

        private double Input(double score)
        {
            if (Mode == CalibrationInput.RawScore)
            {
                return score;
            }

            var bounded = Math.Min(Math.Max(score, Epsilon), 1d - Epsilon);

            return Math.Log(bounded / (1d - bounded));
        }
    }
}