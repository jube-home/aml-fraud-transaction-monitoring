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
using Jube.Engine.Exhaustive.Statistics;
using log4net;

namespace Jube.Engine.Exhaustive.Stages
{
    internal class BaseStatisticsStage(ILog log)
    {
        internal async Task<bool> ExecuteAsync(InstanceRun run)
        {
            var exhaustiveSearchInstance = run.Instance;
            var repositoryExhaustiveSearchInstance = run.RepositoryInstance;
            var token = run.Token;
            var data = run.Data;
            var variables = run.Variables;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Fetched data and there are {data.Length} records " +
                    $"for {exhaustiveSearchInstance.Id} updating status to 2 for calculating base statistics.");
            }

            if (data.Length == 0)
            {
                log.Error(
                    $"Exhaustive Training: no data was returned for Exhaustive Search Instance " +
                    $"{exhaustiveSearchInstance.Id} by its configured data source. Check the data source " +
                    "selected on the instance (Archive filter, Demonstration dataset or Uploaded dataset) " +
                    "actually has rows for this tenant. Setting status 20 and stopping this run without " +
                    "calculating statistics.");

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 20, token)
                    .ConfigureAwait(false);

                return false;
            }

            if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                    exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
            {
                return false;
            }

            await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 2, token)
                .ConfigureAwait(false);

            await CategoricalFieldStore.PersistAsync(run).ConfigureAwait(false);

            await new StatisticsCalculator(log).CalculateStatisticsAsync(exhaustiveSearchInstance.Id, variables, data,
                run.RepositoryVariables,
                run.RepositoryVariableHistograms, token).ConfigureAwait(false);

            return true;
        }
    }
}