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
    [Description("One column of a grid, with everything needed to draw it.")]
    public class GridColumnDto
    {
        [Description("The property name in camel case, as it appears in each row of the response.")]
        public string Field { get; set; } = string.Empty;

        [Description("The text shown in the column heading.")]
        public string Title { get; set; } = string.Empty;

        [Description("What the property means, from the description attribute on the DTO.")]
        public string Description { get; set; } = string.Empty;

        [Description("string, integer, number, boolean, datetime or guid.")]
        public string Type { get; set; } = string.Empty;

        [Description("How the value is formatted: default, integer, decimal, percent, dateTime, date or code.")]
        public string Format { get; set; } = "default";

        [Description("The unit shown with the value, such as µs; empty when there is none.")]
        public string? Unit { get; set; }

        [Description("Where the column sits; lower first.")]
        public int Order { get; set; }

        [Description("Whether the user can sort by the column.")]
        public bool Sortable { get; set; } = true;

        [Description("Whether the column is left out of the grid.")]
        public bool Hidden { get; set; }

        [Description("For a column worked out from two others: the property divided into, otherwise null.")]
        public string? Numerator { get; set; }

        [Description("For a column worked out from two others: the property divided by, otherwise null.")]
        public string? Denominator { get; set; }

        public GridColumnDto Copy()
        {
            return (GridColumnDto)MemberwiseClone();
        }
    }
}
