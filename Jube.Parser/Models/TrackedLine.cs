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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Parser.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public sealed class TrackedLine
    {
        private readonly int originalLength;
        private int[] originEnds;
        private int[] originStarts;

        public TrackedLine(string original)
        {
            Text = original;
            originalLength = original.Length;
            originStarts = new int[original.Length];
            originEnds = new int[original.Length];

            for (var i = 0; i < original.Length; i++)
            {
                originStarts[i] = i;
                originEnds[i] = i + 1;
            }
        }

        public string Text { get; private set; }

        public void Replace(string find, string replacement)
        {
            if (string.IsNullOrEmpty(find))
            {
                return;
            }

            var text = new StringBuilder();
            var starts = new List<int>();
            var ends = new List<int>();
            var index = 0;

            while (index < Text.Length)
            {
                var hit = Text.IndexOf(find, index, StringComparison.Ordinal);

                if (hit < 0)
                {
                    hit = Text.Length;
                }

                for (var i = index; i < hit; i++)
                {
                    text.Append(Text[i]);
                    starts.Add(originStarts[i]);
                    ends.Add(originEnds[i]);
                }

                if (hit == Text.Length)
                {
                    break;
                }

                var spanStart = originStarts[hit];
                var spanEnd = originEnds[hit + find.Length - 1];

                foreach (var character in replacement)
                {
                    text.Append(character);
                    starts.Add(spanStart);
                    ends.Add(spanEnd);
                }

                index = hit + find.Length;
            }

            Text = text.ToString();
            originStarts = [.. starts];
            originEnds = [.. ends];
        }

        public (int Start, int End) ToOriginal(int translatedStart, int translatedEnd)
        {
            if (Text.Length == 0)
            {
                return (0, 0);
            }

            var first = Math.Clamp(translatedStart, 0, Text.Length - 1);
            var last = Math.Clamp(Math.Max(translatedEnd - 1, translatedStart), 0, Text.Length - 1);

            var start = translatedStart >= Text.Length ? originalLength : originStarts[first];
            var end = translatedStart >= Text.Length ? originalLength : originEnds[last];

            if (end <= start)
            {
                end = Math.Min(start + 1, originalLength);
            }

            return (start, end);
        }
    }
}
