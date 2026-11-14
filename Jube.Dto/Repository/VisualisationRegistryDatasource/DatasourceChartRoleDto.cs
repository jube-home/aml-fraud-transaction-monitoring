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

namespace Jube.Dto.Repository.VisualisationRegistryDatasource
{
    [MessagePackObject]
    public class DatasourceChartRoleDto
    {
        [Description("From DatasourceChartRole.")]
        [Key(0)]
        public int RoleId { get; set; }

        [Description("Name of the role.")]
        [Key(1)]
        public string Name { get; set; } = string.Empty;

        [Description("What the role means for this chart.")]
        [Key(2)]
        public string Description { get; set; } = string.Empty;

        [Description("True when the role takes several columns, each drawn as its own series.")]
        [Key(3)]
        public bool Many { get; set; }

        [Description("True when the chart draws without this role; optional roles are only assigned explicitly.")]
        [Key(4)]
        public bool Optional { get; set; }

        [Description("From DatasourceRoleAccepts: which column types may fill the role.")]
        [Key(5)]
        public int AcceptsId { get; set; }
    }
}
