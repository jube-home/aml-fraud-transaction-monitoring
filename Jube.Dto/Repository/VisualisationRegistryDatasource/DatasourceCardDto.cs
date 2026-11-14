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
    public class DatasourceCardDto
    {
        [Description("Identifier of the Datasource. 0 for a preview.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Guid of the Datasource. Empty for a preview.")]
        [Key(1)]
        public Guid Guid { get; set; }

        [Description("Datasource name, used as the card header.")]
        [Key(2)]
        public string Name { get; set; } = string.Empty;

        [Description("Dashboard columns the card spans.")]
        [Key(3)]
        public int ColumnSpan { get; set; }

        [Description("Dashboard rows the card spans.")]
        [Key(4)]
        public int RowSpan { get; set; }

        [Description("Ordering within the dashboard; lower first.")]
        [Key(5)]
        public double Priority { get; set; }

        [Description("From DatasourceCardState: 1=ready, 2=empty, 3=error.")]
        [Key(6)]
        public int StateId { get; set; }

        [Description("Why the card is empty or in error. Null when ready.")]
        [Key(7)]
        public string? Message { get; set; }

        [Description("Rows the query returned, up to the card limit.")]
        [Key(8)]
        public int RowCount { get; set; }

        [Description("True when the query returned more rows than the card limit and the rest were dropped.")]
        [Key(9)]
        public bool Truncated { get; set; }

        [Description("How long the query took.")]
        [Key(10)]
        public int ExecutionMilliseconds { get; set; }

        [Description("The rendered chart. Null when no chart is configured or the card is in error.")]
        [Key(11)]
        public DatasourceCardDisplayDto? Display { get; set; }

        [Description("The rows. Null unless the grid is switched on.")]
        [Key(12)]
        public DatasourceCardGridDto? Grid { get; set; }

        [Description("What the SVG needs in order to draw correctly on its own.")]
        [Key(13)]
        public DatasourceCardStyleDto Style { get; set; } = new();
    }
}
