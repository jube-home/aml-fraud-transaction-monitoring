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

using Jube.Data.Reporting;

namespace Jube.Service.Charts.Datasource
{
    public static class DatasourceSeriesPivot
    {
        public sealed record Result(string[] Categories, List<(string Name, double[] Values)> Series);

        public static Result Pivot(DatasourceFrame frame, string categoryColumn, string seriesColumn,
            string valueColumn)
        {
            var categoryTexts = frame.Texts(categoryColumn);
            var seriesTexts = frame.Texts(seriesColumn);
            var values = frame.Doubles(valueColumn);

            var categories = new List<string>();
            var categoryIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            var names = new List<string>();
            var nameIndex = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var category in categoryTexts)
            {
                if (categoryIndex.TryAdd(category, categories.Count))
                {
                    categories.Add(category);
                }
            }

            foreach (var name in seriesTexts)
            {
                if (nameIndex.TryAdd(name, names.Count))
                {
                    names.Add(name);
                }
            }

            var grid = names.Select(_ => Enumerable.Repeat(double.NaN, categories.Count).ToArray()).ToList();
            var seen = new HashSet<(int, int)>();

            for (var row = 0; row < values.Length; row++)
            {
                var series = nameIndex[seriesTexts[row]];
                var category = categoryIndex[categoryTexts[row]];

                if (!seen.Add((series, category)))
                {
                    throw new DatasourceChartException(DatasourceMessages.PivotDuplicate, categoryTexts[row],
                        seriesTexts[row]);
                }

                grid[series][category] = values[row];
            }

            return new Result(categories.ToArray(), names.Select((n, i) => (n, grid[i])).ToList());
        }
    }
}
