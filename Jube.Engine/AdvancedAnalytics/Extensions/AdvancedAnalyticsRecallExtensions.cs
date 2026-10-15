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
using Jube.HttpAdaptationProtocol;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.AdvancedAnalytics.Extensions
{
    public static class AdvancedAnalyticsRecallExtensions
    {
        public static (double Value, Adaptation Detail) RecallAdvancedAnalyticsWithExplainability(
            this Context context, Guid guid, JObject jObject)
        {
            try
            {
                var model = context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                    .ActiveEntityAnalysisModels
                    .Select(m => m.Value.Snapshot.AdvancedAnalyticsModels.FirstOrDefault(w => w.Guid == guid))
                    .FirstOrDefault(found => found != null);

                if (model == null)
                {
                    return (0, null);
                }

                var inputs = BuildScoreInputs(model.VariablesInOrder, jObject);
                var value = model.Recall(inputs)[0];
                var explanation = model.Explain?.Invoke(inputs);

                var detail = explanation?.ToAdaptation(value, model.Name,
                    [.. model.VariablesInOrder.Select(v => v.ProcessingTypeId)]);

                return (value, detail);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"Advanced Analytics Recall: GUID {guid} has caused an error as {ex}");

                return (0, null);
            }
        }

        internal static double[] BuildScoreInputs(IReadOnlyList<AdvancedAnalyticsModelVariable> variables,
            JObject jObject)
        {
            return AdvancedAnalyticsExtensions.BuildScoreInputs(variables, jObject);
        }
    }
}