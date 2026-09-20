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
namespace Jube.Dto.Query.CaseBySessionCaseSearchCompile
{
    public class CaseBySessionCaseSearchCompileFieldEntryDto
    {
        [Description("Configured name of the case workflow XPath.")]
        public string? Name { get; set; }

        [Description("Value selected from the payload by the XPath.")]
        public string? Value { get; set; }

        [Description("Conditional formatting foreground colour.")]
        public string? CellFormatForeColor { get; set; }

        [Description("Whether the foreground colour applies to the whole row.")]
        public bool CellFormatForeRow { get; set; }

        [Description("Conditional formatting background colour.")]
        public string? CellFormatBackColor { get; set; }

        [Description("Whether the background colour applies to the whole row.")]
        public bool CellFormatBackRow { get; set; }

        [Description("Whether the value matched the configured regular expression.")]
        public bool ExistsMatch { get; set; }

        [Description("Whether conditional regular expression formatting is enabled.")]
        public bool ConditionalRegularExpressionFormatting { get; set; }
    }
}