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

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Query.QueryBuilder
{
    [Description("An operator the rule builder offers, with the rule text it produces.")]
    public class BuilderOperatorDto
    {
        [Description("The operator key used in builder JSON, e.g. is_valid_iban.")]
        public string Type { get; set; } = string.Empty;

        [Description("The label shown to the user, e.g. Is a valid IBAN.")]
        public string Label { get; set; } = string.Empty;

        [Description("The group the operator is listed under, e.g. Validation.")]
        public string Group { get; set; } = string.Empty;

        [Description("The values the operator takes, in order: each has a name, a kind (value, number, integer, " +
                     "text, date, list or field) and whether it takes several values separated by commas.")]
        public List<BuilderOperatorArgumentDto> Arguments { get; set; } = [];

        [Description(
            "The field data types the operator applies to: string, integer, double, boolean, datetime or list.")]
        public List<string> ApplyTo { get; set; } = [];

        [Description("The rule text appended to the field, with ? replaced by the values separated by commas.")]
        public string Template { get; set; } = string.Empty;
    }
}