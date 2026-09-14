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
        public static string CardNetwork(this string? @this)
        {
            var value = (@this ?? "").Replace(" ", "").Replace("-", "");

            if (value.Length < 2 || !value.All(char.IsDigit))
            {
                return "Unknown";
            }

            if (value.StartsWith('4'))
            {
                return "Visa";
            }

            var twoDigitPrefix = int.Parse(value[..2]);
            var threeDigitPrefix = value.Length >= 3 ? int.Parse(value[..3]) : -1;
            var fourDigitPrefix = value.Length >= 4 ? int.Parse(value[..4]) : -1;

            if (twoDigitPrefix is >= 51 and <= 55 || fourDigitPrefix is >= 2221 and <= 2720)
            {
                return "Mastercard";
            }

            if (twoDigitPrefix is 34 or 37)
            {
                return "AmericanExpress";
            }

            if (value.StartsWith("6011") || twoDigitPrefix == 65 || fourDigitPrefix is >= 6440 and <= 6499)
            {
                return "Discover";
            }

            if (threeDigitPrefix is >= 300 and <= 305 || twoDigitPrefix is 36 or 38 or 39)
            {
                return "DinersClub";
            }

            if (fourDigitPrefix is >= 3528 and <= 3589)
            {
                return "Jcb";
            }

            return "Unknown";
        }
    }
}
