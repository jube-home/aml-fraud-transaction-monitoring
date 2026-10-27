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
    [Description("A named group of fields, which may be nested by a slash separated path.")]
    public class FormGroupDto
    {
        [Description("The group path, such as Identity or Advanced/Limits.")]
        public string Path { get; set; } = string.Empty;

        [Description("The text shown for the group.")]
        public string Label { get; set; } = string.Empty;

        [Description("Where the group sits among its siblings; lower first.")]
        public int Order { get; set; } = 0;

        [Description("Whether the group starts collapsed.")]
        public bool Collapsed { get; set; } = false;

        [Description("How the group is drawn.")]
        public string Widget { get; set; } = "default";

        public FormGroupDto Copy()
        {
            return (FormGroupDto)MemberwiseClone();
        }
    }
}
