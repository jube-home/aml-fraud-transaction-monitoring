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
    public sealed class IpRange
    {
        private readonly byte[] network;
        private readonly int prefix;

        private IpRange(byte[] network, int prefix)
        {
            this.network = network;
            this.prefix = prefix;
        }

        public static bool TryParse(string? text, out IpRange range)
        {
            range = null!;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var parts = text.Trim().Split('/');

            if (parts.Length > 2 || !IPAddress.TryParse(parts[0], out var address))
            {
                return false;
            }

            address = Normalise(address);

            var bits = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            var length = bits;

            if (parts.Length == 2 && (!int.TryParse(parts[1], out length) || length < 0 || length > bits))
            {
                return false;
            }

            range = new IpRange(address.GetAddressBytes(), length);

            return true;
        }

        public bool Contains(IPAddress address)
        {
            address = Normalise(address);
            var bytes = address.GetAddressBytes();

            if (bytes.Length != network.Length)
            {
                return false;
            }

            var full = prefix / 8;

            for (var i = 0; i < full; i++)
            {
                if (bytes[i] != network[i])
                {
                    return false;
                }
            }

            var remainder = prefix % 8;

            if (remainder == 0)
            {
                return true;
            }

            var mask = (byte)(0xFF << (8 - remainder));

            return (bytes[full] & mask) == (network[full] & mask);
        }

        private static IPAddress Normalise(IPAddress address)
        {
            return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        }
    }
}
