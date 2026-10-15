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

using System.ComponentModel;
using Jube.AskJooby.Tools.Charting.Interfaces;
using Jube.AskJooby.Charting.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.Export.Interfaces;

namespace Jube.AskJooby.Tools.Charting
{
    public sealed class RenderChartTool(
        IToolResultCache cache,
        IChartRenderer renderer,
        IFileExportClient exportClient,
        IExportSink exportSink)
    {
        [Description(
            "Draws a real chart from a table you already cached with RunSqlQuery, and attaches it to " +
            "this turn's reply where the person will see it -- attachment is turn-wide, so a chart " +
            "drawn by a spawned Joobrik reaches them without the agent that spawned it redrawing it. " +
            "ALWAYS use this instead of writing SVG, ASCII " +
            "art, or chart markup yourself: it draws from the cached rows directly, so the picture " +
            "always matches the data and none of the data has to pass through you to get there. You " +
            "name the pointer and say which column is the category and which holds the values; you " +
            "never need the rows themselves, so do not read them first -- call InspectDataSchema if " +
            "you are unsure what the columns are called. You do not get the picture back and cannot " +
            "see it; once this returns, just say what the chart shows and let it appear on your " +
            "reply. Aggregate in SQL before charting -- one bar per row, so a chart of raw " +
            "transactions is unreadable where a chart of counts per day is not.")]
        public async Task<string> RenderChart(
            [Description("The data_pointer returned when the table was cached.")]
            string dataPointer,
            [Description(
                "One of: column, bar, line, area, pie, donut, scatter, histogram. \"column\" is vertical " +
                "bars and the safe default for a count or total per category; \"bar\" is horizontal, " +
                "better when the category names are long; \"line\" and \"area\" suit something ordered " +
                "over time; \"pie\" and \"donut\" suit a handful of parts of one whole; \"scatter\" " +
                "plots one numeric column against another, so for it the category column must itself be " +
                "numeric. \"histogram\" is the odd one out and the one to reach for when the question is " +
                "what a spread of numbers LOOKS like rather than what each row is: it takes the raw values " +
                "of the value column, works out the bins itself and draws the distribution, so it needs no " +
                "category column at all and is happy with far more rows than the others. Name several " +
                "value columns to overlay several distributions, which is how a long baseline and a recent " +
                "window are compared by eye.")]
            string chartType,
            [Description(
                "The column whose values label each point along the bottom -- the day, the rule name, " +
                "the status. For a scatter chart this is the numeric x axis instead. Ignored for a " +
                "histogram, which labels its own axis with the bins it worked out, so leave it empty " +
                "there.")]
            string categoryColumn,
            [Description(
                "The numeric column, or comma separated columns, to plot. Several columns draw several " +
                "series on the same chart.")]
            string valueColumns,
            [Description("A short title for the chart. Optional.")]
            string? title = null,
            [Description(
                "Histogram only, and optional even there: how many bins to divide the values into. Left " +
                "unset the chart chooses from the data, which is almost always better. Set it only when a " +
                "bin edge has to land somewhere particular -- a reporting threshold being the case that " +
                "matters, because a default binning will straddle it and hide the very thing being looked " +
                "for.")]
            int? bins = null)
        {
            var table = await cache.GetAsync(dataPointer);

            if (table is null)
            {
                return "No cached table matches that pointer -- it may have expired, or never existed.";
            }

            if (string.IsNullOrWhiteSpace(valueColumns))
            {
                return "No value column was named, so there is nothing to plot. Name the numeric "
                       + "column to chart, or several separated by commas.";
            }

            var columns = valueColumns
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            byte[] svg;

            try
            {
                svg = renderer.Render(table,
                    new ChartSpecification(chartType, categoryColumn, columns, title, Bins: bins));
            }
            catch (ArgumentException e)
            {
                return $"Could not draw that chart: {e.Message}";
            }

            var safeName = SanitizeFileName(string.IsNullOrWhiteSpace(title) ? "chart" : title) + ".svg";
            var fileId = await exportClient.ExportAsync(svg, safeName, "image/svg+xml");
            exportSink.Record(fileId, safeName);

            var drawn = chartType.Trim().ToLowerInvariant() == "histogram"
                ? $"Drew a histogram of {table.Rows.Count} value(s) of {string.Join(", ", columns)}."
                : $"Drew a {chartType.Trim().ToLowerInvariant()} chart of {table.Rows.Count} row(s) from "
                  + $"\"{categoryColumn}\" against {string.Join(", ", columns)}.";

            return drawn + " It is attached to this "
                         + "turn's reply to the person, whichever agent drew it -- a chart drawn inside a "
                         + "spawned Joobrik reaches them exactly the same way, so if one reports back that it "
                         + "has already drawn something, do not draw it again. Describe what it shows in "
                         + "words; do not try to reproduce it.";
        }

        private static string SanitizeFileName(string name)
        {
            var cleaned = new string(name
                    .Select(character =>
                        char.IsLetterOrDigit(character) || character is '-' or '_' or ' ' ? character : '-')
                    .ToArray())
                .Trim()
                .Replace(' ', '-');

            while (cleaned.Contains("--", StringComparison.Ordinal))
            {
                cleaned = cleaned.Replace("--", "-", StringComparison.Ordinal);
            }

            cleaned = cleaned.Trim('-');

            return string.IsNullOrWhiteSpace(cleaned)
                ? "chart"
                : cleaned[..Math.Min(cleaned.Length, 60)];
        }
    }
}