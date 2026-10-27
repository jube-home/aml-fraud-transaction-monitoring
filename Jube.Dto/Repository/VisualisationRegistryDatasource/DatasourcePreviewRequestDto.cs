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
    public class DatasourcePreviewRequestDto
    {
        [Description("Identifier of the Visualisation Registry the Datasource belongs to; its parameters bind to the SQL and it must belong to the caller's tenant.")]
        public int VisualisationRegistryId { get; set; }

        [Description("The SQL SELECT statement to run and draw. It is checked and limited exactly as on save and never stored.")]
        public string? Command { get; set; }

        [Description("Which chart to draw, from DatasourceDisplayKind. 0 draws no chart.")]
        public int DisplayKindId { get; set; }

        [Description("Explicit column assignments for chart roles. Roles left out take columns by position.")]
        public List<DatasourceMappingEntryDto> Mapping { get; set; } = [];

        [Description("The template text to render when DisplayKindId is the Template kind. Ignored for charts.")]
        public string? TemplateText { get; set; }

        [Description("When true the rows come back as a grid. When false no row data is sent.")]
        public bool IncludeGrid { get; set; }

        [Description("Parameters, theme, style and size, as for a saved card.")]
        public DatasourceCardRequestDto Card { get; set; } = new();

        [Description("Ordering within the dashboard; carried through to the card unchanged.")]
        public int Priority { get; set; }

        [Description("Dashboard columns the card spans; carried through to the card unchanged.")]
        public int ColumnSpan { get; set; }

        [Description("Dashboard rows the card spans; carried through to the card unchanged.")]
        public int RowSpan { get; set; }
    }
}
