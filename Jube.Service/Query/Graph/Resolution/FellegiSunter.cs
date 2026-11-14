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

namespace Jube.Service.Query.Graph.Resolution
{
    public static class FellegiSunter
    {
        public const double MinimumProbability = 1e-9;

        public static double AgreementWeight(double m, double u)
        {
            return Math.Log2(Clamp(m) / Clamp(u));
        }

        public static double DisagreementWeight(double m, double u)
        {
            return Math.Log2((1 - Clamp(m)) / (1 - Clamp(u)));
        }

        public static double TermFrequencyU(double valueFrequency)
        {
            return Clamp(valueFrequency);
        }

        public static double Posterior(double prior, IEnumerable<double> weights)
        {
            var logit = Math.Log2(Clamp(prior) / (1 - Clamp(prior))) + weights.Sum();

            if (logit > 60)
            {
                return 1;
            }

            if (logit < -60)
            {
                return 0;
            }

            return 1 / (1 + Math.Pow(2, -logit));
        }

        private static double Clamp(double probability)
        {
            return Math.Clamp(probability, MinimumProbability, 1 - MinimumProbability);
        }
    }
}
