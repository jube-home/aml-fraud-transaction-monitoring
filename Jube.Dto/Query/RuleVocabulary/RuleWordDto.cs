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

namespace Jube.Dto.Query.RuleVocabulary
{
    [Description("A word rule text may use, with every way to call it when it is a function.")]
    [MessagePackObject]
    public class RuleWordDto
    {
        [Description("The word, as rule text should spell it; matching ignores case.")]
        [Key(0)]
        public string Name { get; set; } = string.Empty;

        [Description("Keyword (built into the rule language), AllowedWord (allowed by this installation), Function " +
                     "(a rule function called on a value) or CuratedExpression (a named expression called on a " +
                     "string).")]
        [Key(1)]
        public string Kind { get; set; } = string.Empty;

        [Description("For a function or curated expression, every way to call it.")]
        [Key(2)]
        public List<RuleWordOverloadDto> Overloads { get; set; } = [];

        [Description("What the word means in a rule, in a sentence, when one is recorded; otherwise null.")]
        [Key(3)]
        public string? Description { get; set; }
    }
}