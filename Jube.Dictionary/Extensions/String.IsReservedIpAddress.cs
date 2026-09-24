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
        public static bool IsReservedIpAddress(this string? @this)
        {
            return NetworkAddressSupport.TryParseAddress(@this, out var address) &&
                   NetworkAddressSupport.InAny(address, "0.0.0.0/8", "127.0.0.0/8", "169.254.0.0/16", "192.0.0.0/24",
                       "192.0.2.0/24", "198.18.0.0/15", "198.51.100.0/24", "203.0.113.0/24", "224.0.0.0/4",
                       "240.0.0.0/4",
                       "::/128", "::1/128", "fe80::/10", "ff00::/8", "2001:db8::/32");
        }
    }
}