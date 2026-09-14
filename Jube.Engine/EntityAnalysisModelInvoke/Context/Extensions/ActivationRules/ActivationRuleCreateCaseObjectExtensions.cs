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
using System.Threading.Tasks;
using Jube.Cache;
using Jube.Engine.EntityAnalysisModelInvoke.Models.CaseManagement;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    public static class ActivationRuleCreateCaseObjectExtensions
    {
        public static async Task<CreateCase> ActivationRuleCreateCaseObjectAsync(this Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed, CacheService cacheService)
        {
            if (!evaluateActivationRule.EnableCaseWorkflow || suppressed)
            {
                return null;
            }

            if (context.Environment.AppSettings("ActivationRuleIdempotency")
                .Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (!await cacheService.CacheActivationCaseIdempotencyRepository.CheckAndClaimIdempotencyAsync(
                        context.EntityAnalysisModel.Instance.TenantRegistryId,
                        context.EntityAnalysisModel.Instance.Guid,
                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                        evaluateActivationRule.Guid))
                {
                    context.TraceLog(
                        $"activation rule guid {evaluateActivationRule.Guid} has failed case idempotency check.");

                    return null;
                }
            }
            else
            {
                context.TraceLog(
                    $"activation rule guid {evaluateActivationRule.Guid} won't be checked for ActivationRuleIdempotency.");
            }

            var createCase = BuildCreateCase(context, evaluateActivationRule);

            context.TraceLog(
                $"has flagged that a case needs to be created for case workflow id {createCase.CaseWorkflowGuid} and case status id {createCase.CaseWorkflowStatusGuid}. The case will be queued later after the archive XML has been created.");

            return createCase;
        }

        private static CreateCase BuildCreateCase(Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule)
        {
            var createCase = new CreateCase
            {
                TenantRegistryId = context.EntityAnalysisModel.Instance.TenantRegistryId,
                EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload
                    .EntityAnalysisModelInstanceEntryGuid,
                CaseWorkflowGuid = evaluateActivationRule.CaseWorkflowGuid,
                CaseWorkflowStatusGuid = evaluateActivationRule.CaseWorkflowStatusGuid
            };

            ApplySuspendBypass(context, evaluateActivationRule, createCase);
            ApplyCaseKey(context, evaluateActivationRule, createCase);

            return createCase;
        }

        private static void ApplySuspendBypass(Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule, CreateCase createCase)
        {
            if (evaluateActivationRule.BypassSuspendSample > context.Random.NextDouble())
            {
                createCase.SuspendBypass = true;
                context.TraceLog(
                    $"case key is {evaluateActivationRule.CaseKey} has been selected for bypass.");

                createCase.SuspendBypassDate = evaluateActivationRule.BypassSuspendInterval switch
                {
                    'n' => DateTime.UtcNow.AddMinutes(evaluateActivationRule.BypassSuspendValue),
                    'h' => DateTime.UtcNow.AddHours(evaluateActivationRule.BypassSuspendValue),
                    'd' => DateTime.UtcNow.AddDays(evaluateActivationRule.BypassSuspendValue),
                    'm' => DateTime.UtcNow.AddMonths(evaluateActivationRule.BypassSuspendValue),
                    _ => createCase.SuspendBypassDate
                };

                context.TraceLog(
                    $"case key is {evaluateActivationRule.CaseKey} has a bypass interval of {evaluateActivationRule.BypassSuspendInterval} to create a date of {createCase.SuspendBypassDate}.");
            }
            else
            {
                createCase.SuspendBypass = false;

                context.TraceLog(
                    $"case key is {evaluateActivationRule.CaseKey} has been selected for open.");

                createCase.SuspendBypassDate = DateTime.UtcNow;
            }
        }

        private static void ApplyCaseKey(Context context, EntityAnalysisModelActivationRule evaluateActivationRule,
            CreateCase createCase)
        {
            context.TraceLog(
                $"case key is {evaluateActivationRule.CaseKey} which is {(string.IsNullOrEmpty(evaluateActivationRule.CaseKey) ? "an" : "not an")} entry foreign key.");

            if (evaluateActivationRule.CaseKey != null &&
                context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(evaluateActivationRule.CaseKey))
            {
                createCase.CaseKey = evaluateActivationRule.CaseKey;
                createCase.CaseKeyValue = context.EntityAnalysisModelInstanceEntryPayload
                    .Payload[evaluateActivationRule.CaseKey].ToString();

                context.TraceLog(
                    $"case key is {evaluateActivationRule.CaseKey} and case key value is {context.EntityAnalysisModelInstanceEntryPayload.Payload[evaluateActivationRule.CaseKey]}.");
            }
            else
            {
                createCase.CaseKeyValue = context.EntityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId;
                createCase.CaseKey = null;

                context.TraceLog(
                    $"case key is {evaluateActivationRule.CaseKey} does not have a value, has fallen back to the entity id of {context.EntityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId}.");
            }
        }
    }
}