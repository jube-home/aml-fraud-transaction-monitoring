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

namespace Jube.Dto.Query.RuleVocabulary
{
    [Description("A word rule text may use, with every way to call it when it is a function.")]
    public class RuleWordDto
    {
        [Description("The word, as rule text should spell it; matching ignores case.")]
        public string Name { get; set; } = string.Empty;

        [Description("Keyword (built into the rule language), AllowedWord (allowed by this installation), Function " +
                     "(a rule function called on a value) or CuratedExpression (a named expression called on a " +
                     "string).")]
        public string Kind { get; set; } = string.Empty;

        [Description("For a function or curated expression, every way to call it.")]
        public List<RuleWordOverloadDto> Overloads { get; set; } = [];
    }
}