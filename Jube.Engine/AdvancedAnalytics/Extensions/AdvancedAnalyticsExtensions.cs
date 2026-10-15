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
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.BackgroundTasks.Context;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.AdvancedAnalytics.Extensions
{
    public static class AdvancedAnalyticsExtensions
    {
        public static double RecallAdvancedAnalytics(this Context context, Guid guid, JObject jObject)
        {
            if (context.Services.Log.IsInfoEnabled)
            {
                context.Services.Log.Info($"Advanced Analytics Recall: GUID {guid} callback received.");
            }

            var value = Recall(
                context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels.ActiveEntityAnalysisModels.Values,
                guid, jObject,
                message => context.Services.Log.Info($"Advanced Analytics Recall: GUID {guid} {message}"));

            if (context.Services.Log.IsInfoEnabled)
            {
                context.Services.Log.Info($"Advanced Analytics Recall: GUID {guid} has completed.");
            }

            return value;
        }

        public static AdvancedAnalyticsModel FindAdvancedAnalyticsModel(
            this IEnumerable<EntityAnalysisModel> entityAnalysisModels, Guid guid)
        {
            return entityAnalysisModels
                .Select(model => model.Snapshot.AdvancedAnalyticsModels.FirstOrDefault(w => w.Guid == guid))
                .FirstOrDefault(model => model != null);
        }

        internal static double Recall(IEnumerable<EntityAnalysisModel> entityAnalysisModels, Guid guid,
            JObject jObject, Action<string> trace = null)
        {
            var model = entityAnalysisModels.FindAdvancedAnalyticsModel(guid);

            if (model == null)
            {
                throw new KeyNotFoundException();
            }

            return Score(model.Recall, model.VariablesInOrder, jObject, trace);
        }

        internal static double Score(Func<double[], double[]> recall,
            IReadOnlyList<AdvancedAnalyticsModelVariable> variables, JObject jObject, Action<string> trace = null)
        {
            ArgumentNullException.ThrowIfNull(recall);

            return recall(BuildScoreInputs(variables, jObject, trace))[0];
        }

        internal static double[] BuildScoreInputs(IReadOnlyList<AdvancedAnalyticsModelVariable> variables,
            JObject jObject, Action<string> trace = null)
        {
            var scoreInputs = new double[variables.Count];

            for (var i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                var value = double.NaN;

                var token = GetValueFromJson(jObject, variable.Name);

                if (token is { Type: not JTokenType.Null })
                {
                    try
                    {
                        value = Convert.ToDouble(token);
                    }
                    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
                    {
                        trace?.Invoke($"could not convert {variable.Name} as {ex.Message}, treating as missing.");
                    }
                }

                if (!double.IsFinite(value))
                {
                    value = double.NaN;
                    trace?.Invoke($"has no usable value for {variable.Name}, the model imputes as trained.");
                }

                scoreInputs[i] = value;
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