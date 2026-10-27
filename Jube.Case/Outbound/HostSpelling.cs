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

namespace Jube.Case.Outbound
{
    public static class HostSpelling
    {
        public static bool TryParseAddress(string? host, out IPAddress address)
        {
            address = IPAddress.None;

            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            var text = host.Trim();

            if (text.StartsWith('[') && text.EndsWith(']'))
            {
                text = text[1..^1];
            }

            var percent = text.IndexOf('%');

            if (percent >= 0)
            {
                text = text[..percent];
            }

            if (text.Contains(':'))
            {
                return IPAddress.TryParse(text, out address!);
            }

            return TryParseLegacyIPv4(text, out address);
        }

        private static bool TryParseLegacyIPv4(string text, out IPAddress address)
        {
            address = IPAddress.None;

            if (text.EndsWith('.'))
            {
                text = text[..^1];
            }

            var parts = text.Split('.');

            if (parts.Length is < 1 or > 4)
            {
                return false;
            }

            var values = new ulong[parts.Length];

            for (var i = 0; i < parts.Length; i++)
            {
                if (!TryParsePart(parts[i], out values[i]))
                {
                    return false;
                }
            }

            for (var i = 0; i < parts.Length - 1; i++)
            {
                if (values[i] > 255)
                {
                    return false;
                }
            }

            var last = values[^1];
            var capacity = 1UL << (8 * (5 - parts.Length));

            if (last >= capacity)
            {
                return false;
            }

            ulong result = last;

            for (var i = 0; i < parts.Length - 1; i++)
            {
                result |= values[i] << (8 * (3 - i));
            }

            address = new IPAddress([(byte)(result >> 24), (byte)(result >> 16), (byte)(result >> 8), (byte)result]);

            return true;
        }

        private static bool TryParsePart(string part, out ulong value)
        {
            value = 0;

            if (part.Length == 0)
            {
                return false;
            }

            if (part.Length > 2 && part[0] == '0' && (part[1] == 'x' || part[1] == 'X'))
            {
                return ulong.TryParse(part[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
            }

            if (part.Length > 1 && part[0] == '0')
            {
                return TryParseOctal(part[1..], out value);
            }

            return part.All(char.IsAsciiDigit) && ulong.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseOctal(string digits, out ulong value)
        {
            value = 0;

            foreach (var digit in digits)
            {
                if (digit is < '0' or > '7' || value > ulong.MaxValue / 8 - 8)
                {
                    return false;
                }

                value = value * 8 + (ulong)(digit - '0');
            }

            return true;
        }
    }
}
