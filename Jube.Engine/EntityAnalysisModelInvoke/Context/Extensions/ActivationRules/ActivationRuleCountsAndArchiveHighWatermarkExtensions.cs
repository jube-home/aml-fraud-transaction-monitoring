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

using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    public static class ActivationRuleCountsAndArchiveHighWatermarkExtensions
    {
        public static void ActivationRuleCountsAndArchiveHighWatermark(this Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed, ref int activationRuleCount, ref int? prevailingActivationRuleId,
            ref string prevailingActivationRuleName)
        {
            if (suppressed)
            {
                return;
            }

            if (evaluateActivationRule.Visible)
            {
                prevailingActivationRuleId = evaluateActivationRule.Id;
                prevailingActivationRuleName = evaluateActivationRule.Name;

                activationRuleCount += 1;
            }
            else
            {
                var currentPrevailingActivationRuleId = prevailingActivationRuleId;
                var currentActivationRuleCount = activationRuleCount;
                context.TraceLog(
                    $"response elevation for activation rule {evaluateActivationRule.Id} has not been included in the local count as the rule is not set to visible. The prevailing activation rule id is {currentPrevailingActivationRuleId} and this activation rule count is {currentActivationRuleCount}.");
            }

            var finalPrevailingActivationRuleId = prevailingActivationRuleId;
            var finalActivationRuleCount = activationRuleCount;
            context.TraceLog(
                $"response elevation for activation rule {evaluateActivationRule.Id} activation counter has been incremented and the prevailing activation rule has been set to {finalPrevailingActivationRuleId}. This activation rule count is {finalActivationRuleCount}.");
        }
    }
}