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

using System.ComponentModel;
using MessagePack;

namespace Jube.Dto.Query.WireSchema
{
    [Description("An endpoint that accepts a MessagePack request body, so the client can encode the body " +
                 "positionally before sending it.")]
    [MessagePackObject]
    public class WireRouteDto
    {
        [Description("Upper-case HTTP method.")]
        [Key(0)]
        public string Method { get; set; } = string.Empty;

        [Description("Route template exactly as registered, including any parameter constraints.")]
        [Key(1)]
        public string Template { get; set; } = string.Empty;

        [Description("Shape of the request body.")]
        [Key(2)]
        public string Shape { get; set; } = string.Empty;
    }
}
