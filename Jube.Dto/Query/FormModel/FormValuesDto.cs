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
    [Description("The rows of values a record owns, edited in a grid under the record, such as the values of a list or the pairs of a dictionary.")]
    public class FormValuesDto
    {
        [Description("The area of the value record type.")]
        public string Area { get; set; } = string.Empty;

        [Description("The route the value rows are created, read, updated and deleted through.")]
        public string Endpoint { get; set; } = string.Empty;

        [Description("The property of a value row that holds the identifier of the owning record, which is also the name of the read parameter.")]
        public string ParentField { get; set; } = string.Empty;

        [Description("The approval kind number of the owning record, which the value approval status is requested by.")]
        public int ParentKind { get; set; }

        [Description("The editable values, in column order.")]
        public List<FormValueFieldDto> Fields { get; set; } = [];
    }
}
