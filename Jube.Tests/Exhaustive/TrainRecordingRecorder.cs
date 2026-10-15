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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Engine.Exhaustive.Interfaces;

namespace Jube.Test.Exhaustive
{
    internal sealed class TrainRecordingRecorder : ITrialRecorder
    {
        private int nextId;

        public bool StopFromStart { get; set; }
        public int StopAfterTrialInstances { get; set; } = int.MaxValue;
        public Func<int, Exception?>? FailOnTrialInstance { get; set; }
        public Func<int, Exception?>? FailOnDeleteTrialVariables { get; set; }
        public Func<ExhaustiveSearchInstancePromotedTrialInstanceRoc, Exception?>? FailOnRoc { get; set; }

        public List<string> Events { get; } = [];
        public List<ExhaustiveSearchInstanceTrialInstance> TrialInstances { get; } = [];
        public List<ExhaustiveSearchInstanceTrialInstanceTopologyTrial> TopologyTrials { get; } = [];

        public List<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial> ActivationFunctionTrials { get; } =
            [];

        public List<int> DeletedTrialVariableSets { get; } = [];
        public List<ExhaustiveSearchInstanceTrialInstanceVariable> TrialVariables { get; } = [];
        public List<(int VariableId, int TrialInstanceId)> RemovedVariables { get; } = [];
        public List<ExhaustiveSearchInstanceTrialInstanceSensitivity> TrialSensitivities { get; } = [];
        public List<ExhaustiveSearchInstancePromotedTrialInstance> Promoted { get; } = [];
        public List<ExhaustiveSearchInstancePromotedTrialInstanceRoc> Roc { get; } = [];
        public List<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual> PredictedActual { get; } = [];
        public List<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity> PromotedSensitivities { get; } = [];
        public List<ExhaustiveSearchInstancePromotedTrialInstanceVariable> Prescriptions { get; } = [];
        public List<(double Score, int Complexity)> BestScoreUpdates { get; } = [];
        public List<int> ModelsUpdates { get; } = [];
        public List<int> ModelsSinceBestUpdates { get; } = [];

        public List<(int Id, AdvancedAnalyticsModelKind Kind, double Score, string Json, string MetricsJson,
                string VariablesJson)>
            TrialOutcomes { get; } = [];

        public List<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue>
            PredictedActualVariableValues { get; } = [];

        public List<ExhaustiveSearchInstancePrescriptionHistogram>
            PrescriptionHistograms { get; } =
            [];

        public Task<bool> IsStopRequestedAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Events.Add("stop-check");

            return Task.FromResult(StopFromStart || TrialInstances.Count >= StopAfterTrialInstances);
        }

        public Task<ExhaustiveSearchInstanceTrialInstance> InsertTrialInstanceAsync(
            ExhaustiveSearchInstanceTrialInstance model, CancellationToken token = default)
        {
            var failure = FailOnTrialInstance?.Invoke(TrialInstances.Count + 1);

            if (failure != null)
            {
                TrialInstances.Add(model);
                model.Id = ++nextId;
                throw failure;
            }

            model.Id = ++nextId;
            TrialInstances.Add(model);
            Events.Add("trial");

            return Task.FromResult(model);
        }

        public Task UpdateTrialOutcomeAsync(int id, AdvancedAnalyticsModelKind kind, double score, string json,
            string metricsJson, string variablesJson, CancellationToken token = default)
        {
            TrialOutcomes.Add((id, kind, score, json, metricsJson, variablesJson));

            return Task.CompletedTask;
        }

        public Task<ExhaustiveSearchInstanceTrialInstanceTopologyTrial> InsertTopologyTrialAsync(
            ExhaustiveSearchInstanceTrialInstanceTopologyTrial model, CancellationToken token = default)
        {
            model.Id = ++nextId;
            TopologyTrials.Add(Snapshot(model));
            Events.Add(model.Finalisation == 1 ? "finalisation" : "topology");

            return Task.FromResult(model);
        }

        public Task<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial> InsertActivationFunctionTrialAsync(
            ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial model, CancellationToken token = default)
        {
            model.Id = ++nextId;
            ActivationFunctionTrials.Add(Snapshot(model));
            Events.Add("activation");

            return Task.FromResult(model);
        }

        public Task DeleteTrialVariablesAsync(int exhaustiveSearchInstanceTrialInstanceId,
            CancellationToken token = default)
        {
            DeletedTrialVariableSets.Add(exhaustiveSearchInstanceTrialInstanceId);
            var failure = FailOnDeleteTrialVariables?.Invoke(DeletedTrialVariableSets.Count);

            if (failure != null)
            {
                throw failure;
            }

            Events.Add("delete-variables");

            return Task.CompletedTask;
        }

        public Task<ExhaustiveSearchInstanceTrialInstanceVariable> InsertTrialVariableAsync(
            ExhaustiveSearchInstanceTrialInstanceVariable model, CancellationToken token = default)
        {
            model.Id = ++nextId;
            TrialVariables.Add(model);
            Events.Add("variable");

            return Task.FromResult(model);
        }

        public Task MarkTrialVariableRemovedAsync(int exhaustiveSearchInstanceVariableId,
            int exhaustiveSearchInstanceTrialInstanceId, CancellationToken token = default)
        {
            RemovedVariables.Add((exhaustiveSearchInstanceVariableId, exhaustiveSearchInstanceTrialInstanceId));
            Events.Add("removed");

            return Task.CompletedTask;
        }

        public Task InsertTrialSensitivityAsync(ExhaustiveSearchInstanceTrialInstanceSensitivity model,
            CancellationToken token = default)
        {
            TrialSensitivities.Add(model);
            Events.Add("trial-sensitivity");

            return Task.CompletedTask;
        }

        public Task<ExhaustiveSearchInstancePromotedTrialInstance> InsertPromotedAsync(
            ExhaustiveSearchInstancePromotedTrialInstance model, CancellationToken token = default)
        {
            model.Id = ++nextId;
            Promoted.Add(model);
            Events.Add("promoted");

            return Task.FromResult(model);
        }

        public Task InsertRocAsync(ExhaustiveSearchInstancePromotedTrialInstanceRoc model,
            CancellationToken token = default)
        {
            var failure = FailOnRoc?.Invoke(model);

            if (failure != null)
            {
                throw failure;
            }

            Roc.Add(model);
            Events.Add("roc");

            return Task.CompletedTask;
        }

        public Task InsertPredictedActualAsync(ExhaustiveSearchInstancePromotedTrialInstancePredictedActual model,
            CancellationToken token = default)
        {
            PredictedActual.Add(model);
            Events.Add("predicted-actual");

            return Task.CompletedTask;
        }

        public Task InsertPredictedActualVariableValueAsync(
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue model,
            CancellationToken token = default)
        {
            PredictedActualVariableValues.Add(model);
            Events.Add("predicted-actual-variable-value");

            return Task.CompletedTask;
        }

        public Task InsertPromotedSensitivityAsync(ExhaustiveSearchInstancePromotedTrialInstanceSensitivity model,
            CancellationToken token = default)
        {
            PromotedSensitivities.Add(model);
            Events.Add("promoted-sensitivity");

            return Task.CompletedTask;
        }

        public Task<ExhaustiveSearchInstancePromotedTrialInstanceVariable> InsertPromotedVariableAsync(
            ExhaustiveSearchInstancePromotedTrialInstanceVariable model, CancellationToken token = default)
        {
            model.Id = ++nextId;
            Prescriptions.Add(model);
            Events.Add("prescription");

            return Task.FromResult(model);
        }

        public Task InsertPromotedVariableHistogramAsync(
            ExhaustiveSearchInstancePrescriptionHistogram model,
            CancellationToken token = default)
        {
            PrescriptionHistograms.Add(model);
            Events.Add("prescription-histogram");

            return Task.CompletedTask;
        }

        public Task UpdateBestScoreAsync(double score, int topologyComplexity, CancellationToken token = default)
        {
            BestScoreUpdates.Add((score, topologyComplexity));
            Events.Add("best-score");

            return Task.CompletedTask;
        }

        public Task UpdateModelsAsync(int models, CancellationToken token = default)
        {
            ModelsUpdates.Add(models);
            Events.Add("models");

            return Task.CompletedTask;
        }

        public Task UpdateModelsSinceBestAsync(int modelsSinceBest, CancellationToken token = default)
        {
            ModelsSinceBestUpdates.Add(modelsSinceBest);
            Events.Add("models-since-best");

            return Task.CompletedTask;
        }

        private static T Snapshot<T>(T value) where T : class
        {
            return (T)typeof(object).GetMethod("MemberwiseClone",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(value, null)!;
        }
    }
}