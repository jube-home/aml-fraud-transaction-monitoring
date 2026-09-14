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
        public static double SimilarityTo(this string? @this, string? other)
        {
            var source = @this ?? "";
            var target = other ?? "";

            if (source.Length == 0 && target.Length == 0)
            {
                return 1.0;
            }

            var distance = source.LevenshteinDistance(target);
            var maxLength = Math.Max(source.Length, target.Length);

            return 1.0 - (double)distance / maxLength;
        }
    }
}
