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
        public static string ToRadixString(this long @this, int radix)
        {
            if (radix is < 2 or > 36)
            {
                return "";
            }

            if (@this == 0)
            {
                return "0";
            }

            const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            var negative = @this < 0;
            var magnitude = negative ? (ulong)(-(@this + 1)) + 1 : (ulong)@this;
            var builder = new System.Text.StringBuilder();
            while (magnitude > 0)
            {
                builder.Insert(0, digits[(int)(magnitude % (ulong)radix)]);
                magnitude /= (ulong)radix;
            }

            return negative ? "-" + builder : builder.ToString();
        }
    }
}