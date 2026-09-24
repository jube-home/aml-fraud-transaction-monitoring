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

namespace Jube.Dto.Filter
{
    [Description("How many rows match a query builder JSON filter, optionally broken down by the values of one " +
                 "field.")]
    public class FilterCountResultDto
    {
        [Description("True when the filter and the group-by field are valid.")]
        public bool Valid { get; set; }

        [Description("The number of matching rows.")]
        public int Count { get; set; }

        [Description("When a group-by field was given, the matching rows counted by that field's value, largest " +
                     "first, capped at 200 values.")]
        public List<FilterCountGroupDto> Groups { get; set; } = [];

        [Description("True when there were more than 200 distinct values and the smallest groups were left out.")]
        public bool GroupsTruncated { get; set; }

        [Description("Problems with the filter or the group-by field. Empty when Valid is true.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}