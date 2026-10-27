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

namespace Jube.CaseRisk.Analytics
{
    public static class Histograms
    {
        public static readonly double[] MinuteEdges = [5, 15, 30, 60, 120, 240, 480, 1440, 4320, 10080];

        public static List<HistogramBucket> Build(IEnumerable<double> values, double[] edges,
            Func<double, string> format)
        {
            var counts = new int[edges.Length + 1];

            foreach (var value in values)
            {
                var index = 0;

                while (index < edges.Length && value >= edges[index])
                {
                    index++;
                }

                counts[index]++;
            }

            var buckets = new List<HistogramBucket>();

            for (var i = 0; i <= edges.Length; i++)
            {
                var from = i == 0 ? 0 : edges[i - 1];
                double? to = i == edges.Length ? null : edges[i];
                var label = i == 0 ? "< " + format(edges[0])
                    : i == edges.Length ? format(edges[^1]) + "+"
                    : format(edges[i - 1]) + " – " + format(edges[i]);

                buckets.Add(new HistogramBucket(label, from, to, counts[i]));
            }

            return buckets;
        }

        public static string Minutes(double minutes) => minutes switch
        {
            < 60 => (int)minutes + " min",
            < 1440 => Math.Round(minutes / 60, 1).ToString("0.#") + " h",
            _ => Math.Round(minutes / 1440, 1).ToString("0.#") + " d"
        };
    }
}
