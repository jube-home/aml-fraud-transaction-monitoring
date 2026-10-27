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

namespace Jube.Case.Outbound
{
    public static class IpClassifier
    {
        public static string? BlockedCategory(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return ClassifyV4(address.GetAddressBytes());
            }

            if (address.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return "unsupportedFamily";
            }

            if (address.IsIPv4MappedToIPv6)
            {
                return ClassifyV4(address.MapToIPv4().GetAddressBytes()) ?? "mappedIPv4";
            }

            return ClassifyV6(address.GetAddressBytes());
        }

        public static bool IsBlocked(IPAddress address)
        {
            return BlockedCategory(address) != null;
        }

        private static string? ClassifyV4(byte[] a)
        {
            if (a[0] == 0)
            {
                return "unspecified";
            }

            if (a[0] == 10)
            {
                return "private";
            }

            if (a[0] == 100 && (a[1] & 0xC0) == 64)
            {
                return "sharedAddressSpace";
            }

            if (a[0] == 127)
            {
                return "loopback";
            }

            if (a[0] == 169 && a[1] == 254)
            {
                return "linkLocal";
            }

            if (a[0] == 172 && (a[1] & 0xF0) == 16)
            {
                return "private";
            }

            if (a[0] == 192 && a[1] == 168)
            {
                return "private";
            }

            if (a[0] == 192 && a[1] == 0 && (a[2] == 0 || a[2] == 2))
            {
                return "reserved";
            }

            if (a[0] == 192 && a[1] == 88 && a[2] == 99)
            {
                return "reserved";
            }

            if (a[0] == 198 && (a[1] & 0xFE) == 18)
            {
                return "benchmarking";
            }

            if ((a[0] == 198 && a[1] == 51 && a[2] == 100) || (a[0] == 203 && a[1] == 0 && a[2] == 113))
            {
                return "documentation";
            }

            if (a[0] >= 224)
            {
                return a[0] < 240 ? "multicast" : "reserved";
            }

            return null;
        }

        private static string? ClassifyV6(byte[] a)
        {
            if (a.Take(15).All(b => b == 0))
            {
                return a[15] == 1 ? "loopback" : "unspecified";
            }

            if (a.Take(12).All(b => b == 0))
            {
                return ClassifyV4(a[12..]) ?? "reserved";
            }

            if (a.Take(10).All(b => b == 0) && a[10] == 0xFF && a[11] == 0xFF)
            {
                return ClassifyV4(a[12..]) ?? "mappedIPv4";
            }

            if (a.Take(8).All(b => b == 0) && a[8] == 0xFF && a[9] == 0xFF && a[10] == 0 && a[11] == 0)
            {
                return ClassifyV4(a[12..]) ?? "translatedIPv4";
            }

            if (a[0] == 0x00 && a[1] == 0x64 && a[2] == 0xFF && a[3] == 0x9B)
            {
                return a[4] == 0 && a[5] == 0 && a[6] == 0 && a[7] == 0 && a[8] == 0 && a[9] == 0 && a[10] == 0
                       && a[11] == 0
                    ? ClassifyV4(a[12..]) ?? "nat64"
                    : "reserved";
            }

            if (a[0] == 0x20 && a[1] == 0x02)
            {
                return ClassifyV4(a[2..6]) ?? "sixToFour";
            }

            if (a[0] == 0x20 && a[1] == 0x01 && a[2] == 0x00 && a[3] == 0x00)
            {
                return "teredo";
            }

            if (a[0] == 0x20 && a[1] == 0x01 && a[2] == 0x0D && a[3] == 0xB8)
            {
                return "documentation";
            }

            if (a[0] == 0x01 && a[1] == 0x00 && a.Skip(2).Take(6).All(b => b == 0))
            {
                return "discard";
            }

            if ((a[0] & 0xFE) == 0xFC)
            {
                return "uniqueLocal";
            }

            if (a[0] == 0xFE && (a[1] & 0xC0) == 0x80)
            {
                return "linkLocal";
            }

            if (a[0] == 0xFE && (a[1] & 0xC0) == 0xC0)
            {
                return "siteLocal";
            }

            if (a[0] == 0xFF)
            {
                return "multicast";
            }

            return null;
        }
    }
}
