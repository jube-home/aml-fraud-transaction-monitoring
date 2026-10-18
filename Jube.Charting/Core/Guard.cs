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

using Jube.Charting.Text;

namespace Jube.Charting.Core
{
    internal static class Guard
    {
        public static IReadOnlyList<string?> Colours(IEnumerable<string?> colours)
        {
            return [.. colours.Select(ColourCheck.Normalise)];
        }

        public static string Summary(string kind, IReadOnlyList<string> names, string detail)
        {
            var shown = names.Take(6).Select(n => TextMetrics.Truncate(n, 200, 12)).ToList();

            var list = string.Join(", ", shown) +
                       (names.Count > 6 ? " and " + (names.Count - 6) + " more" : string.Empty);

            var head = names.Count == 0
                ? kind + " with no series."
                : kind + " with " + names.Count +
                  " series: " + list + ".";

            return detail.Length == 0 ? head : head + " " + detail;
        }
    }
}