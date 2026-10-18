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
    internal static partial class FlowPredicates
    {
        internal static bool DoubleIsInsidePolygon(double v, double longitude, IEnumerable<string>? vertices)
        {
            return v.IsInsidePolygon(longitude, vertices);
        }

        internal static bool DoubleIsWithinAnyRadiusKm(double v, double longitude, IEnumerable<string>? centres,
            double radiusKm)
        {
            return v.IsWithinAnyRadiusKm(longitude, centres, radiusKm);
        }

        internal static bool StringIsDelimitedMember(string? v, string? delimitedList, string? separator)
        {
            return v.IsDelimitedMember(delimitedList, separator);
        }

        internal static bool StringIsInAnyCidr(string? v, IEnumerable<string>? cidrs)
        {
            return v.IsInAnyCidr(cidrs);
        }

        internal static bool StringIsCryptoWalletAddress(string? v)
        {
            return v.IsCryptoWalletAddress();
        }

        internal static bool StringJsonTextArrayAnyInList(string? v, string? path, IEnumerable<string>? list)
        {
            return v.JsonTextArrayAnyInList(path, list);
        }

        internal static bool StringNumberInTextWithinPercentOf(string? v, double target, double tolerancePercent)
        {
            return v.NumberInTextWithinPercentOf(target, tolerancePercent);
        }

        internal static bool DateTimeDayOfMonthIn(DateTime v, int[] days)
        {
            return days.Contains(v.Day);
        }
    }
}