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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.UserRegistryApiKey
{
    [MessagePackObject]
    public class UserRegistryApiKeyDto
    {
        [Description("Server-assigned identifier of the API key grant. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Identifier of the UserRegistry (service account) this API key authenticates as. Must " +
                     "resolve to a UserRegistry visible to the caller's tenant.")]
        [Key(1)]
        public int UserRegistryId { get; set; }

        [Description("Display name of the API key, chosen by the caller for their own reference. Not required " +
                     "to be unique.")]
        [Key(2)]
        public string? Name { get; set; }

        [Description("Free-text description of the API key's purpose. Optional.")]
        [Key(3)]
        public string? Description { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [Key(4)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned. Immediately after Create, this carries the full plaintext API key -- " +
                     "the only time it is ever returned. On every later read it carries only the first 8 " +
                     "characters of the key, for display/identification purposes; the full key is not " +
                     "recoverable once this response is gone.")]
        [Key(5)]
        public string? ApiKeyDisplay { get; set; }
    }
}