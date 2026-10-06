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

using System.Linq;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    public static class ActivationRuleOverridesExtensions
    {
        public static EntityAnalysisModelOverride ActivationRuleGetOverrides(this Context context)
        {
            var outcome = new EntityAnalysisModelOverride();
            var overrides = context.Snapshot.EntityAnalysisModelOverrides;

            if (overrides == null)
            {
                return outcome;
            }

            foreach (var xpath in context.Snapshot.EntityAnalysisModelRequestXPaths
                         .Where(w => w.EnableOverride))
            {
                context.TraceLog(
                    $"Override key is {xpath.Name}. Will now check to see if has a overridden value.");

                if (!context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(xpath.Name,
                        out var payloadValue))
                {
                    context.TraceLog(
                        $"Override key is {xpath.Name} but could not locate the value in the data payload.");

                    continue;
                }

                if (!overrides.TryGetValue(xpath.Name, out var values))
                {
                    context.TraceLog($"Override key is {xpath.Name} but it has no keys.");

                    continue;
                }

                if (!values.TryGetValue(payloadValue.AsString(), out var overrideBinding))
                {
                    context.TraceLog(
                        $"Override key is {xpath.Name} but the payload value has no override.");

                    continue;
                }

                outcome.Merge(Admissible(context, overrideBinding, xpath.Name));

                context.TraceLog(
                    $"Override key is {xpath.Name} contributed an all activation rules override of {overrideBinding.AllActivationRules} and {overrideBinding.ActivationRules.Count} activation rule overrides before admissibility.");
            }

            context.TraceLog(
                $"Override resolved to an all activation rules override of {outcome.AllActivationRules} and {outcome.ActivationRules.Count} activation rule overrides.");

            return outcome;
        }

        private static EntityAnalysisModelOverride Admissible(Context context,
            EntityAnalysisModelOverride binding, string overrideKey)
        {
            var admissible = new EntityAnalysisModelOverride
            {
                AllActivationRules = binding.AllActivationRules
            };

            foreach (var (activationRuleName, kind) in binding.ActivationRules)
            {
                var rule = context.Snapshot.ModelActivationRules
                    .FirstOrDefault(w => w.Name == activationRuleName);

                if (rule == null)
                {
                    context.TraceLog(
                        $"Override key is {overrideKey} naming activation rule {activationRuleName}, which the model does not have, so it is ignored.");

                    continue;
                }

                if (!rule.EnableOverride)
                {
                    context.TraceLog(
                        $"Override key is {overrideKey} naming activation rule {activationRuleName}, which is not enabled for override, so it is ignored.");

                    continue;
                }

                if (kind == EntityAnalysisModelOverrideKind.Force && !rule.EnableForce)
                {
                    context.TraceLog(
                        $"Override key is {overrideKey} forcing activation rule {activationRuleName}, which is not enabled for force, so it is ignored.");

                    continue;
                }

                if (!string.IsNullOrEmpty(rule.OverrideKey) && rule.OverrideKey != overrideKey)
                {
                    context.TraceLog(
                        $"Override key is {overrideKey} naming activation rule {activationRuleName}, which is bound to override key {rule.OverrideKey}, so it is ignored.");

                    continue;
                }

                admissible.Promote(activationRuleName, kind);
            }

            return admissible;
        }
    }
}