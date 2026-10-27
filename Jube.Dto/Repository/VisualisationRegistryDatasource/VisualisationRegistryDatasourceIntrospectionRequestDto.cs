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
    public class VisualisationRegistryDatasourceIntrospectionRequestDto
    {
        [Description("Identifier of the Visualisation Registry the Datasource belongs to. Its parameters, with " +
                     "their default values, are bound to any '@Parameter_Name' tokens in the Command, and it must " +
                     "belong to the caller's tenant.")]
        public int VisualisationRegistryId { get; set; }

        [Description("The SQL SELECT statement to inspect. It is checked against the reporting connection exactly " +
                     "as it would be on save, but is never executed beyond reading its column definitions.")]
        public string? Command { get; set; }

        [Description("Optional. The chart kind being built, from DatasourceDisplayKind. When set, the result says " +
                     "how the columns map to that chart's roles. 0 asks about no particular chart.")]
        public int DisplayKindId { get; set; }

        [Description("Optional. Explicit assignments of result columns to chart roles for the chart being built. " +
                     "Roles left out are taken by position.")]
        public List<DatasourceMappingEntryDto> Mapping { get; set; } = [];

        [Description("Optional. The template text being built, checked when DisplayKindId is the Template kind.")]
        public string? TemplateText { get; set; }
    }
}
