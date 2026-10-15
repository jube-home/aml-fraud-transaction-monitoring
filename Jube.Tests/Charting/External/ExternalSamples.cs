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

using System;
using System.Collections.Generic;
using System.Linq;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Interfaces;
using Jube.Test.Charting.Diagrams;
using Jube.Test.Charting.More;
using Jube.Test.Charting.Performance;

namespace Jube.Test.Charting.External
{
    public static class ExternalSamples
    {
        public static IReadOnlyList<string> Names()
        {
            return [.. Registry().Keys];
        }

        public static IReadOnlyList<string> GoldenNames()
        {
            return
            [
                .. ChartSamples.All().Keys
                    .Concat(DiagramSamples.All().Keys)
                    .Concat(MoreSamples.All().Keys)
                    .Concat(PerformanceSamples.All().Keys)
                    .Concat(Extras().Keys)
                    .OrderBy(k => k, StringComparer.Ordinal)
            ];
        }

        public static string RenderInline(string name)
        {
            return Registry()[name]().Render();
        }

        public static string RenderExternal(string name)
        {
            return ToExternal(Registry()[name]()).Render();
        }

        public static ISvgChart ToExternal(ISvgChart chart)
        {
            return ((IStyleModeChart)chart).WithStyleMode(ChartStyleMode.External);
        }

        private static SortedDictionary<string, Func<ISvgChart>> Registry()
        {
            var all = new SortedDictionary<string, Func<ISvgChart>>(StringComparer.Ordinal);

            foreach (var key in ChartSamples.All().Keys)
            {
                var k = key;
                all[k] = () => ChartSamples.All()[k];
            }

            foreach (var key in DiagramSamples.Showcase().Keys)
            {
                var k = key;
                all[k] = () => DiagramSamples.Showcase()[k];
            }

            foreach (var key in MoreSamples.All().Keys)
            {
                var k = key;
                all[k] = () => MoreSamples.All()[k];
            }

            foreach (var key in PerformanceSamples.All().Keys)
            {
                var k = key;
                all[k] = () => PerformanceSamples.All()[k];
            }

            foreach (var pair in Extras())
            {
                all[pair.Key] = pair.Value;
            }

            return all;
        }

        private static SortedDictionary<string, Func<ISvgChart>> Extras()
        {
            return new SortedDictionary<string, Func<ISvgChart>>(StringComparer.Ordinal)
            {
                ["x-custom-series-colours"] = () => Chart.Line().Title("Custom colours").Id("custom-colours")
                    .Series("Red", [1.0, 3, 2, 4], "#c0392b").Series("Green", [2.0, 2, 3, 1], "#1e8449")
                    .Series("Default", [3.0, 1, 4, 2]),
                ["x-point-colours"] = () => Chart.Column().Title("Point colours").Id("point-colours")
                    .Categories(["a", "b", "c"]).Series("Count", [3.0, 5, 2]).PointColors(["#c0392b", null, "#1e8449"]),
                ["x-many-series"] = () => Chart.Line().Title("Twelve series").Id("many-series")
                    .Series("s1", [1.0, 2]).Series("s2", [2.0, 3]).Series("s3", [3.0, 4]).Series("s4", [4.0, 5])
                    .Series("s5", [5.0, 6]).Series("s6", [6.0, 7]).Series("s7", [7.0, 8]).Series("s8", [8.0, 9])
                    .Series("s9", [9.0, 10]).Series("s10", [10.0, 11]).Series("s11", [11.0, 12], "#336699")
                    .Series("s12", [12.0, 13]),
                ["x-dark-fixed"] = () => Chart.Column().Title("Dark fixed").Id("dark-fixed").Theme(ThemeMode.Dark)
                    .FixedSize(480, 300).Categories(["a", "b"]).Series("Count", [3.0, 5])
            };
        }
    }
}