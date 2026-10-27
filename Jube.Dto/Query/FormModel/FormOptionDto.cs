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
    [Description("One choice a field offers: the value stored and the text shown for it.")]
    public class FormOptionDto
    {
        [Description("A stable name for the choice, the option set and the member, which translations are keyed by.")]
        public string Key { get; set; } = string.Empty;

        [Description("The value stored in the record: a number for a numeric property, text for a text or character " +
                     "property.")]
        public object Value { get; set; } = string.Empty;

        [Description("The text shown to the user.")]
        public string Label { get; set; } = string.Empty;

        public FormOptionDto Copy()
        {
            return (FormOptionDto)MemberwiseClone();
        }
    }
}
