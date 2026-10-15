/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using System.Text;
using Jube.AskJooby.Charting.Models;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.Tools.Charting.Interfaces;
using Jube.Charting;
using Jube.Charting.Core;
using Jube.Charting.Core.Models;

namespace Jube.Service.AskJoobyCharting
{
    public sealed class AskJoobyChartRenderer : IChartRenderer
    {
        private const int MaximumCategories = 2000;
        private const double Width = 760;
        private const double Height = 420;
        private const int MaximumHistogramValues = 200000;

        private static readonly string[] chartTypes =
            ["column", "bar", "line", "area", "pie", "donut", "scatter", "histogram"];

        public IReadOnlyList<string> SupportedChartTypes => chartTypes;

        public byte[] Render(CachedTable table, ChartSpecification specification)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(specification);

            if (table.Rows.Count == 0)
            {
                throw new ArgumentException("That cached table has no rows, so there is nothing to plot.");
            }

            var type = specification.ChartType.Trim().ToLowerInvariant();

            if (!chartTypes.Contains(type))
            {
                throw new ArgumentException(
                    $"\"{specification.ChartType}\" is not a chart type. Choose one of: "
                    + string.Join(", ", chartTypes) + ".");
            }

            var histogram = type == "histogram";

            if (table.Rows.Count > (histogram ? MaximumHistogramValues : MaximumCategories))
            {
                throw new ArgumentException(histogram
                    ? $"That table has {table.Rows.Count} rows, over the {MaximumHistogramValues} this will "
                      + "bin. Filter it down, or aggregate in SQL first."
                    : $"That table has {table.Rows.Count} rows, over the {MaximumCategories} a readable chart "
                      + "can carry. Aggregate it in SQL, or filter it down, before charting it.");
            }

            var categoryColumn = histogram
                ? string.Empty
                : ResolveColumn(table, specification.CategoryColumn, "categoryColumn");

            if (specification.ValueColumns is null || specification.ValueColumns.Count == 0)
            {
                throw new ArgumentException("At least one value column is needed to plot anything.");
            }

            var valueColumns = specification.ValueColumns
                .Select(name => ResolveColumn(table, name, "valueColumns"))
                .ToList();

            var svg = type switch
            {
                "pie" => RenderPie(table, categoryColumn, valueColumns[0], specification, false),
                "donut" => RenderPie(table, categoryColumn, valueColumns[0], specification, true),
                "scatter" => RenderScatter(table, categoryColumn, valueColumns, specification),
                "histogram" => RenderHistogram(table, valueColumns, specification),
                _ => RenderCategorical(table, categoryColumn, valueColumns, specification, type)
            };

            return Encoding.UTF8.GetBytes(svg);
        }

        private static string RenderCategorical(CachedTable table, string categoryColumn,
            IReadOnlyList<string> valueColumns, ChartSpecification specification, string type)
        {
            var categories = table.Rows.Select(row => Describe(Value(row, categoryColumn))).ToList();

            switch (type)
            {
                case "line":
                {
                    var chart = Chart.Line().Categories(categories);

                    foreach (var column in valueColumns)
                    {
                        chart = chart.Series(column, Numbers(table, column));
                    }

                    return Finish(chart.Size(Width, Height), specification, categoryColumn, valueColumns);
                }

                case "area":
                {
                    var chart = Chart.Area().Categories(categories);

                    foreach (var column in valueColumns)
                    {
                        chart = chart.Series(column, Numbers(table, column));
                    }

                    return Finish(chart.Size(Width, Height), specification, categoryColumn, valueColumns);
                }

                default:
                {
                    var chart = type == "bar"
                        ? Chart.Bar().Categories(categories)
                        : Chart.Column().Categories(categories);

                    foreach (var column in valueColumns)
                    {
                        chart = chart.Series(column, Numbers(table, column));
                    }

                    return Finish(chart.Size(Width, Height), specification, categoryColumn, valueColumns);
                }
            }
        }

        private static string RenderPie(CachedTable table, string categoryColumn, string valueColumn,
            ChartSpecification specification, bool donut)
        {
            var labels = table.Rows.Select(row => Describe(Value(row, categoryColumn))).ToList();
            var chart = Chart.Pie().Slices(labels, Numbers(table, valueColumn)).Size(Width, Height);

            if (donut)
            {
                chart = chart.Donut();
            }

            return Finish(chart, specification, categoryColumn, [valueColumn]);
        }

        private static string RenderScatter(CachedTable table, string xColumn,
            IReadOnlyList<string> valueColumns, ChartSpecification specification)
        {
            var xs = Numbers(table, xColumn);
            var chart = Chart.Scatter().Size(Width, Height);

            foreach (var column in valueColumns)
            {
                chart = chart.Series(column, xs, Numbers(table, column));
            }

            return Finish(chart, specification, xColumn, valueColumns);
        }

        private static string RenderHistogram(CachedTable table, IReadOnlyList<string> valueColumns,
            ChartSpecification specification)
        {
            var chart = Chart.Histogram();

            foreach (var column in valueColumns)
            {
                chart = chart.Values(column, Numbers(table, column));
            }

            if (specification.Bins is > 0)
            {
                chart = chart.BinCount(specification.Bins.Value);
            }

            return chart.Size(Width, Height)
                .Title(specification.Title)
                .XAxisTitle(specification.CategoryAxisTitle ?? string.Join(", ", valueColumns))
                .YAxisTitle(specification.ValueAxisTitle ?? "Count")
                .Theme(ThemeMode.Light)
                .Interactive(false)
                .StyleMode(ChartStyleMode.Inline)
                .Render();
        }

        private static string Finish<T>(T chart, ChartSpecification specification, string categoryColumn,
            IReadOnlyList<string> valueColumns) where T : ChartBuilder<T>
        {
            return chart
                .Title(specification.Title)
                .XAxisTitle(specification.CategoryAxisTitle ?? categoryColumn)
                .YAxisTitle(specification.ValueAxisTitle ?? string.Join(", ", valueColumns))
                .Theme(ThemeMode.Light)
                .Interactive(false)
                .StyleMode(ChartStyleMode.Inline)
                .Render();
        }

        private static string ResolveColumn(CachedTable table, string? requested, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(requested))
            {
                throw new ArgumentException($"{parameterName} was not supplied.");
            }

            var match = table.Columns.FirstOrDefault(column =>
                string.Equals(column.Name, requested, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                throw new ArgumentException(
                    $"\"{requested}\" is not a column in that table. Its columns are: "
                    + string.Join(", ", table.Columns.Select(column => column.Name)) + ".");
            }

            return match.Name;
        }

        private static IReadOnlyList<double> Numbers(CachedTable table, string column)
        {
            var values = new List<double>(table.Rows.Count);
            var unusable = 0;

            foreach (var row in table.Rows)
            {
                if (TryToDouble(Value(row, column), out var number))
                {
                    values.Add(number);
                }
                else
                {
                    values.Add(0);
                    unusable++;
                }
            }

            if (unusable == table.Rows.Count)
            {
                throw new ArgumentException(
                    $"No value in \"{column}\" could be read as a number, so it cannot be plotted. "
                    + "Pick a numeric column, or aggregate to one in SQL.");
            }

            return values;
        }

        private static object? Value(IReadOnlyDictionary<string, object?> row, string column)
        {
            return row.TryGetValue(column, out var value) ? value : null;
        }

        private static bool TryToDouble(object? value, out double number)
        {
            switch (value)
            {
                case null:
                    number = 0;

                    return false;
                case double d:
                    number = d;

                    return !double.IsNaN(d) && !double.IsInfinity(d);
                case bool b:
                    number = b ? 1 : 0;

                    return true;
                default:
                    return double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out number);
            }
        }

        private static string Describe(object? value)
        {
            return value switch
            {
                null => string.Empty,
                double d => d.ToString(CultureInfo.InvariantCulture),
                DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }
    }
}