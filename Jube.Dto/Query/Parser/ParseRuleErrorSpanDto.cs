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

namespace Jube.Dto.Query.Parser
{
    [Description("The location of one parse or compile error within the rule text.")]
    [MessagePackObject]
    public class ParseRuleErrorSpanDto
    {
        [Description("Zero based character offset of the error within the rule text.")]
        [Key(0)]
        public int Start { get; set; }

        [Description("Length in characters of the erroneous span.")]
        [Key(1)]
        public int Length { get; set; }

        [Description("Human readable error message.")]
        [Key(2)]
        public string Message { get; set; } = string.Empty;

        [Description("Zero based line of the error within the rule text.")]
        [Key(3)]
        public int Line { get; set; }

        [Description("Zero based column in the rule text's own line where the error starts, in the text as typed " +
                     "rather than as translated for compilation; absent when the position is unknown.")]
        [Key(4)]
        public int? Column { get; set; }

        [Description("Zero based column in the rule text's own line where the error ends, exclusive; absent when the " +
                     "position is unknown.")]
        [Key(5)]
        public int? EndColumn { get; set; }

        [Description("A stable code for the kind of problem when one is known, such as ResultDiscarded, " +
                     "NeverAnswers or RestrictedToken; otherwise null. Use it to offer a fix, never the message text.")]
        [Key(6)]
        public string? Code { get; set; }
    }
}