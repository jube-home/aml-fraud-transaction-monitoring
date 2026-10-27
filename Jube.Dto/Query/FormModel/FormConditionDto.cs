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
    [Description("A rule that looks at another field of the same record.")]
    public class FormConditionDto
    {
        [Description("The property being looked at, in camel case.")]
        public string Property { get; set; } = string.Empty;

        [Description("equals, notEquals, in, notIn, greaterThan, isEmpty or isNotEmpty.")]
        public string Op { get; set; } = string.Empty;

        [Description("The values the property is compared with.")]
        public List<object?> Values { get; set; } = [];

        [Description("Whether all or any of a field's conditions of this kind must hold.")]
        public string MatchMode { get; set; } = "all";
    }
}
