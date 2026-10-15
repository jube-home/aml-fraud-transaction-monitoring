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
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using log4net;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    public sealed class AdvancedAnalyticsTrainingDataPointerReader(
        string connectionString,
        int tenantRegistryId,
        string userName,
        ILog log) : IAdvancedAnalyticsTrainingDataPointerReader
    {
        public async Task<(double[][] Inputs, int[] Labels)> ResolveAsync(Guid pointer, int labelColumn,
            int variableCount, CancellationToken token)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                connectionString, log);

            var repository = new AdvancedAnalyticsCacheRepository(dbContext, tenantRegistryId, userName,
                new AdvancedAnalyticsCacheLimits());

            var entry = await repository.GetAsync(pointer, token).ConfigureAwait(false);

            if (entry == null)
            {
                throw new InvalidOperationException(
                    "The cached input has expired or does not exist. Put the rows in the cache again and try again.");
            }

            var values = AdvancedAnalyticsPacking.Unpack(entry.Data, entry.PackingVersion);

            return CachedTableRows.Split(values, entry.Columns, entry.Rows, labelColumn,
                variableCount, token);
        }
    }
}