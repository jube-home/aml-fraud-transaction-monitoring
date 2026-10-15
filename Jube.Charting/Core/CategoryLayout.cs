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
using Jube.Charting.Interfaces;
using Jube.Charting.Scales;
using Jube.Charting.Text;

namespace Jube.Charting.Core
{
    internal sealed class CategoryLayout
    {
        public required Rect Plot { get; init; }
        public required IScale Value { get; init; }
        public required IReadOnlyList<Tick> ValueTicks { get; init; }
        public required BandScale Band { get; init; }

        public static CategoryLayout Vertical(ChartCanvas canvas, Extent extent, int count,
            Func<double, string>? format,
            double padding = 0.3, (double Min, double Max)? fixedDomain = null)
        {
            var lo = extent.Any ? extent.Min : 0;
            var hi = extent.Any ? extent.Max : 1;
            var plot = canvas.Frame(30, 20);
            IScale scale = null!;
            IReadOnlyList<Tick> ticks = [];

            for (var pass = 0; pass < 3; pass++)
            {
                (scale, var values) = AxisFactory.Numeric(lo, hi, plot.Bottom, plot.Top, AxisKind.Linear,
                    (int)Math.Clamp(plot.Height / 42, 3, 10), fixedDomain);

                ticks = AxisFactory.ToTicks(scale, values, format);
                plot = canvas.Frame(TextMetrics.MaxWidth(ticks.Select(t => t.Label), 11), 20);
            }

            return new CategoryLayout
            {
                Plot = plot,
                Value = scale,
                ValueTicks = ticks,
                Band = new BandScale(Math.Max(count, 1), plot.Left, plot.Right, count > 1 ? padding : 0.5)
            };
        }

        public void DrawAxes(ChartCanvas canvas, IReadOnlyList<string> categories, bool zeroBaseline = true)
        {
            canvas.YAxis(ValueTicks);
            canvas.Baseline(zeroBaseline ? Value.Map(Math.Clamp(0, Value.DomainMin, Value.DomainMax)) : Plot.Bottom);
            canvas.XAxis(CategoryTicks(categories, Band));
        }

        public static IReadOnlyList<Tick> CategoryTicks(IReadOnlyList<string> categories, BandScale band)
        {
            var widest = TextMetrics.MaxWidth(categories, 11);
            var every = Math.Max(1, (int)Math.Ceiling((Math.Min(widest, 140) + 10) / Math.Max(band.Step, 1)));
            var ticks = new List<Tick>();

            for (var i = 0; i < categories.Count; i += every)
            {
                ticks.Add(new Tick(i, band.Center(i),
                    TextMetrics.Truncate(categories[i], Math.Max(band.Step * every - 8, 10), 11)));
            }

            return ticks;
        }
    }
}