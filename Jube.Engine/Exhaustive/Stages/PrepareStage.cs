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

using System;
using System.Linq;
using System.Threading.Tasks;
using Jube.Engine.Exhaustive.Preparation;
using Jube.Engine.Exhaustive.Stages.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Stages
{
    internal class PrepareStage(ILog log, Random seeded)
    {
        internal async Task<bool> ExecuteAsync(InstanceRun run)
        {
            var exhaustiveSearchInstance = run.Instance;
            var repositoryExhaustiveSearchInstance = run.RepositoryInstance;
            var token = run.Token;
            var data = run.Data;
            var variables = run.Variables;
            var outputs = run.Outputs;
            var classCount = run.ClassCount;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Recalled Filter and found {classCount} further " +
                    $"class for {exhaustiveSearchInstance.Id} updating status to 9 for shuffling.");
            }

            if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                    exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
            {
                return false;
            }

            await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 9, token)
                .ConfigureAwait(false);

            if (classCount > 0)
            {
                DatasetShaper.Shuffle(new Random(), data, outputs, out data, out outputs);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Finished shuffling {exhaustiveSearchInstance.Id} " +
                        $"updating status to 10 for normalising.");
                }

                if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                        exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
                {
                    return false;
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 10, token)
                    .ConfigureAwait(false);

                data = new Normalisation(log).NormaliseData(variables, data);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Updating status to 11 for {exhaustiveSearchInstance.Id} " +
                        $"for over sampling.");
                }

                if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                        exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
                {
                    return false;
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 11, token)
                    .ConfigureAwait(false);

                var countPositive = (int)outputs.Sum();
                var countNegative = outputs.Length - countPositive;
                var majorityCount = Math.Max(countPositive, countNegative);
                var minorityCount = Math.Min(countPositive, countNegative);

                var targetPerClass = exhaustiveSearchInstance.Bootstrap
                    ? Math.Min(Math.Max(2, exhaustiveSearchInstance.BootstrapTo ?? 30000) / 2, majorityCount)
                    : minorityCount;

                new DatasetBalancer(log, seeded).DatasetSymmetry(data, outputs, targetPerClass, out data,
                    out outputs);

                run.BootstrapTargetPerClass = targetPerClass;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Has made the dataset symmetric for {exhaustiveSearchInstance.Id} " +
                        $"against a target per class of {targetPerClass} (majority available {majorityCount}), " +
                        $"updating status to 12 for storing training data.  " +
                        $"Dataset has a length of {outputs.Length} with {outputs.Sum()} class value of 1. Is about to store final training data.");
                }
            }

            run.Data = data;
            run.Outputs = outputs;

            return true;
        }
    }
}