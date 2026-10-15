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

using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery;

namespace Jube.Engine.Exhaustive.Storage
{
    internal static class TrainingDataStore
    {
        internal static async Task StoreDataAsync(DbContext dbContext, double[] outputs,
            GetNextExhaustiveSearchInstanceQueryDto exhaustiveSearchInstance, double[][] data,
            double[] cloneOfOutputsFromAnomaly, CancellationToken token = default)
        {
            var exhaustiveSearchInstanceDataRepository = new ExhaustiveSearchInstanceDataRepository(dbContext);

            for (var i = 0; i < outputs.Length; i++)
            {
                var exhaustiveSearchInstanceData = new ExhaustiveSearchInstanceData
                {
                    Dependent = outputs[i],
                    ExhaustiveSearchInstanceId = exhaustiveSearchInstance.Id,
                    Independent = data[i],
                    Anomaly = cloneOfOutputsFromAnomaly != null ? cloneOfOutputsFromAnomaly[i] : 0
                };

                await exhaustiveSearchInstanceDataRepository.InsertAsync(exhaustiveSearchInstanceData, token)
                    .ConfigureAwait(false);
            }
        }
    }
}