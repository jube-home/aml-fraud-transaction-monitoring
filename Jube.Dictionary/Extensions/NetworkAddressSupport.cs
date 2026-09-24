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

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace Jube.Dictionary.Extensions
{
    internal static class NetworkAddressSupport
    {
        internal static bool TryParseAddress(string? text, out IPAddress address)
        {
            address = IPAddress.None;
            var value = (text ?? "").Trim();
            if (value.Length is 0 or > 64 || value.Contains('%'))
            {
                return false;
            }

            if (!IPAddress.TryParse(value, out var parsed))
            {
                return false;
            }

            if (parsed.AddressFamily == AddressFamily.InterNetwork &&
                (value.Count(c => c == '.') != 3 ||
                 value.Split('.').Any(p => p.Length is 0 or > 3 || !p.All(char.IsAsciiDigit))))
            {
                return false;
            }

            address = parsed.IsIPv4MappedToIPv6 ? parsed.MapToIPv4() : parsed;
            return true;
        }

        internal static BigInteger ToNumber(IPAddress address)
        {
            return new BigInteger(address.GetAddressBytes(), true, true);
        }

        internal static IPAddress FromNumber(BigInteger number, AddressFamily family)
        {
            var length = family == AddressFamily.InterNetwork ? 4 : 16;
            var bytes = number.ToByteArray(true, true);
            var padded = new byte[length];
            Array.Copy(bytes, 0, padded, length - bytes.Length, bytes.Length);
            return new IPAddress(padded);
        }

        internal static bool TryParseCidr(string? text, out IPAddress network, out int prefix, out int bits)
        {
            network = IPAddress.None;
            prefix = 0;
            bits = 0;
            var value = (text ?? "").Trim();
            var slash = value.IndexOf('/');
            if (slash < 0)
            {
                return false;
            }

            if (!TryParseAddress(value.Substring(0, slash), out var address) ||
                !int.TryParse(value.Substring(slash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out prefix))
            {
                return false;
            }

            bits = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            if (prefix < 0 || prefix > bits)
            {
                return false;
            }

            var mask = Mask(prefix, bits);
            network = FromNumber(ToNumber(address) & mask, address.AddressFamily);
            return true;
        }

        internal static BigInteger Mask(int prefix, int bits)
        {
            var all = (BigInteger.One << bits) - 1;
            return all ^ ((BigInteger.One << (bits - prefix)) - 1);
        }

        internal static bool InRange(IPAddress address, string first, string last)
        {
            return TryParseAddress(first, out var low) && TryParseAddress(last, out var high) &&
                   low.AddressFamily == address.AddressFamily &&
                   ToNumber(address) >= ToNumber(low) && ToNumber(address) <= ToNumber(high);
        }

        internal static bool InAny(IPAddress address, params string[] cidrs)
        {
            return cidrs.Any(cidr => address.ToString().IsInCidr(cidr));
        }
    }
}