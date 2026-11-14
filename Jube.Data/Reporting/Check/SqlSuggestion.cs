/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Jube.Data.Reporting.Check
{
    internal static class SqlSuggestion
    {
        public static string Nearest(string name, IEnumerable<string> candidates)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var allowed = Math.Min(3, Math.Max(1, name.Length / 3));
            string best = null;
            var bestDistance = int.MaxValue;

            foreach (var candidate in candidates)
            {
                if (string.Equals(candidate, name, StringComparison.Ordinal))
                {
                    continue;
                }

                var distance = Distance(name.ToLowerInvariant(), candidate.ToLowerInvariant());

                if (distance > allowed || distance >= bestDistance)
                {
                    continue;
                }

                best = candidate;
                bestDistance = distance;
            }

            return best;
        }

        private static int Distance(string a, string b)
        {
            if (Math.Abs(a.Length - b.Length) > 4)
            {
                return int.MaxValue;
            }

            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];

            for (var j = 0; j <= b.Length; j++)
            {
                previous[j] = j;
            }

            for (var i = 1; i <= a.Length; i++)
            {
                current[0] = i;

                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }

                (previous, current) = (current, previous);
            }

            return previous[b.Length];
        }
    }
}
