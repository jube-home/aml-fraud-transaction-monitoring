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
        public static bool IsValidCardExpiryAsOf(this string? @this, DateTime asOf)
        {
            var parts = (@this ?? "").Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var year) ||
                month is < 1 or > 12)
            {
                return false;
            }

            if (year < 100)
            {
                year += 2000;
            }

            if (year is < 1 or > 9998)
            {
                return false;
            }

            return new DateTime(year, month, 1).AddMonths(1).AddTicks(-1) >= asOf;
        }
    }
}