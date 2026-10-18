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

using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal static class LinearCoefficients
    {
        public static double?[]? StandardErrors(JToken model, int weightCount)
        {
            var raw = ModelJson.Doubles(model, "standardErrors");

            if (raw is null)
            {
                return null;
            }

            double[] usable;

            if (raw.Length == weightCount + 1)
            {
                usable = raw[1..];
            }
            else if (raw.Length == weightCount)
            {
                usable = raw;
            }
            else
            {
                return null;
            }

            if (!usable.Any(e => e > 0 && double.IsFinite(e)))
            {
                return null;
            }

            return usable.Select(e => e > 0 && double.IsFinite(e) ? (double?)e : null).ToArray();
        }
    }
}