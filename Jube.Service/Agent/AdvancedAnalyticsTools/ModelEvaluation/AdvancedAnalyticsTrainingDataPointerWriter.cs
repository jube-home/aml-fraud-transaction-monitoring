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
using Jube.AskJooby.DataCache.Interfaces;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsTrainingData;
using Jube.Service.Repository.AdvancedAnalyticsTrainingData.Models;
using log4net;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    public sealed class AdvancedAnalyticsTrainingDataPointerWriter(
        string connectionString,
        int tenantRegistryId,
        string userName,
        ILog log,
        IToolResultCache toolCache) : IAdvancedAnalyticsTrainingDataPointerWriter
    {
        private const int MinimumRows = 2;

        public async Task<AdvancedAnalyticsTrainingDataDto> PutAsync(string dataPointer, string[] variableNames,
            string labelColumnName, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(dataPointer))
            {
                throw new ArgumentException("Name the data_pointer of a cached table.", nameof(dataPointer));
            }

            var table = await toolCache.GetAsync(dataPointer, token).ConfigureAwait(false);

            if (table == null)
            {
                throw new InvalidOperationException(
                    "No cached table matches that data_pointer. Run the query again to cache a fresh one.");
            }

            if (table.Rows.Count < MinimumRows)
            {
                throw new InvalidOperationException(
                    $"A table of {table.Rows.Count} row(s) cannot be split into training and test rows.");
            }

            var matrix = AdvancedAnalyticsTrainingMatrix.Build(table, variableNames, labelColumnName);

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                connectionString, log);

            var repository = new AdvancedAnalyticsCacheRepository(dbContext, tenantRegistryId, userName,
                new AdvancedAnalyticsCacheLimits());

            var entry = await repository.PutAsync(matrix.Packed, matrix.Rows, matrix.Columns, token: token)
                .ConfigureAwait(false);

            return new AdvancedAnalyticsTrainingDataDto
            {
                CachePointer = entry.Guid,
                Rows = entry.Rows,
                Columns = entry.Columns,
                LabelColumn = entry.Columns - 1,
                VariableNames = variableNames,
                LabelColumnName = labelColumnName,
                SizeBytes = entry.SizeBytes,
                ExpiryDate = entry.ExpiryDate,
                MissingValues = matrix.MissingValues,
                PositiveLabels = matrix.PositiveLabels
            };
        }
    }
}