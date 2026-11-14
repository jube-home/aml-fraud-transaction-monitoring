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
using Jube.Dto.Wire;
using MessagePack;
using MessagePack.Resolvers;

namespace Jube.App.Wire
{
    public static class WireSerializer
    {
        private const int MaximumObjectGraphDepth = 64;

        private static readonly UtcDateTimeFormatter DateTimeFormatter = new();
        private static readonly UtcDateTimeOffsetFormatter DateTimeOffsetFormatter = new();
        private static readonly FiniteDoubleFormatter DoubleFormatter = new();
        private static readonly DecimalAsDoubleFormatter DecimalFormatter = new();
        private static readonly CharAsStringFormatter CharFormatter = new();

        public const int CompressionMinimumBytes = 1024;
        private const byte Lz4BlockExtensionType = 99;

        public static bool IsLz4Block(byte[] body)
        {
            if (body.Length < 3)
            {
                return false;
            }

            return body[0] switch
            {
                0xC7 => body.Length > 2 && body[2] == Lz4BlockExtensionType,
                0xC8 => body.Length > 3 && body[3] == Lz4BlockExtensionType,
                0xC9 => body.Length > 5 && body[5] == Lz4BlockExtensionType,
                _ => false
            };
        }

        public static MessagePackSerializerOptions Options { get; } = MessagePackSerializerOptions.Standard
            .WithResolver(CompositeResolver.Create(
                [
                    DateTimeFormatter,
                    DateTimeOffsetFormatter,
                    DoubleFormatter,
                    DecimalFormatter,
                    CharFormatter,
                    new StrictUtf8StringFormatter(),
                    new NullableValueFormatter<DateTime>(DateTimeFormatter),
                    new NullableValueFormatter<DateTimeOffset>(DateTimeOffsetFormatter),
                    new NullableValueFormatter<double>(DoubleFormatter),
                    new NullableValueFormatter<decimal>(DecimalFormatter),
                    new NullableValueFormatter<char>(CharFormatter),
                    new JsonShapedObjectFormatter()
                ],
                [ColumnarRowsResolver.Instance, StringKeyedMapResolver.Instance, StandardResolver.Instance]))
            .WithSecurity(MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(MaximumObjectGraphDepth));

        public static MessagePackSerializerOptions Lz4Options { get; } = Options
            .WithCompression(MessagePackCompression.Lz4Block)
            .WithCompressionMinLength(CompressionMinimumBytes);
    }
}
