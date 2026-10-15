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

using System.Globalization;
using Jube.Charting.Core;
using Jube.Charting.Interfaces;
using Jube.Charting.Scales;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    internal static class MoreAxes
    {
        public static (IScale Scale, IReadOnlyList<Tick> Ticks) X(XAxisKind kind, double min, double max, Rect plot,
            (double Min, double Max)? domain, Func<double, string>? format)
        {
            if (kind == XAxisKind.Time)
            {
                var lo = domain?.Min ?? min;
                var hi = domain?.Max ?? max;
                var scale = new LinearScale(lo, hi, plot.Left, plot.Right);
                var count = (int)Math.Clamp(plot.Width / 90, 2, 12);

                var ticks = TimeTicks.Generate(TimeScale.FromSeconds(scale.DomainMin),
                    TimeScale.FromSeconds(scale.DomainMax), count);

                return (scale, [.. ticks.Select(t => new Tick(t.Seconds, scale.Map(t.Seconds), t.Label))]);
            }

            var target = (int)Math.Clamp(plot.Width / 80, 3, 12);

            var (xs, values) = AxisFactory.Numeric(min, max, plot.Left, plot.Right,
                kind == XAxisKind.Log ? AxisKind.Log : AxisKind.Linear, target, domain);

            return (xs, AxisFactory.ToTicks(xs, values, format));
        }

        public static string XText(XAxisKind kind, double x, Func<double, string>? format)
        {
            if (kind == XAxisKind.Time)
            {
                var t = TimeScale.FromSeconds(x);

                return t.TimeOfDay == TimeSpan.Zero
                    ? t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : t.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }

            return format is null ? NumberFormat.Value(x) : format(x);
        }
    }
}