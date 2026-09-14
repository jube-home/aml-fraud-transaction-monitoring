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

using System.Collections.Generic;
using System.Linq;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    public static class ActivationRuleGetSuppressedModelExtensions
    {
        public static bool ActivationRuleGetSuppressedModel(this Context context,
            ref List<string> suppressedActivationRules)
        {
            var suppressedModelValue = false;
            foreach (var xpath in context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths
                         .Where(w => w.EnableSuppression)
                         .ToList())
            {
                context.TraceLog(
                    $"Suppression key is {xpath.Name}. Will now check to see if has a suppressed value.");

                if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(xpath.Name))
                {
                    if (context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionModels.TryGetValue(
                            xpath.Name, out var value))
                    {
                        suppressedModelValue = value.Contains(
                            context.EntityAnalysisModelInstanceEntryPayload.Payload[xpath.Name].AsString());
                    }
                    else
                    {
                        context.TraceLog($"Suppression key is {xpath.Name} but it has no keys.");
                    }

                    var suppressedModelValueForLog = suppressedModelValue;
                    context.TraceLog($"Suppression status is {suppressedModelValueForLog}.");

                    if (context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionRules.ContainsKey(
                            xpath.Name))
                    {
                        if (context.EntityAnalysisModel.Dependencies.EntityAnalysisModelSuppressionRules[xpath.Name]
                            .ContainsKey(
                                context.EntityAnalysisModelInstanceEntryPayload.Payload[xpath.Name].AsString()))
                        {
                            suppressedActivationRules =
                                context.EntityAnalysisModel.Dependencies
                                    .EntityAnalysisModelSuppressionRules[xpath.Name][
                                        context.EntityAnalysisModelInstanceEntryPayload.Payload[xpath.Name].AsString()];

                            context.TraceLog($"Suppression status is {suppressedModelValueForLog}.");
                        }
                        else
                        {
                            context.TraceLog($"Suppression status is {suppressedModelValueForLog}.");
                        }
                    }
                    else
                    {
                        context.TraceLog($"Suppression key is {xpath.Name} but it has no keys.");
                    }
                }
                else
                {
                    context.TraceLog(
                        $"Suppression key is {xpath.Name} but could not locate the value in the data payload.");
                }
            }

            return suppressedModelValue;
        }
    }
}