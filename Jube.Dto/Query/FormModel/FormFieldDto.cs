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
    [Description("One field of a form, with everything needed to draw and validate it.")]
    public class FormFieldDto
    {
        [Description("The property name in camel case, as it appears in the JSON of the record.")]
        public string Name { get; set; } = string.Empty;

        [Description("The text shown beside the field.")]
        public string Label { get; set; } = string.Empty;

        [Description("What the field means, from the description attribute on the DTO.")]
        public string Description { get; set; } = string.Empty;

        [Description("string, integer, number, boolean, datetime or guid.")]
        public string Type { get; set; } = string.Empty;

        [Description("Whether the property may be null.")]
        public bool Nullable { get; set; } = false;

        [Description("The path of the group the field belongs to; empty when ungrouped.")]
        public string Group { get; set; } = string.Empty;

        [Description("Where the field sits within its group; lower first.")]
        public int Order { get; set; } = 0;

        [Description("How the field is drawn, such as default, switch, date, dropdown or code.")]
        public string Widget { get; set; } = "default";

        [Description("How many of the form's two columns the field takes: 1 (the default) or 2. A field that takes " +
                     "two starts a new row when it does not fit beside the one before it.")]
        public int Span { get; set; } = 1;

        [Description("Whether the field is shown but cannot be edited.")]
        public bool ReadOnly { get; set; } = false;

        [Description("Hint text shown inside an empty field.")]
        public string? Placeholder { get; set; }

        [Description("Longer guidance shown with the field.")]
        public string? Help { get; set; }

        [Description("The name of a specialised editor component to use instead of the generic control.")]
        public string? Editor { get; set; }

        [Description("Where the choices come from, when the field picks from another record type.")]
        public FormLookupDto? Lookup { get; set; }

        [Description("The value a new record starts with.")]
        public object? NewDefault { get; set; }

        [Description("Whether the field must always have a value, from an unconditional rule in the validator. A rule " +
                     "that applies only under a condition is not here; it is a required-when condition on the field.")]
        public bool Required { get; set; }

        [Description("The shortest text the validator accepts; null when there is no minimum.")]
        public int? MinLength { get; set; }

        [Description("The longest text the validator accepts; null when there is no maximum.")]
        public int? MaxLength { get; set; }

        [Description("The smallest number the validator accepts; null when there is no minimum.")]
        public double? Min { get; set; }

        [Description("The largest number the validator accepts; null when there is no maximum.")]
        public double? Max { get; set; }

        [Description("Whether the minimum itself is refused. Always false for an integer field, whose minimum is " +
                     "adjusted to the first value allowed.")]
        public bool ExclusiveMin { get; set; }

        [Description("Whether the maximum itself is refused. Always false for an integer field, whose maximum is " +
                     "adjusted to the last value allowed.")]
        public bool ExclusiveMax { get; set; }

        [Description("Regular expressions the whole value must match, in a syntax that works in both .NET and " +
                     "JavaScript, from the validator. Only anchored whole-value patterns that use no .NET-only " +
                     "construct are carried.")]
        public List<string> Patterns { get; set; } = [];

        [Description("The fixed choices of a select or radio field, in the order of the underlying values; empty " +
                     "when the field is not a fixed choice.")]
        public List<FormOptionDto> Options { get; set; } = [];

        [Description("Conditions that decide whether the field is shown.")]
        public List<FormConditionDto> VisibleWhen { get; set; } = [];

        [Description("Conditions that make the field required.")]
        public List<FormConditionDto> RequiredWhen { get; set; } = [];

        [Description("Conditions that decide whether the field can be edited.")]
        public List<FormConditionDto> EnabledWhen { get; set; } = [];

        public FormFieldDto Copy()
        {
            return (FormFieldDto)MemberwiseClone();
        }
    }
}
