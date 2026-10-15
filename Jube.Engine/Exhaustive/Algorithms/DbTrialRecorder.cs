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
using Jube.Data.Repository.Models;
using Jube.Engine.Exhaustive.Interfaces;

namespace Jube.Engine.Exhaustive.Algorithms
{
    public class DbTrialRecorder(
        DbContext dbContext,
        ExhaustiveSearchInstanceRepository repositoryExhaustiveSearchInstance,
        int exhaustiveSearchInstanceId) : ITrialRecorder
    {
        private readonly ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository
            activationFunctionTrialRepository = new(dbContext);

        private readonly ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository
            predictedActualRepository = new(dbContext);

        private readonly ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValueRepository
            predictedActualVariableValueRepository = new(dbContext);

        private readonly ExhaustiveSearchInstancePrescriptionHistogramRepository
            prescriptionHistogramRepository = new(dbContext);

        private readonly ExhaustiveSearchInstancePromotedTrialInstanceRepository promotedRepository = new(dbContext);

        private readonly ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository
            promotedSensitivityRepository = new(dbContext);

        private readonly ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository promotedVariableRepository =
            new(dbContext);

        private readonly ExhaustiveSearchInstancePromotedTrialInstanceRocRepository rocRepository = new(dbContext);

        private readonly ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository topologyTrialRepository =
            new(dbContext);

        private readonly ExhaustiveSearchInstanceTrialInstanceRepository trialInstanceRepository = new(dbContext);

        private readonly ExhaustiveSearchInstanceTrialInstanceSensitivityRepository trialSensitivityRepository =
            new(dbContext);

        private readonly ExhaustiveSearchInstanceTrialInstanceVariableRepository trialVariableRepository =
            new(dbContext);

        public async Task<bool> IsStopRequestedAsync(CancellationToken token = default)
        {
            return !await repositoryExhaustiveSearchInstance.IsStoppingOrStoppedAsync(exhaustiveSearchInstanceId, token)
                .ConfigureAwait(false);
        }

        public Task<ExhaustiveSearchInstanceTrialInstance> InsertTrialInstanceAsync(
            ExhaustiveSearchInstanceTrialInstance model, CancellationToken token = default)
        {
            return trialInstanceRepository.InsertAsync(model, token);
        }

        public Task UpdateTrialOutcomeAsync(int id, AdvancedAnalyticsModelKind kind, double score, string json,
            string metricsJson, string variablesJson, CancellationToken token = default)
        {
            return trialInstanceRepository.UpdateOutcomeAsync(id, kind, score, json, metricsJson, variablesJson,
                token);
        }

        public Task<ExhaustiveSearchInstanceTrialInstanceTopologyTrial> InsertTopologyTrialAsync(
            ExhaustiveSearchInstanceTrialInstanceTopologyTrial model, CancellationToken token = default)
        {
            return topologyTrialRepository.InsertAsync(model, token);
        }

        public Task<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial> InsertActivationFunctionTrialAsync(
            ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial model, CancellationToken token = default)
        {
            return activationFunctionTrialRepository.InsertAsync(model, token);
        }

        public Task DeleteTrialVariablesAsync(int exhaustiveSearchInstanceTrialInstanceId,
            CancellationToken token = default)
        {
            return trialVariableRepository.DeleteAllByExhaustiveSearchInstanceTrialInstanceIdAsync(
                exhaustiveSearchInstanceTrialInstanceId, token);
        }

        public Task<ExhaustiveSearchInstanceTrialInstanceVariable> InsertTrialVariableAsync(
            ExhaustiveSearchInstanceTrialInstanceVariable model, CancellationToken token = default)
        {
            return trialVariableRepository.InsertAsync(model, token);
        }

        public Task MarkTrialVariableRemovedAsync(int exhaustiveSearchInstanceVariableId,
            int exhaustiveSearchInstanceTrialInstanceId, CancellationToken token = default)
        {
            return trialVariableRepository.UpdateAsRemovedByExhaustiveSearchInstanceVariableIdAsync(
                exhaustiveSearchInstanceVariableId, exhaustiveSearchInstanceTrialInstanceId, token);
        }

        public Task InsertTrialSensitivityAsync(ExhaustiveSearchInstanceTrialInstanceSensitivity model,
            CancellationToken token = default)
        {
            return trialSensitivityRepository.InsertAsync(model, token);
        }

        public async Task<ExhaustiveSearchInstancePromotedTrialInstance> InsertPromotedAsync(
            ExhaustiveSearchInstancePromotedTrialInstance model, CancellationToken token = default)
        {
            if (model.Active == 1)
            {
                await promotedRepository.DeactivateAllForExhaustiveSearchInstanceAsync(exhaustiveSearchInstanceId,
                    token).ConfigureAwait(false);
            }

            return await promotedRepository.InsertAsync(model, token).ConfigureAwait(false);
        }

        public Task InsertRocAsync(ExhaustiveSearchInstancePromotedTrialInstanceRoc model,
            CancellationToken token = default)
        {
            return rocRepository.InsertAsync(model, token);
        }

        public Task InsertPredictedActualAsync(ExhaustiveSearchInstancePromotedTrialInstancePredictedActual model,
            CancellationToken token = default)
        {
            return predictedActualRepository.InsertAsync(model, token);
        }

        public Task InsertPredictedActualVariableValueAsync(
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue model,
            CancellationToken token = default)
        {
            return predictedActualVariableValueRepository.InsertAsync(model, token);
        }

        public Task InsertPromotedSensitivityAsync(ExhaustiveSearchInstancePromotedTrialInstanceSensitivity model,
            CancellationToken token = default)
        {
            return promotedSensitivityRepository.InsertAsync(model, token);
        }

        public Task<ExhaustiveSearchInstancePromotedTrialInstanceVariable> InsertPromotedVariableAsync(
            ExhaustiveSearchInstancePromotedTrialInstanceVariable model, CancellationToken token = default)
        {
            return promotedVariableRepository.InsertAsync(model, token);
        }

        public Task InsertPromotedVariableHistogramAsync(
            ExhaustiveSearchInstancePrescriptionHistogram model,
            CancellationToken token = default)
        {
            return prescriptionHistogramRepository.InsertAsync(model, token);
        }

        public Task UpdateBestScoreAsync(double score, int topologyComplexity, CancellationToken token = default)
        {
            return repositoryExhaustiveSearchInstance.UpdateBestScoreAsync(exhaustiveSearchInstanceId, score,
                topologyComplexity, token);
        }

        public Task UpdateModelsAsync(int models, CancellationToken token = default)
        {
            return repositoryExhaustiveSearchInstance.UpdateModelsAsync(exhaustiveSearchInstanceId, models, token);
        }

        public Task UpdateModelsSinceBestAsync(int modelsSinceBest, CancellationToken token = default)
        {
            return repositoryExhaustiveSearchInstance.UpdateModelsSinceBestAsync(exhaustiveSearchInstanceId,
                modelsSinceBest, token);
        }
    }
}