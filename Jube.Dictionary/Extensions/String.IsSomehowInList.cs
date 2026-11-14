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
        extension(string? @this)
        {
            [Description("True when: fuzzily in the List token using the default profile: Jaro-Winkler or token sort, at least 0.92, after folding case, accents and punctuation.")]
            public bool IsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string>? list)
            {
                return FuzzyListMatcher.Any(@this, list, FuzzyOptions.Default);
            }

            [Description("True when: fuzzily in the List token using an options string.")]
            public bool IsSomehowInList([Description("The List token whose entries are compared.")] IEnumerable<string>? list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return FuzzyListMatcher.Any(@this, list, FuzzyOptions.Parse(options));
            }
        }
    }
}
