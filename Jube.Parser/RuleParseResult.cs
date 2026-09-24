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

namespace Jube.Parser
{
    using System.Collections.Generic;

    public sealed class RuleParseResult
    {
        internal RuleParseResult()
        {
        }

        public bool Compiled { get; internal init; }
        public string Message { get; internal init; }
        public List<ErrorSpan> ErrorSpans { get; internal init; } = [];
        public string ParsedRuleText { get; internal init; }
        public string ClassName { get; internal init; }
        public List<RuleReference> References { get; internal init; } = [];
        internal byte[] CompiledBinary { get; init; }
    }
}