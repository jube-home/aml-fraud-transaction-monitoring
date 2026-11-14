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
    [Description("The positional layout of every DTO the UI exchanges as MessagePack, and the endpoints that accept " +
                 "a MessagePack body. Static for the life of the process and identified by Hash.")]
    [MessagePackObject]
    public class WireSchemaDto
    {
        [Description("Content hash of Types and Routes. Cache the schema against it indefinitely.")]
        [Key(0)]
        public string Hash { get; set; } = string.Empty;

        [Description("Wire types keyed by name.")]
        [Key(1)]
        public Dictionary<string, WireTypeDto> Types { get; set; } = new();

        [Description("Endpoints that accept a MessagePack request body.")]
        [Key(2)]
        public List<WireRouteDto> Routes { get; set; } = [];

        [Description("Json, Mixed or MessagePack: the server's wire policy. Request bodies are sent as MessagePack " +
                     "only when this is MessagePack; responses always announce their own format.")]
        [Key(3)]
        public string Mode { get; set; } = string.Empty;

        [Description("True when the server accepts and requires the client to protect each authenticated call with " +
                     "a per-request AES-GCM key wrapped by EncryptionPublicKey.")]
        [Key(4)]
        public bool Encryption { get; set; }

        [Description("Base64 SubjectPublicKeyInfo of the RSA key a per-request key is wrapped with using " +
                     "RSA-OAEP-SHA256. Empty when Encryption is false.")]
        [Key(5)]
        public string EncryptionPublicKey { get; set; } = string.Empty;

        [Description("Content hash of the wire types that come from the Jube.Dto assembly alone, which is what the " +
                     "generated TypeScript decoders are built from. A client uses its generated decoders only " +
                     "when this equals the hash baked into them.")]
        [Key(6)]
        public string TypesHash { get; set; } = string.Empty;

        [Description("True when the browser may use the generated TypeScript decoders, which it downloads once as a " +
                     "separate cacheable module. False makes it use the decoders it builds from this schema instead.")]
        [Key(7)]
        public bool GeneratedDecoders { get; set; } = true;
    }
}
