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
using Jube.Dictionary.Fuzzy.Models;

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Dice bigram similarity from 0 to 1 on case-, accent- and punctuation-folded text.")]
        public static double DiceSimilarity(this string? @this,
            [Description("The text the value is compared with.")] string? other)
        {
            return FuzzyScorers.Dice(FuzzyNormaliser.Normalise(@this, FuzzyNormalisation.Default),
                FuzzyNormaliser.Normalise(other, FuzzyNormalisation.Default));
        }
    }
}