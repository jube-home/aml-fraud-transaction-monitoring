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

namespace Jube.Dto.Query.FormModel
{
    [Description("One column of the list view of a record type.")]
    public class FormListColumnDto
    {
        [Description("The property shown, in camel case.")]
        public string Field { get; set; } = string.Empty;

        [Description("The column heading.")]
        public string Title { get; set; } = string.Empty;

        [Description("Where the column sits; lower first.")]
        public int Order { get; set; } = 0;

        [Description("Whether the user can sort by the column.")]
        public bool Sortable { get; set; } = true;

        [Description("Whether the user can filter by the column.")]
        public bool Filterable { get; set; } = true;

        [Description("Whether the column is available but not shown to start with.")]
        public bool Hidden { get; set; } = false;

        public FormListColumnDto Copy()
        {
            return (FormListColumnDto)MemberwiseClone();
        }
    }
}
