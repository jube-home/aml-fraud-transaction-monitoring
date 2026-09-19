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

using Jube.Dto.Query.Parser;

namespace Jube.Service.Query.Parser
{
    internal static class ParserMapper
    {
        internal static List<ParseRuleErrorSpanDto> ToDto(IEnumerable<global::Jube.Parser.ErrorSpan> errorSpans)
        {
            return errorSpans.Select(s => new ParseRuleErrorSpanDto
            {
                Start = s.Start,
                Length = s.Length,
                Message = s.Message,
                Line = s.Line
            }).ToList();
        }
    }
}