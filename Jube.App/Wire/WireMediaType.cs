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
using Microsoft.AspNetCore.Http;

namespace Jube.App.Wire
{
    public static class WireMediaType
    {
        public const string MessagePack = "application/x-msgpack";
        public const string Json = "application/json";
        public const string ColumnarJson = "application/vnd.jube.columnar+json";
        public const string Encrypted = "application/x-jube-encrypted";
        public const string KeyHeader = "X-Wire-Key";
        public const string TimeHeader = "X-Wire-Time";
        public const string InnerHeader = "X-Wire-Inner";
        public const string CompressionHeader = "X-Wire-Compression";
        public const string Lz4 = "lz4";
        public const string ShapeHeader = "X-Wire-Shape";
        public const string SchemaHeader = "X-Wire-Schema";

        public static bool IsMessagePack(string contentType)
        {
            return contentType != null && contentType.StartsWith(MessagePack, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsEncrypted(string contentType)
        {
            return contentType != null && contentType.StartsWith(Encrypted, StringComparison.OrdinalIgnoreCase);
        }

        public static bool AcceptsColumnarJson(HttpRequest request)
        {
            return request.Headers.Accept.Any(value => value != null
                                                       && value.Contains(ColumnarJson,
                                                           StringComparison.OrdinalIgnoreCase));
        }

        public static bool AcceptsMessagePack(HttpRequest request)
        {
            return request.Headers.Accept.Any(value => value != null
                                                       && value.Contains(MessagePack,
                                                           StringComparison.OrdinalIgnoreCase));
        }
    }
}
