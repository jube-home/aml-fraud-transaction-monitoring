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

using System.Collections.Generic;
using System.ComponentModel;
using Jube.Dto.Query.FormModel;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.GridModel
{
    [Description("One filter above a grid, sent to the list endpoint as a query parameter.")]
    public class GridFilterDto
    {
        [Description("The query parameter the value is sent as.")]
        public string Param { get; set; } = string.Empty;

        [Description("The text shown beside the filter.")]
        public string Label { get; set; } = string.Empty;

        [Description("select when the filter picks one of the options, text otherwise.")]
        public string Kind { get; set; } = "text";

        [Description("The choices, when the filter is a select.")]
        public List<FormOptionDto> Options { get; set; } = [];

        [Description("The form area whose records are the choices, when the filter picks one record by its identifier.")]
        public string? Lookup { get; set; }

        public GridFilterDto Copy()
        {
            return (GridFilterDto)MemberwiseClone();
        }
    }
}
