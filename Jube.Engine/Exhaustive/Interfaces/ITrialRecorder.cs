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
using Jube.Data.Poco;
using Jube.Data.Repository.Models;

namespace Jube.Engine.Exhaustive.Interfaces
{
    public interface ITrialRecorder
    {
        Task<bool> IsStopRequestedAsync(CancellationToken token = default);

        Task<ExhaustiveSearchInstanceTrialInstance> InsertTrialInstanceAsync(
            ExhaustiveSearchInstanceTrialInstance model, CancellationToken token = default);

        Task UpdateTrialOutcomeAsync(int id, AdvancedAnalyticsModelKind kind, double score, string json,
            string metricsJson, string variablesJson, CancellationToken token = default);

        Task<ExhaustiveSearchInstanceTrialInstanceTopologyTrial> InsertTopologyTrialAsync(
            ExhaustiveSearchInstanceTrialInstanceTopologyTrial model, CancellationToken token = default);

        Task<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial> InsertActivationFunctionTrialAsync(
            ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial model, CancellationToken token = default);

        Task DeleteTrialVariablesAsync(int exhaustiveSearchInstanceTrialInstanceId, CancellationToken token = default);

        Task<ExhaustiveSearchInstanceTrialInstanceVariable> InsertTrialVariableAsync(
            ExhaustiveSearchInstanceTrialInstanceVariable model, CancellationToken token = default);

        Task MarkTrialVariableRemovedAsync(int exhaustiveSearchInstanceVariableId,
            int exhaustiveSearchInstanceTrialInstanceId, CancellationToken token = default);

        Task InsertTrialSensitivityAsync(ExhaustiveSearchInstanceTrialInstanceSensitivity model,
            CancellationToken token = default);

        Task<ExhaustiveSearchInstancePromotedTrialInstance> InsertPromotedAsync(
            ExhaustiveSearchInstancePromotedTrialInstance model, CancellationToken token = default);

        Task InsertRocAsync(ExhaustiveSearchInstancePromotedTrialInstanceRoc model,
            CancellationToken token = default);

        Task InsertPredictedActualAsync(ExhaustiveSearchInstancePromotedTrialInstancePredictedActual model,
            CancellationToken token = default);

        Task InsertPredictedActualVariableValueAsync(
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue model,
            CancellationToken token = default);

        Task InsertPromotedSensitivityAsync(ExhaustiveSearchInstancePromotedTrialInstanceSensitivity model,
            CancellationToken token = default);

        Task<ExhaustiveSearchInstancePromotedTrialInstanceVariable> InsertPromotedVariableAsync(
            ExhaustiveSearchInstancePromotedTrialInstanceVariable model, CancellationToken token = default);

        Task InsertPromotedVariableHistogramAsync(
            ExhaustiveSearchInstancePrescriptionHistogram model,
            CancellationToken token = default);

        Task UpdateBestScoreAsync(double score, int topologyComplexity, CancellationToken token = default);
        Task UpdateModelsAsync(int models, CancellationToken token = default);
        Task UpdateModelsSinceBestAsync(int modelsSinceBest, CancellationToken token = default);
    }
}