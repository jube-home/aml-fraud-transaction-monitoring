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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.FormModel
{
    [Description("Where the choices of a dropdown field come from.")]
    [MessagePackObject]
    public class FormLookupDto
    {
        [Description("The name of the record type or list to read the choices from.")]
        [Key(0)]
        public string Source { get; set; } = string.Empty;

        [Description("The property shown to the user.")]
        [Key(1)]
        public string TextField { get; set; } = "name";

        [Description("The property stored in the record.")]
        [Key(2)]
        public string ValueField { get; set; } = "guid";

        [Description("The property of this record whose value narrows the choices.")]
        [Key(3)]
        public string? ParentField { get; set; }

        [Description("Optional property that holds the highest value a choice stands for, so that a stored value " +
                     "between the choice's value and this property is shown as that choice.")]
        [Key(4)]
        public string? UpperField { get; set; }

        [Description("Whether no choice is a valid answer.")]
        [Key(5)]
        public bool AllowEmpty { get; set; } = false;
    }
}
