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
using System.Net;
using System.Net.Sockets;

namespace Jube.Svirfd
{
    public static class SvirfdAddressScope
    {
        public static bool IsPubliclyRoutable(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                return false;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var bytes = address.GetAddressBytes();

                return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && !address.IsIPv6Multicast &&
                       (bytes[0] & 0xFE) != 0xFC;
            }

            var b = address.GetAddressBytes();

            return b[0] switch
            {
                0 or 10 or >= 224 => false,
                100 => b[1] is < 64 or > 127,
                169 => b[1] != 254,
                172 => b[1] is < 16 or > 31,
                192 => b[1] != 168,
                _ => true
            };
        }
    }
}
