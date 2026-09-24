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
    [Description("A word rule text may use, in brief.")]
    public class RuleWordSummaryDto
    {
        [Description("The word.")] public string Name { get; set; } = string.Empty;

        [Description("Keyword, AllowedWord, Function or CuratedExpression.")]
        public string Kind { get; set; } = string.Empty;

        [Description("For a function, the types of value it can be called on.")]
        public List<string> ReceiverTypes { get; set; } = [];

        [Description("How many ways there are to call it; describe it for the signatures.")]
        public int Overloads { get; set; }
    }
}