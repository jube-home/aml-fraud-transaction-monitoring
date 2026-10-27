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

namespace Jube.Dto.Query.GridModel
{
    [Description("Everything needed to draw a grid of rows from a list endpoint, read from the DTO of one row.")]
    public class GridModelDto
    {
        [Description("The area name, the same as the DTO's grid endpoint attribute.")]
        public string Area { get; set; } = string.Empty;

        [Description("The route that returns the rows, total and statistics.")]
        public string Endpoint { get; set; } = string.Empty;

        [Description("The title of the grid.")]
        public string Title { get; set; } = string.Empty;

        [Description("True when the grid shows the current state and has no time window or row limit.")]
        public bool Snapshot { get; set; }

        [Description("The property the rows are sorted by to begin with; empty for the endpoint's own order.")]
        public string SortField { get; set; } = string.Empty;

        [Description("asc or desc.")]
        public string SortDirection { get; set; } = "desc";

        [Description("The most rows asked for at once.")]
        public int Take { get; set; }

        [Description("The property the from and to window applies to; null when there is no time window.")]
        public string? TimeField { get; set; }

        [Description("How many minutes back the window starts by default.")]
        public int WindowMinutes { get; set; }

        [Description("The query parameter and label of the free-text search; null when there is none.")]
        public GridFilterDto? Search { get; set; }

        [Description("The filters, in display order.")]
        public List<GridFilterDto> Filters { get; set; } = [];

        [Description("The columns, in display order.")]
        public List<GridColumnDto> Columns { get; set; } = [];

        public GridModelDto Copy()
        {
            return (GridModelDto)MemberwiseClone();
        }
    }
}
