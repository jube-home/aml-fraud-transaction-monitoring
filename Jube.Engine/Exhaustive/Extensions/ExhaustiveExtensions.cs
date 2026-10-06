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

namespace Jube.Engine.Exhaustive.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BackgroundTasks.Context;
    using Newtonsoft.Json.Linq;

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

                        var scoreInputs = new double[exhaustive.NetworkVariablesInOrder.Count];
                        for (var i = 0; i < exhaustive.NetworkVariablesInOrder.Count; i++)
                        {
                            var scoreInput = exhaustive.NetworkVariablesInOrder[i];

                            if (context.Services.Log.IsInfoEnabled)
                            {
                                context.Services.Log.Info(
                                    $"Exhaustive Recall: GUID {guid}" +
                                    $" looking for match on {scoreInput.Name}.");
                            }

                            double valueElement;
                            try
                            {
                                var selectedToken = GetValueFromJson(jObject, scoreInput.Name);
                                if (selectedToken != null)
                                {
                                    valueElement = Convert.ToDouble(selectedToken);

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Exhaustive Recall: GUID {guid} " +
                                            $"has found a value in the payload for {scoreInput.Name} as {valueElement}.");
                                    }

                                    if (scoreInput.NormalisationTypeId == 2
                                       )
                                    {
                                        valueElement = (valueElement - scoreInput.Mean) / scoreInput.Sd;

                                        if (context.Services.Log.IsInfoEnabled)
                                        {
                                            context.Services.Log.Info(
                                                $"Exhaustive Recall: GUID {guid}" +
                                                $" has a standardization type of 2 for {scoreInput.Name}" +
                                                $" and has been standardized to {valueElement} .");
                                        }
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Exhaustive Recall: GUID {guid}" +
                                            $" could not locate a match for {scoreInput.Name}.");
                                    }

                                    valueElement = 0;
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error(
                                    $"Exhaustive Recall: GUID {guid}" +
                                    $" has produced an error on {scoreInput.Name} as {ex}.");

                                valueElement = 0;
                            }

                            scoreInputs[i] = valueElement;
                        }

                        valueRecall = exhaustive.TopologyNetwork.Compute(scoreInputs)[0];
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