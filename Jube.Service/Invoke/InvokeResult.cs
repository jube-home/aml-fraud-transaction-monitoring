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

namespace Jube.Service.Invoke
{
    public sealed class InvokeResult
    {
        private InvokeResult(int statusCode)
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; }
        public byte[]? Payload { get; private init; }
        public string? ContentType { get; private init; }
        public bool HasJsonValue { get; private init; }
        public object? JsonValue { get; private init; }
        public bool IsForbid { get; private init; }

        public static InvokeResult Empty(int statusCode)
        {
            return new InvokeResult(statusCode);
        }

        public static InvokeResult Content(byte[] payload, string contentType)
        {
            return new InvokeResult(200) { Payload = payload, ContentType = contentType };
        }

        public static InvokeResult Json(int statusCode, object? value)
        {
            return new InvokeResult(statusCode) { HasJsonValue = true, JsonValue = value };
        }

        public static InvokeResult Forbidden()
        {
            return new InvokeResult(403) { IsForbid = true };
        }
    }
}