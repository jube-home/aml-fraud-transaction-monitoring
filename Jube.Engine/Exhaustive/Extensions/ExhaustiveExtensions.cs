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
using System.Linq;
using Accord.Neuro;
using Jube.Analytics;
using Jube.Engine.BackgroundTasks.Context;
using Jube.Engine.Exhaustive.Models;
using Jube.Engine.Exhaustive.Utilities;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.Exhaustive.Extensions
{
    public static class ExhaustiveExtensions
    {
        public static double RecallExhaustive(this Context context, Guid guid, JObject jObject)
        {
            double valueRecall = 0;

            try
            {
                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Exhaustive Recall: GUID {guid} callback received for Exhaustive.");
                }

                var foundExhaustive = false;

                try
                {
                    foreach (var exhaustive in context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                                 .ActiveEntityAnalysisModels.Select(model =>
                                     model.Value.Snapshot.ExhaustiveModels
                                         .FirstOrDefault(w => w.Guid
                                                              == guid)).Where(exhaustive => exhaustive != null))
                    {
                        foundExhaustive = true;

                        valueRecall = Score(exhaustive, jObject,
                            message => context.Services.Log.Info($"Exhaustive Recall: GUID {guid} {message}"));
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (context.Services.Log.IsInfoEnabled)
                    {
                        context.Services.Log.Info(
                            $"Exhaustive Recall: GUID {guid} is in error as {ex} and has flushed an error message to the response stream.");
                    }

                    throw;
                }

                if (!foundExhaustive)
                {
                    if (context.Services.Log.IsInfoEnabled)
                    {
                        context.Services.Log.Info(
                            $"Exhaustive Recall: GUID {guid} could not find the adaptation and has flushed an error message to the response stream.");
                    }

                    throw new KeyNotFoundException();
                }

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Exhaustive Recall: GUID {guid} has flushed the adaptation response to the response stream.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"Exhaustive Recall: GUID {guid} has caused an error as {ex}");
            }
            finally
            {
                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info($"Exhaustive Recall: GUID {guid} has completed.");
                }
            }

            return valueRecall;
        }

        public static (double Value, RecallRelevance Relevance) RecallExhaustiveWithExplainability(
            this Context context, Guid guid, JObject jObject)
        {
            var exhaustive = context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                .ActiveEntityAnalysisModels
                .Select(model => model.Value.Snapshot.ExhaustiveModels.FirstOrDefault(w => w.Guid == guid))
                .FirstOrDefault(found => found != null);

            return exhaustive == null ? (0, null) : context.RecallExhaustiveWithExplainability(exhaustive, jObject);
        }

        public static (double Value, RecallRelevance Relevance) RecallExhaustiveWithExplainability(
            this Context context, ExhaustiveSearchInstance exhaustive, JObject jObject)
        {
            try
            {
                if (exhaustive.TopologyNetwork != null)
                {
                    var inputs = BuildScoreInputs(exhaustive.NetworkVariablesInOrder, jObject);
                    var rawScore = exhaustive.TopologyNetwork.Compute(inputs)[0];
                    var names = exhaustive.NetworkVariablesInOrder.Select(v => v.Name).ToArray();
                    var relevance = RecallExplainability.Explain(exhaustive.TopologyNetwork, inputs, names);

                    return (exhaustive.Calibration.Apply(rawScore), relevance);
                }

                var rawInputs = BuildScoreInputs(exhaustive.NetworkVariablesInOrder, jObject, standardise: false);
                var value = exhaustive.ChallengerRecall.Predict(rawInputs)[0];

                return (value, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"Exhaustive Recall: GUID {exhaustive.Guid} has caused an error as {ex}");

                return (0, null);
            }
        }

        internal static double Score(ExhaustiveSearchInstance model, JObject jObject,
            Action<string> trace = null)
        {
            if (model.TopologyNetwork == null && model.ChallengerRecall != null)
            {
                var rawInputs = BuildScoreInputs(model.NetworkVariablesInOrder, jObject, trace, false);

                return model.ChallengerRecall.Predict(rawInputs)[0];
            }

            return model.Calibration.Apply(Score(model.TopologyNetwork, model.NetworkVariablesInOrder, jObject,
                trace));
        }

        internal static double Score(ActivationNetwork network,
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstanceVariable> variables, JObject jObject,
            Action<string> trace = null)
        {
            return network.Compute(BuildScoreInputs(variables, jObject, trace))[0];
        }

        internal static double[] BuildScoreInputs(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstanceVariable> variables, JObject jObject,
            Action<string> trace = null, bool standardise = true)
        {
            var scoreInputs = new double[variables.Count];

            for (var i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];

                if (variable.IsCategorical)
                {
                    var indicator = variable.CategoricalEncoder.Indicator(
                        CategoricalValue.FromToken(GetValueFromJson(jObject, variable.CategoricalSourceName)),
                        variable.CategoricalIndicatorIndex);

                    scoreInputs[i] = standardise ? variable.ZScore(indicator) : indicator;
                    continue;
                }

                double? raw = null;

                var token = GetValueFromJson(jObject, variable.Name);

                if (token is { Type: not JTokenType.Null })
                {
                    try
                    {
                        raw = Convert.ToDouble(token);
                    }
                    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
                    {
                        trace?.Invoke($"could not convert {variable.Name} as {ex.Message}, treating as missing.");
                    }
                }

                scoreInputs[i] = Standardisation.Resolve(raw, variable.Mean, variable.Sd,
                    standardise && variable.NormalisationTypeId == 2, out var imputed);

                if (imputed)
                {
                    trace?.Invoke($"has no usable value for {variable.Name}, imputed the training mean.");
                }
            }

            return scoreInputs;
        }

        private static JToken GetValueFromJson(JObject jObject, string name)
        {
            foreach (var (key, value) in jObject)
            {
                if (key == name)
                {
                    return value;
                }
            }

            return null;
        }
    }
}