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
using System.Reflection;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.Tools.DemoDataset.Interfaces;

namespace Jube.Service.DemoDataset
{
    public sealed class AskJoobyDemoDatasetProvider : IDemoDatasetProvider
    {
        private const string TransactionsDataset = "transactions";
        private const string ResourceName = "Jube.Service.DemoDataset.Mock.csv";
        private static readonly string[] datasets = [TransactionsDataset];
        private static CachedTable? cached;
        private static readonly Lock gate = new();
        public IReadOnlyList<string> AvailableDatasets => datasets;

        public CachedTable Load(string datasetName)
        {
            if (!string.Equals(datasetName, TransactionsDataset, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"\"{datasetName}\" is not a demo dataset. The ones that exist are: "
                    + string.Join(", ", datasets) + ".");
            }

            lock (gate)
            {
                return cached ??= Parse(ReadResource());
            }
        }

        private static string ReadResource()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(ResourceName)
                               ?? throw new InvalidOperationException(
                                   $"The demo dataset resource \"{ResourceName}\" is not embedded in "
                                   + "this build.");

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        private static CachedTable Parse(string content)
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.TrimEnd('\r'))
                .Where(line => line.Length > 0)
                .ToList();

            if (lines.Count < 2)
            {
                throw new InvalidOperationException("The demo dataset holds no rows.");
            }

            var headers = lines[0].Split(',').Select(header => header.Trim()).ToList();
            var rows = new List<IReadOnlyDictionary<string, object?>>(lines.Count - 1);

            foreach (var line in lines.Skip(1))
            {
                var fields = line.Split(',');
                var row = new Dictionary<string, object?>(headers.Count);

                for (var i = 0; i < headers.Count; i++)
                {
                    var raw = i < fields.Length ? fields[i].Trim() : string.Empty;

                    row[headers[i]] = double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture,
                        out var number)
                        ? number
                        : raw.Length == 0
                            ? null
                            : raw;
                }

                rows.Add(row);
            }

            var columns = headers.Select(header => new CachedColumn(header, "double")).ToList();

            return new CachedTable(columns, rows);
        }
    }
}