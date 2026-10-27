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
    public class DatasourceCardDto
    {
        [Description("Identifier of the Datasource. 0 for a preview.")]
        public int Id { get; set; }

        [Description("Guid of the Datasource. Empty for a preview.")]
        public Guid Guid { get; set; }

        [Description("Datasource name, used as the card header.")]
        public string Name { get; set; } = string.Empty;

        [Description("Dashboard columns the card spans.")]
        public int ColumnSpan { get; set; }

        [Description("Dashboard rows the card spans.")]
        public int RowSpan { get; set; }

        [Description("Ordering within the dashboard; lower first.")]
        public double Priority { get; set; }

        [Description("From DatasourceCardState: 1=ready, 2=empty, 3=error.")]
        public int StateId { get; set; }

        [Description("Why the card is empty or in error. Null when ready.")]
        public string? Message { get; set; }

        [Description("Rows the query returned, up to the card limit.")]
        public int RowCount { get; set; }

        [Description("True when the query returned more rows than the card limit and the rest were dropped.")]
        public bool Truncated { get; set; }

        [Description("How long the query took.")]
        public int ExecutionMilliseconds { get; set; }

        [Description("The rendered chart. Null when no chart is configured or the card is in error.")]
        public DatasourceCardDisplayDto? Display { get; set; }

        [Description("The rows. Null unless the grid is switched on.")]
        public DatasourceCardGridDto? Grid { get; set; }

        [Description("What the SVG needs in order to draw correctly on its own.")]
        public DatasourceCardStyleDto Style { get; set; } = new();
    }
}
