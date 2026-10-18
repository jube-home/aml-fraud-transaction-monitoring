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
using System.Threading.Tasks;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Stages.Models;
using Jube.Engine.Helpers;
using log4net;

namespace Jube.Engine.Exhaustive.Stages
{
    internal class SupervisedStage(
        ILog log,
        DynamicEnvironment.DynamicEnvironment environment,
        JsonSerializationHelper jsonSerializationHelper,
        Random seeded)
    {
        internal async Task<bool> ExecuteAsync(InstanceRun run)
        {
            var exhaustiveSearchInstance = run.Instance;
            var dbContext = run.DbContext;
            var repositoryExhaustiveSearchInstance = run.RepositoryInstance;
            var token = run.Token;
            var data = run.Data;
            var variables = run.Variables;
            var outputs = run.Outputs;

            if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                    exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
            {
                return false;
            }

            await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 15, token)
                .ConfigureAwait(false);

            var supervised = new Supervised(ExhaustiveTrainingSettings.FromEnvironment(environment),
                new DbTrialRecorder(dbContext, repositoryExhaustiveSearchInstance, exhaustiveSearchInstance.Id),
                exhaustiveSearchInstance.Id, jsonSerializationHelper,
                seeded,
                variables, data, outputs, log,
                BaseRateResolution.WithOverride(exhaustiveSearchInstance.BaseRate, run.BaseRate),
                ExhaustiveCalibrationRequest.OrDefault(exhaustiveSearchInstance.CalibrationMethod),
                run.SyntheticData, run.SyntheticOutputs,
                exhaustiveSearchInstance.ChallengerModelBaseProbability);

            if (await supervised.TrainAsync(token).ConfigureAwait(false))
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Finished training {exhaustiveSearchInstance.Id} " +
                        $"updating status to 16 for finished.");
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 16, token)
                    .ConfigureAwait(false);
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Stopped training {exhaustiveSearchInstance.Id} " +
                        $"updating status to 19 for stopped.");
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 19, token)
                    .ConfigureAwait(false);
            }

            return true;
        }
    }
}