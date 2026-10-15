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
using System.Globalization;
using System.Text;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.Export.Interfaces;

namespace Jube.AskJooby.Tools.Export
{
    public sealed class ExportCachedTableTool(
        IToolResultCache cache,
        IFileExportClient exportClient,
        IExportSink exportSink)
    {
        private const long MaxExportBytes = 10 * 1024 * 1024;

        [Description(
            "Exports a table you already cached with RunSqlQuery as a downloadable CSV file, attached " +
            "directly to your reply -- use this whenever the caller asks to download, export, or save " +
            "results, instead of pasting the data into your answer as text. The file is built straight " +
            "from the cached rows, not from anything you write, so it always exactly matches what's in " +
            "the cache. You don't get the file's contents back and can't render it yourself -- once this " +
            "returns, just tell the caller it's ready; the download appears on your reply automatically " +
            "when you finish.")]
        public async Task<string> ExportCachedTableToCsv(
            [Description("The data_pointer returned when the table was cached.")]
            string dataPointer,
            [Description(
                "A short, descriptive file name for what this export is, with no extension -- \".csv\" is " +
                "added automatically.")]
            string fileName)
        {
            var table = await cache.GetAsync(dataPointer);

            if (table is null)
            {
                return "No cached table matches that pointer -- it may have expired, or never existed.";
            }

            if (table.Rows.Count == 0)
            {
                return "That cached table has no rows -- nothing to export.";
            }

            var csvBytes = BuildCsvBytes(table);

            if (csvBytes.LongLength > MaxExportBytes)
            {
                return $"That table is too large to export as one file ({csvBytes.LongLength:N0} bytes, limit " +
                       $"{MaxExportBytes:N0}). Narrow it down with QueryCachedData first, then export that.";
            }

            var safeName = SanitizeFileName(fileName) + ".csv";
            var fileId = await exportClient.ExportAsync(csvBytes, safeName, "text/csv");
            exportSink.Record(fileId, safeName);

            return $"Exported {table.Rows.Count} row(s), {table.Columns.Count} column(s) to \"{safeName}\" -- " +
                   "it will show up as a download attachment on your reply. Don't repeat the data in your " +
                   "answer; just confirm to the caller that it's ready to download.";
        }

        private static byte[] BuildCsvBytes(CachedTable table)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", table.Columns.Select(c => EscapeCsvField(c.Name))));

            foreach (var row in table.Rows)
            {
                var fields = table.Columns.Select(c =>
                    EscapeCsvField(FormatCsvValue(row.GetValueOrDefault(c.Name))));
                builder.AppendLine(string.Join(",", fields));
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(builder.ToString());
            var result = new byte[preamble.Length + body.Length];
            preamble.CopyTo(result, 0);
            body.CopyTo(result, preamble.Length);

            return result;
        }

        private static string FormatCsvValue(object? value)
        {
            return value switch
            {
                null => string.Empty,
                double d => d.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString(CultureInfo.InvariantCulture),
                DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }

        private static string EscapeCsvField(string field)
        {
            if (field.IndexOfAny([',', '"', '\n', '\r']) < 0)
            {
                return field;
            }

            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        private static string SanitizeFileName(string fileName)
        {
            var trimmed = fileName.Trim();

            if (trimmed.Length == 0)
            {
                trimmed = "export";
            }

            var invalid = Path.GetInvalidFileNameChars();

            var cleaned = new string(trimmed.Select(c => invalid.Contains(c) || c is '/' or '\\' ? '_' : c)
                .ToArray());

            return cleaned.Length > 100 ? cleaned[..100] : cleaned;
        }
    }
}