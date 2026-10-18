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
using Jube.AskJooby.DataCache;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.DemoDataset.Interfaces;

namespace Jube.AskJooby.Tools.DemoDataset
{
    public sealed class DemoDatasetTool(IDemoDatasetProvider provider, IToolResultCache cache)
    {
        private const int PreviewRowCount = 3;

        [Description(
            "Loads a built-in demo dataset and caches it, returning a data_pointer exactly as " +
            "RunSqlQuery does, so everything that works on a queried table works on this one: " +
            "InspectDataSchema, PreviewDataRows, QueryCachedData and RenderChart. Use it whenever the " +
            "caller asks about \"the demo dataset\", or wants to try analysis or model training without " +
            "naming real data. It is a fixed, anonymous sample that belongs to nobody, so nothing in it " +
            "is a real customer or a real transaction and nothing about it describes this deployment. " +
            "Say which dataset you loaded and how big it is; do not list its rows.")]
        public async Task<string> GetDemoDataset(
            [Description(
                "Which demo dataset to load. \"transactions\" is a labelled card-transaction sample: one " +
                "row per transaction, a \"Dependent\" column that is 1 for fraud and 0 for genuine, and " +
                "the rest numeric features of the transaction and of the account's recent behaviour.")]
            string datasetName = "transactions")
        {
            CachedTable table;

            try
            {
                table = provider.Load(datasetName);
            }
            catch (ArgumentException e)
            {
                return e.Message;
            }

            var pointer = await cache.StoreAsync(table, "demo dataset: " + datasetName);

            var preview = table.Rows.Take(PreviewRowCount)
                .Select(row => CachedRowText.FormatFirst(table.Columns, row, 4));

            return $"Loaded the \"{datasetName}\" demo dataset: {table.Rows.Count} row(s), "
                   + $"{table.Columns.Count} column(s). data_pointer: \"{pointer}\". "
                   + $"Columns: {string.Join(", ", table.Columns.Select(column => column.Name))}. "
                   + $"First {Math.Min(PreviewRowCount, table.Rows.Count)} (first four columns only): "
                   + $"{string.Join(" | ", preview)}. "
                   + "Use InspectDataSchema/PreviewDataRows/QueryCachedData with this data_pointer to "
                   + "explore it, and RenderChart to draw from it. Aggregate before charting.";
        }
    }
}