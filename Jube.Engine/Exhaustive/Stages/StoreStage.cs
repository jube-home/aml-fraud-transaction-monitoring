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

using System.Threading.Tasks;
using Jube.Engine.Exhaustive.Stages.Models;
using Jube.Engine.Exhaustive.Storage;
using log4net;

namespace Jube.Engine.Exhaustive.Stages
{
    internal class StoreStage(ILog log)
    {
        internal async Task<bool> ExecuteAsync(InstanceRun run)
        {
            var exhaustiveSearchInstance = run.Instance;
            var dbContext = run.DbContext;
            var repositoryExhaustiveSearchInstance = run.RepositoryInstance;
            var token = run.Token;
            var data = run.Data;
            var outputs = run.Outputs;
            var cloneOfOutputsFromAnomaly = run.CloneOfOutputsFromAnomaly;

            if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                    exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
            {
                return false;
            }

            await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 12, token)
                .ConfigureAwait(false);

            await TrainingDataStore.StoreDataAsync(dbContext, outputs, exhaustiveSearchInstance, data,
                cloneOfOutputsFromAnomaly,
                token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info("Has stored final training data.  Updating status to 13 for correlation analysis.");
            }

            return true;
        }
    }
}