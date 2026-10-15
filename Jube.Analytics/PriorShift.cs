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

namespace Jube.Analytics
{
    public static class PriorShift
    {
        public static double Offset(double trainingPrior, double targetRate)
        {
            Validate(trainingPrior, nameof(trainingPrior));
            Validate(targetRate, nameof(targetRate));

            return Math.Log(targetRate / (1d - targetRate)) - Math.Log(trainingPrior / (1d - trainingPrior));
        }

        public static double Apply(double probability, double trainingPrior, double targetRate)
        {
            var offset = Offset(trainingPrior, targetRate);

            if (double.IsNaN(probability))
            {
                return double.NaN;
            }

            if (probability < 0 || probability > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(probability), "A probability must be between 0 and 1.");
            }

            var logit = Math.Log(probability) - Numerics.Log1P(-probability);

            return Numerics.Sigmoid(logit + offset);
        }

        public static double[] Apply(double[] probabilities, double trainingPrior, double targetRate)
        {
            ArgumentNullException.ThrowIfNull(probabilities);
            Offset(trainingPrior, targetRate);
            var result = new double[probabilities.Length];

            for (var i = 0; i < probabilities.Length; i++)
            {
                result[i] = Apply(probabilities[i], trainingPrior, targetRate);
            }

            return result;
        }

        private static void Validate(double prior, string name)
        {
            if (double.IsNaN(prior) || prior <= 0 || prior >= 1)
            {
                throw new ArgumentOutOfRangeException(name, "A prior or base rate must be strictly between 0 and 1.");
            }
        }
    }
}