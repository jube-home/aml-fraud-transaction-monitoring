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

using Jube.Analytics.Datasets;
using Jube.AskJooby.DataCache.Models;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Service.AskJoobyDataCache.Interfaces;

namespace Jube.Service.AskJoobyDataCache
{
    public sealed class AdvancedAnalyticsCachedTableReader(DbContext dbContext, int tenantRegistryId, string userName)
        : IAdvancedAnalyticsCachedTableReader
    {
        public async Task<CachedTable?> GetAsync(Guid pointer, CancellationToken token = default)
        {
            var repository = new AdvancedAnalyticsCacheRepository(dbContext, tenantRegistryId, userName);
            var entry = await repository.GetAsync(pointer, token).ConfigureAwait(false);

            if (entry == null)
            {
                return null;
            }

            var values = AdvancedAnalyticsPacking.Unpack(entry.Data, entry.PackingVersion);

            return ToCachedTable(values, entry.Rows, entry.Columns);
        }

        private static CachedTable ToCachedTable(double[] values, int rows, int columns)
        {
            var columnNames = new string[columns];

            for (var c = 0; c < columns; c++)
            {
                columnNames[c] = c == columns - 1 ? "Label" : $"Column{c}";
            }

            var cachedColumns = columnNames.Select(name => new CachedColumn(name, "Double"))
                .ToList();

            var cachedRows = new List<IReadOnlyDictionary<string, object?>>(rows);

            for (var r = 0; r < rows; r++)
            {
                var row = new Dictionary<string, object?>(columns);

                for (var c = 0; c < columns; c++)
                {
                    row[columnNames[c]] = values[r * columns + c];
                }

                cachedRows.Add(row);
            }

            return new CachedTable(cachedColumns, cachedRows);
        }
    }
}