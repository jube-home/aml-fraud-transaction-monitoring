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
using Jube.Dto.Validation;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Filter
{
    [Description("The rows matching a query builder JSON filter, ordered by id and capped; when More is true, call " +
                 "again with afterId set to the last returned Id to continue.")]
    [MessagePackObject]
    public class FilterResultDto<T>
    {
        [Description("True when the filter is valid; the rows are then in Items.")]
        [Key(0)]
        public bool Valid { get; set; }

        [Description("The matching rows, ordered by id.")]
        [Key(1)]
        public List<T> Items { get; set; } = [];

        [Description("True when more matching rows follow the last one returned.")]
        [Key(2)]
        public bool More { get; set; }

        [Description("Problems with the filter, each with a JSON path such as $.rules[0].operator. Empty when " +
                     "Valid is true.")]
        [Key(3)]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}