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

using System;
using System.Linq;

namespace Jube.App.Wire
{
    public sealed class WireOptions(
        WireMode mode,
        string encryptionPublicKey = null,
        WireCompression compression = WireCompression.None,
        bool generatedDecoders = true)
    {
        public WireMode Mode { get; } = mode;

        public WireCompression Compression { get; } = compression;

        public bool GeneratedDecoders { get; } = generatedDecoders;

        public string EncryptionPublicKey { get; } = encryptionPublicKey;

        public bool Encryption => !string.IsNullOrEmpty(EncryptionPublicKey);

        public static bool TryParse(string value, out WireMode mode)
        {
            return TryParseName(value, out mode);
        }

        public static bool TryParseCompression(string value, out WireCompression compression)
        {
            return TryParseName(value, out compression);
        }

        public static bool TryParseHttpCompression(string value, out WireHttpCompressionKind kind)
        {
            return TryParseName(value, out kind);
        }

        public static bool TryParseHttpCompressionLevel(string value, out System.IO.Compression.CompressionLevel level)
        {
            return TryParseName(value, out level) && level != System.IO.Compression.CompressionLevel.NoCompression;
        }

        private static bool TryParseName<TEnum>(string value, out TEnum result) where TEnum : struct, Enum
        {
            result = default;

            var name = Enum.GetNames<TEnum>()
                .FirstOrDefault(candidate => candidate.Equals(value, StringComparison.OrdinalIgnoreCase));

            return name != null && Enum.TryParse(name, out result);
        }
    }
}
