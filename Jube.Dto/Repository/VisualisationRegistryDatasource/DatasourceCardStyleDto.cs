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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.VisualisationRegistryDatasource
{
    public class DatasourceCardStyleDto
    {
        [Description("From DatasourceStyleMode: 1=external, 2=inline.")]
        public int ModeId { get; set; }

        [Description("Version of the chart stylesheet the SVG was drawn against. Empty for an inline SVG.")]
        public string Version { get; set; } = string.Empty;

        [Description("The chart stylesheet, present only when the caller did not already hold this version. Null for an inline SVG.")]
        public string? Css { get; set; }
    }
}
