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
using Jube.Charting.Ticks;

namespace Jube.Charting.Core
{
    internal static class AxisFactory
    {
        public static (IScale Scale, IReadOnlyList<double> Values) Numeric(double min, double max, double r0, double r1,
            AxisKind kind, int target, (double Min, double Max)? fixedDomain = null)
        {
            if (fixedDomain is { } fd)
            {
                var scale = kind == AxisKind.Log && fd.Min > 0
                    ? (IScale)new LogScale(fd.Min, fd.Max, r0, r1)
                    : new LinearScale(fd.Min, fd.Max, r0, r1);

                var values = kind == AxisKind.Log && fd.Min > 0
                    ? LogTicks.Generate(fd.Min, fd.Max)
                    :
                    [
                        .. ExtendedTicks.Generate(fd.Min, fd.Max, target)
                            .Where(v => v >= fd.Min - 1e-9 && v <= fd.Max + 1e-9)
                    ];

                return (scale, values);
            }

            if (kind == AxisKind.Log && min > 0)
            {
                var scale = new LogScale(min, max, r0, r1);

                return (scale, LogTicks.Generate(scale.DomainMin, scale.DomainMax));
            }

            var ticks = ExtendedTicks.Generate(min, max, target, true);
            var lo = ticks[0];
            var hi = ticks[^1];

            if (ticks.Count < 2 || hi <= lo)
            {
                var pad = min == 0 ? 1 : Math.Abs(min) * 0.1;
                ticks = ExtendedTicks.Generate(min - pad, max + pad, target, true);
                lo = ticks[0];
                hi = ticks[^1];
            }

            return (new LinearScale(lo, hi, r0, r1), ticks);
        }

        public static IReadOnlyList<Tick> ToTicks(IScale scale, IReadOnlyList<double> values,
            Func<double, string>? format)
        {
            var labels = format is null
                ? NumberFormat.Axis(values)
                : [.. values.Select(format)];

            var result = new Tick[values.Count];

            for (var i = 0; i < values.Count; i++)
            {
                result[i] = new Tick(values[i], scale.Map(values[i]), labels[i]);
            }

            return result;
        }
    }
}