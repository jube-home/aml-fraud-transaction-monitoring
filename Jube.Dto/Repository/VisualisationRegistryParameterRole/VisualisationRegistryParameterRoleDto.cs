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

namespace Jube.Dto.Repository.VisualisationRegistryParameterRole
{
    [MessagePackObject]
    public class VisualisationRegistryParameterRoleDto
    {
        [Description("Server-assigned identifier. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Guid of the Visualisation Registry Parameter this grant applies to.")]
        [Key(1)]
        public Guid VisualisationRegistryParameterGuid { get; set; }

        [Description("Guid of the Role Registry entry being granted access to the Visualisation Registry Parameter.")]
        [Key(2)]
        public Guid RoleRegistryGuid { get; set; }
    }
}