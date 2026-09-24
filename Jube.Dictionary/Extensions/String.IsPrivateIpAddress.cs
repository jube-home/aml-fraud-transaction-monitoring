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
        public static bool IsPrivateIpAddress(this string? @this)
        {
            return NetworkAddressSupport.TryParseAddress(@this, out var address) &&
                   NetworkAddressSupport.InAny(address, "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16",
                       "100.64.0.0/10",
                       "fc00::/7");
        }
    }
}