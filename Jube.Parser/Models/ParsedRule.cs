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

using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Parser.Models
{
    public class ParsedRule
    {
        public readonly Dictionary<string, SelectedPayloadData> SelectedPayloadData = [];
        public readonly List<RuleReference> References = [];
        public readonly List<TrackedLine> LineMaps = [];
        public int CharOffset;
        public List<ErrorSpan> ErrorSpans;
        public int LineOffset;
        public string OriginalRuleText;
        public string ParsedRuleText;

        public void Locate(int line, int translatedStart, int translatedEnd, ErrorSpan span)
        {
            if (line < 0 || line >= LineMaps.Count)
            {
                return;
            }

            var (start, end) = LineMaps[line].ToOriginal(translatedStart, translatedEnd);

            span.Column = start;
            span.EndColumn = end;
        }
    }
}