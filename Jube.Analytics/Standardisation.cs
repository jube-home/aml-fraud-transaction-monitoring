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
    public static class Standardisation
    {
        public static double ZScore(double value, double mean, double sd)
        {
            if (double.IsNaN(sd) || double.IsInfinity(sd) || sd == 0)
            {
                return 0;
            }

            var result = (value - mean) / sd;

            return double.IsNaN(result) || double.IsInfinity(result) ? 0 : result;
        }

        public static ZScoreScaler Fit(double[] values)
        {
            ArgumentNullException.ThrowIfNull(values);
            var finite = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToArray();

            if (finite.Length == 0)
            {
                throw new ArgumentException("There are no finite values to fit on.");
            }

            var mean = finite.Average();

            var sd = finite.Length < 2
                ? 0d
                : Math.Sqrt(finite.Sum(v => (v - mean) * (v - mean)) / (finite.Length - 1));

            return new ZScoreScaler(mean, sd);
        }

        public static double Resolve(double? value, double mean, double sd, bool standardise, out bool imputed)
        {
            imputed = value is null || double.IsNaN(value.Value) || double.IsInfinity(value.Value);
            var raw = imputed ? mean : value!.Value;

            return standardise ? ZScore(raw, mean, sd) : raw;
        }
    }
}