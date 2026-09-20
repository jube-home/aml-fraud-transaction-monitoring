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
namespace Jube.Dto.Query.CaseById
{
    [Description("A formatted payload field of a case.")]
    public class CaseByIdFieldEntryDto
    {
        [Description("Display name of the field.")]
        public string? Name { get; set; }

        [Description("Value of the field selected from the case JSON.")]
        public string? Value { get; set; }

        [Description("Foreground colour applied when the conditional format matches.")]
        public string? CellFormatForeColor { get; set; }

        [Description("True when the foreground colour applies to the whole row.")]
        public bool CellFormatForeRow { get; set; }

        [Description("Background colour applied when the conditional format matches.")]
        public string? CellFormatBackColor { get; set; }

        [Description("True when the background colour applies to the whole row.")]
        public bool CellFormatBackRow { get; set; }

        [Description("True when the regular expression matched the value.")]
        public bool ExistsMatch { get; set; }

        [Description("True when conditional regular expression formatting is enabled for the field.")]
        public bool ConditionalRegularExpressionFormatting { get; set; }
    }
}