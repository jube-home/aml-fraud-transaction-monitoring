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

namespace Jube.Dto.Validation
{
    [Description("One validation failure. When the failure is in rule text, Line, Start and Length locate it within " +
                 "that text.")]
    public class ValidationErrorDto
    {
        [Description("Name of the property that failed, e.g. Name or CoderRuleScript.")]
        public string PropertyName { get; set; } = string.Empty;

        [Description("Stable machine readable code for the failure, e.g. NameDuplicate or RuleScriptInvalid.")]
        public string ErrorCode { get; set; } = string.Empty;

        [Description("Human readable, localised explanation of the failure.")]
        public string Message { get; set; } = string.Empty;

        [Description("Zero based line within the rule text, for rule failures; otherwise null.")]
        public int? Line { get; set; }

        [Description("Zero based character offset within the rule text, for rule failures; otherwise null.")]
        public int? Start { get; set; }

        [Description("Length in characters of the failing span within the rule text, for rule failures; otherwise " +
                     "null.")]
        public int? Length { get; set; }
    }
}