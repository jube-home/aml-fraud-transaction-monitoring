/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
    public class VisualisationRegistryDatasourceSqlCheckDto
    {
        [Description("True when no fault was found in the SQL Command. The reporting database is not consulted for " +
                     "this check beyond its cached table and column names.")]
        [Key(0)]
        public bool Valid { get; set; }

        [Description("Every fault found, in the order they occur. Empty when Valid is true.")]
        [Key(1)]
        public List<VisualisationRegistryDatasourceSqlFindingDto> Findings { get; set; } = [];
    }
}
