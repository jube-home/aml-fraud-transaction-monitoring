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

using System.Numerics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Jube.Dictionary.Extensions
{
    internal static class WalletAddressSupport
    {
        private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        private const string Bech32Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        private const int Bech32Constant = 1;
        private const int Bech32MConstant = 0x2bc830a3;

        private static readonly Regex ethereumAddress =
            new("^0x[0-9a-fA-F]{40}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal static bool IsValid(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return ethereumAddress.IsMatch(text) || IsBase58CheckAddress(text) || IsBech32BitcoinAddress(text);
        }

        private static bool IsBase58CheckAddress(string text)
        {
            if (text.Length < 26 || text.Length > 35)
            {
                return false;
            }

            var value = BigInteger.Zero;

            foreach (var character in text)
            {
                var digit = Base58Alphabet.IndexOf(character);

                if (digit < 0)
                {
                    return false;
                }

                value = value * 58 + digit;
            }

            var significant =
                value.IsZero ? Array.Empty<byte>() : value.ToByteArray(isUnsigned: true, isBigEndian: true);

            var leadingZeros = text.TakeWhile(character => character == '1').Count();

            var payload = new byte[leadingZeros + significant.Length];
            Array.Copy(significant, 0, payload, leadingZeros, significant.Length);

            if (payload.Length != 25 || (payload[0] != 0x00 && payload[0] != 0x05))
            {
                return false;
            }

            var checksum = SHA256.HashData(SHA256.HashData(payload.AsSpan(0, 21)));

            return checksum.AsSpan(0, 4).SequenceEqual(payload.AsSpan(21, 4));
        }

        private static bool IsBech32BitcoinAddress(string text)
        {
            if (text.Length < 14 || text.Length > 90)
            {
                return false;
            }

            var lower = text.ToLowerInvariant();

            if (text != lower && text != text.ToUpperInvariant())
            {
                return false;
            }

            if (!lower.StartsWith("bc1", StringComparison.Ordinal))
            {
                return false;
            }

            var data = new List<int>();

            for (var i = 3; i < lower.Length; i++)
            {
                var value = Bech32Charset.IndexOf(lower[i]);

                if (value < 0)
                {
                    return false;
                }

                data.Add(value);
            }

            if (data.Count < 7)
            {
                return false;
            }

            var hrpExpanded = new[] { 'b', 'c' }.Select(character => character >> 5)
                .Concat(new[] { 0 })
                .Concat(new[] { 'b', 'c' }.Select(character => character & 31));

            var version = data[0];

            if (version > 16)
            {
                return false;
            }

            var expectedConstant = version == 0 ? Bech32Constant : Bech32MConstant;

            if (Polymod(hrpExpanded.Concat(data)) != expectedConstant)
            {
                return false;
            }

            var program = ConvertBits(data.Skip(1).Take(data.Count - 7), 5, 8, false);

            if (program is null || program.Count < 2 || program.Count > 40)
            {
                return false;
            }

            return version != 0 || program.Count == 20 || program.Count == 32;
        }

        private static int Polymod(IEnumerable<int> values)
        {
            int[] generator = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
            var checksum = 1;

            foreach (var value in values)
            {
                var top = checksum >> 25;
                checksum = ((checksum & 0x1ffffff) << 5) ^ value;

                for (var i = 0; i < 5; i++)
                {
                    if (((top >> i) & 1) == 1)
                    {
                        checksum ^= generator[i];
                    }
                }
            }

            return checksum;
        }

        private static List<byte>? ConvertBits(IEnumerable<int> data, int from, int to, bool pad)
        {
            var accumulator = 0;
            var bits = 0;
            var maximum = (1 << to) - 1;
            var mask = (1 << (from + to - 1)) - 1;
            var result = new List<byte>();

            foreach (var value in data)
            {
                if (value < 0 || (value >> from) != 0)
                {
                    return null;
                }

                accumulator = ((accumulator << from) | value) & mask;
                bits += from;

                while (bits >= to)
                {
                    bits -= to;
                    result.Add((byte)((accumulator >> bits) & maximum));
                }
            }

            if (pad)
            {
                if (bits > 0)
                {
                    result.Add((byte)((accumulator << (to - bits)) & maximum));
                }
            }
            else if (bits >= from || ((accumulator << (to - bits)) & maximum) != 0)
            {
                return null;
            }

            return result;
        }
    }
}