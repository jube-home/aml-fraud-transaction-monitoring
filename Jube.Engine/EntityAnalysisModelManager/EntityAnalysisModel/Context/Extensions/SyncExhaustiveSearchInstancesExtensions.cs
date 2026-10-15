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
using System.Threading.Tasks;
using Accord.Neuro;
using AutoMapper.Internal;
using Jube.Analytics.Categorical;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Engine.AdvancedAnalytics.Recall;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Models;
using log4net;
using Newtonsoft.Json;
using Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    public static class SyncExhaustiveSearchInstancesExtensions
    {
        public static async Task<Context> SyncExhaustiveSearchInstancesAsync(this Context context)
        {
            var loaded = 0;
            var incomplete = 0;

            try
            {
                context.Services.Log.Info("Entity Start: Exhaustive sync started.");

                context.Services.Parser.EntityAnalysisModelsExhaustiveAdaptations = [];

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding models created and promoted by the Exhaustive Algorithm.");
                    }

                    var repository = new ExhaustiveSearchInstanceRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing select from Exhaustive Search Instance Repository and entity model key of {key}.");
                    }

                    var records = await repository.GetApprovedByEntityAnalysisModelIdOrderByIdAsync(key,
                        context.ApprovalsRequired,
                        context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityAnalysisModelExhaustive = new List<ExhaustiveSearchInstance>();

                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Exhaustive ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            var getExhaustiveSearchInstancePromotedTrialInstanceQuery
                                = await new ExhaustiveSearchInstancePromotedTrialInstanceRepository(
                                        context.Services.DbContext)
                                    .GetLastActiveByExhaustiveSearchInstanceIdAsync(record.Id,
                                        context.Services.CancellationToken).ConfigureAwait(false);

                            if (getExhaustiveSearchInstancePromotedTrialInstanceQuery != null)
                            {
                                IEnumerable<GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto>
                                    variableRows = null;

                                try
                                {
                                    variableRows =
                                        await new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(
                                                context.Services.DbContext)
                                            .ExecuteByExhaustiveSearchInstanceTrialInstanceIdAsync(
                                                getExhaustiveSearchInstancePromotedTrialInstanceQuery
                                                    .ExhaustiveSearchInstanceTrialInstanceId.GetValueOrDefault(),
                                                context.Services.CancellationToken).ConfigureAwait(false);
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    context.Services.Log.Error(
                                        $"Entity Start: Exhaustive GUID {record.Guid} (Id {record.Id}) failed to load the promoted trial variables as {ex}.");
                                }

                                var exhaustive = BuildExhaustiveModel(record,
                                    getExhaustiveSearchInstancePromotedTrialInstanceQuery,
                                    variableRows,
                                    context.JsonSerializationHelper.DeserializeTopologyNetworkJsonSerializerSettings,
                                    ex => context.Services.Log.Error(
                                        $"Entity Start: Exhaustive GUID {record.Guid} (Id {record.Id}) failed to load the promoted network as {ex}."));

                                LogIncompleteModel(context.Services.Log, exhaustive);

                                shadowEntityAnalysisModelExhaustive.Add(exhaustive);
                                loaded++;

                                if ((exhaustive.TopologyNetwork == null && exhaustive.ChallengerRecall == null)
                                    || exhaustive.NetworkVariablesInOrder.Count == 0
                                    || exhaustive.Calibration.IsInvalid)
                                {
                                    incomplete++;
                                }

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Exhaustive GUID {exhaustive.Id} has added {exhaustive.Name} to shadow collection.");
                                }

                                context.Services.Parser.EntityAnalysisModelsExhaustiveAdaptations.TryAdd(
                                    exhaustive.Name);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Exhaustive GUID {exhaustive.Id} has added {exhaustive.Name} to parser.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Exhaustive GUID {record.Id} has empty json indicating training not concluded.");
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Exhaustive Id {record.Id} returned for model {key} as created an error as {ex}.");
                        }
                    }

                    context.Snapshots.Builder(key, value).ExhaustiveModels = shadowEntityAnalysisModelExhaustive;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncExhaustiveSearchInstancesAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(
                        EntityAnalysisModelSynchronisationErrorRepository
                            .EntityAnalysisModelSynchronisationErrorStepEnum.ExhaustiveSearchInstances, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            context.Services.Log.Info(
                $"Entity Start: Exhaustive sync ended with {loaded} Exhaustive models loaded, of which {incomplete} have no network or no variables.");

            return context;
        }

        internal static void LogIncompleteModel(ILog log, ExhaustiveSearchInstance exhaustive)
        {
            if (exhaustive.TopologyNetwork == null && exhaustive.ChallengerRecall == null)
            {
                log.Error(
                    $"Entity Start: Exhaustive GUID {exhaustive.Guid} ({exhaustive.Name}) is registered with a NULL network and will not score.");
            }

            if (exhaustive.NetworkVariablesInOrder.Count == 0)
            {
                log.Error(
                    $"Entity Start: Exhaustive GUID {exhaustive.Guid} ({exhaustive.Name}) is registered with NO variables and will not score.");
            }

            if (exhaustive.Calibration.IsInvalid)
            {
                log.Error(
                    $"Entity Start: Exhaustive GUID {exhaustive.Guid} ({exhaustive.Name}) has a stored calibrator that is invalid ({exhaustive.Calibration.InvalidReason}) and will not score rather than serve uncalibrated values as probabilities.");
            }

            if (exhaustive.TopologyNetwork != null && exhaustive.NetworkVariablesInOrder.Count != 0
                                                   && exhaustive.TopologyNetwork.InputsCount !=
                                                   exhaustive.NetworkVariablesInOrder.Count)
            {
                log.Error(
                    $"Entity Start: Exhaustive GUID {exhaustive.Guid} ({exhaustive.Name}) is registered with {exhaustive.NetworkVariablesInOrder.Count} variables but a network of {exhaustive.TopologyNetwork.InputsCount} inputs and will not score.");
            }
        }

        internal static ExhaustiveSearchInstance BuildExhaustiveModel(
            Data.Poco.ExhaustiveSearchInstance record,
            Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance promotedTrialRow,
            IEnumerable<GetExhaustiveSearchInstancePromotedTrialInstanceVariableQueryDto> variableRows,
            JsonSerializerSettings settings,
            Action<Exception> onLoadError = null)
        {
            var exhaustive = new ExhaustiveSearchInstance
            {
                Id = record.Id,
                Guid = record.Guid,
                Name = record.Name == null ? $"Exhaustive_{record.Id}" : record.Name.Replace(" ", "_"),
                ResponsePayload = record.ResponsePayload == 1,
                ReportTable = record.ReportTable == 1
            };

            exhaustive.Calibration = ExhaustiveCalibration.FromColumns(
                promotedTrialRow.CalibrationMethodId?.ToString().ToLowerInvariant(),
                promotedTrialRow.CalibrationInputId?.ToString().ToLowerInvariant(),
                promotedTrialRow.CalibrationSlope,
                promotedTrialRow.CalibrationIntercept, promotedTrialRow.CalibrationEpsilon,
                promotedTrialRow.CalibrationPrior, promotedTrialRow.BaseRate,
                promotedTrialRow.BaseRateSourceId?.ToString(),
                promotedTrialRow.CalibrationParametersJson);

            try
            {
                if (promotedTrialRow.KindId == null
                    || promotedTrialRow.KindId == AdvancedAnalyticsModelKind.ExhaustiveTopologyNetwork)
                {
                    exhaustive.TopologyNetwork = JsonConvert.DeserializeObject<ActivationNetwork>(
                        promotedTrialRow.Json, settings);
                }
                else
                {
                    exhaustive.ChallengerRecall = AdvancedAnalyticsModelRegistry.Build(promotedTrialRow.Json);
                }

                if (variableRows != null)
                {
                    var encoders = new Dictionary<int, CategoricalFieldEncoder>();

                    foreach (var variable in variableRows)
                    {
                        CategoricalFieldEncoder encoder = null;

                        if (variable.CategoricalFieldId.HasValue && variable.CategoricalVocabularyJson != null)
                        {
                            if (!encoders.TryGetValue(variable.CategoricalFieldId.Value, out encoder))
                            {
                                encoder = CategoricalFieldEncoder.FromJson(variable.CategoricalVocabularyJson);
                                encoders[variable.CategoricalFieldId.Value] = encoder;
                            }
                        }

                        exhaustive.NetworkVariablesInOrder.Add(new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                        {
                            Name = variable.Name,
                            ProcessingTypeId = variable.ProcessingTypeId,
                            Mean = variable.Mean,
                            Sd = variable.StandardDeviation,
                            NormalisationTypeId = variable.NormalisationTypeId,
                            CategoricalEncoder = encoder,
                            CategoricalIndicatorIndex = variable.CategoricalIndicatorIndex.GetValueOrDefault()
                        });
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onLoadError?.Invoke(ex);
            }

            return exhaustive;
        }
    }
}