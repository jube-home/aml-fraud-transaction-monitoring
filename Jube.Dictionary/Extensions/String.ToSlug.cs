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

using Jube.Dictionary.Fuzzy;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        public static string ToSlug(this string? @this)
        {
            var folded = FuzzyNormaliser.FoldDiacritics(@this ?? "").ToLowerInvariant();
            var builder = new System.Text.StringBuilder(folded.Length);
            var pendingHyphen = false;
            foreach (var c in folded)
            {
                if (char.IsAsciiLetterOrDigit(c))
                {
                    if (pendingHyphen && builder.Length > 0)
                    {
                        builder.Append('-');
                    }

                    builder.Append(c);
                    pendingHyphen = false;
                }
                else
                {
                    pendingHyphen = true;
                }
            }

            return builder.ToString();
        }
    }
}