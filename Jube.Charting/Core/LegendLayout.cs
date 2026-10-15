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

using Jube.Charting.Core.Models;
using Jube.Charting.Text;

namespace Jube.Charting.Core
{
    internal sealed class LegendLayout
    {
        public required IReadOnlyList<LegendItem> Items { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }

        public static LegendLayout Compute(IReadOnlyList<LegendEntry> entries, LegendPosition position,
            double availableWidth, int maxRows = 0)
        {
            const double rowHeight = 20;
            const double gap = 18;
            var items = new List<LegendItem>();

            if (position == LegendPosition.Right)
            {
                if (maxRows > 0 && entries.Count > maxRows)
                {
                    var kept = Math.Max(maxRows - 1, 1);

                    entries =
                    [
                        .. entries.Take(kept),
                        new LegendEntry("+" + (entries.Count - kept) + " more", 0, LegendShape.None)
                    ];
                }

                var maxText = Math.Min(availableWidth * 0.28, 200);
                var y = 0.0;
                var widest = 0.0;

                foreach (var entry in entries)
                {
                    var text = TextMetrics.Truncate(entry.Label, maxText, 12);
                    var w = 18 + TextMetrics.Width(text, 12);
                    items.Add(new LegendItem(entry, text, 0, y, w));
                    widest = Math.Max(widest, w);
                    y += rowHeight;
                }

                return new LegendLayout { Items = items, Width = widest, Height = y };
            }

            var maxItem = Math.Min(availableWidth, 240);
            var x = 0.0;
            var row = 0;

            foreach (var entry in entries)
            {
                var text = TextMetrics.Truncate(entry.Label, maxItem - 18, 12);
                var w = 18 + TextMetrics.Width(text, 12);

                if (x > 0 && x + w > availableWidth)
                {
                    x = 0;
                    row++;
                }

                items.Add(new LegendItem(entry, text, x, row * rowHeight, w));
                x += w + gap;
            }

            if (maxRows > 0 && row + 1 > maxRows)
            {
                items = Overflow(items, entries.Count, maxRows, availableWidth, gap);
                row = maxRows - 1;
            }

            return new LegendLayout { Items = items, Width = availableWidth, Height = (row + 1) * rowHeight };
        }

        private static List<LegendItem> Overflow(List<LegendItem> items, int total, int maxRows, double availableWidth,
            double gap)
        {
            const double rowHeight = 20;
            var kept = items.Where(i => i.Y < maxRows * rowHeight - 0.5).ToList();
            var lastRow = (maxRows - 1) * rowHeight;

            while (true)
            {
                var text = "+" + (total - kept.Count) + " more";
                var w = TextMetrics.Width(text, 12);
                var x = kept.Count > 0 && kept[^1].Y > lastRow - 0.5 ? kept[^1].X + kept[^1].Width + gap : 0;

                if (x + w <= availableWidth || kept.Count <= 1)
                {
                    kept.Add(new LegendItem(new LegendEntry(text, 0, LegendShape.None), text, x, lastRow, w));

                    return kept;
                }

                kept.RemoveAt(kept.Count - 1);
            }
        }
    }

    internal readonly record struct LegendItem(LegendEntry Entry, string Text, double X, double Y, double Width);
}