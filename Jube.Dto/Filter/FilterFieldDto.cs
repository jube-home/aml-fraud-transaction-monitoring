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

namespace Jube.Dto.Filter
{
    [Description("A field that query builder JSON filters may use: its id, its builder type, the operators allowed " +
                 "for that type and what the field means.")]
    public class FilterFieldDto
    {
        [Description("The field id to put in a rule's id, e.g. Name or Active.")]
        public string Name { get; set; } = string.Empty;

        [Description("The builder type: String, Integer, Double, Boolean, DateTime or Guid.")]
        public string DataType { get; set; } = string.Empty;

        [Description("The operators a rule on this field may use.")]
        public List<string> Operators { get; set; } = [];

        [Description("What the field means.")] public string Description { get; set; } = string.Empty;
    }
}