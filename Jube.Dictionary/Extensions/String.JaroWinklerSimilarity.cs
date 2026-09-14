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
        public static double JaroWinklerSimilarity(this string? @this, string? other)
        {
            var s1 = @this ?? "";
            var s2 = other ?? "";

            if (s1.Length == 0 && s2.Length == 0)
            {
                return 1.0;
            }

            if (s1.Length == 0 || s2.Length == 0)
            {
                return 0.0;
            }

            var matchDistance = Math.Max(0, Math.Max(s1.Length, s2.Length) / 2 - 1);

            var s1Matches = new bool[s1.Length];
            var s2Matches = new bool[s2.Length];

            var matches = 0;
            for (var i = 0; i < s1.Length; i++)
            {
                var start = Math.Max(0, i - matchDistance);
                var end = Math.Min(i + matchDistance + 1, s2.Length);

                for (var j = start; j < end; j++)
                {
                    if (s2Matches[j] || s1[i] != s2[j])
                    {
                        continue;
                    }

                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0)
            {
                return 0.0;
            }

            var transpositions = 0;
            var k = 0;
            for (var i = 0; i < s1.Length; i++)
            {
                if (!s1Matches[i])
                {
                    continue;
                }

                while (!s2Matches[k])
                {
                    k++;
                }

                if (s1[i] != s2[k])
                {
                    transpositions++;
                }

                k++;
            }

            transpositions /= 2;

            var jaro = ((double)matches / s1.Length +
                        (double)matches / s2.Length +
                        (double)(matches - transpositions) / matches) / 3.0;

            var maxPrefix = Math.Min(4, Math.Min(s1.Length, s2.Length));
            var prefixLength = 0;
            for (var i = 0; i < maxPrefix; i++)
            {
                if (s1[i] != s2[i])
                {
                    break;
                }

                prefixLength++;
            }

            const double scalingFactor = 0.1;

            return jaro + prefixLength * scalingFactor * (1 - jaro);
        }
    }
}
