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

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static double ShannonEntropy(this string? @this)
        {
            if (string.IsNullOrEmpty(@this))
            {
                return 0.0;
            }

            var frequencies = new Dictionary<char, int>();
            foreach (var c in @this)
            {
                frequencies[c] = frequencies.GetValueOrDefault(c) + 1;
            }

            var length = (double)@this.Length;
            var entropy = 0.0;

            foreach (var count in frequencies.Values)
            {
                var probability = count / length;
                entropy -= probability * Math.Log2(probability);
            }

            return entropy;
        }
    }
}
