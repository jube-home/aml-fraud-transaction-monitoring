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
namespace Jube.Dto.Query.Parser
{
    [Description("The location of one parse or compile error within the rule text.")]
    public class ParseRuleErrorSpanDto
    {
        [Description("Zero based character offset of the error within the rule text.")]
        public int Start { get; set; }

        [Description("Length in characters of the erroneous span.")]
        public int Length { get; set; }

        [Description("Human readable error message.")]
        public string Message { get; set; } = string.Empty;

        [Description("Zero based line of the error within the rule text.")]
        public int Line { get; set; }
    }
}