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

#nullable enable

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models
{
    using System.Collections.Generic;

    public class EntityAnalysisModelOverride
    {
        public EntityAnalysisModelOverrideKind? AllActivationRules { get; set; }

        public Dictionary<string, EntityAnalysisModelOverrideKind> ActivationRules { get; } =
            new Dictionary<string, EntityAnalysisModelOverrideKind>();

        public void Promote(string? activationRuleName, EntityAnalysisModelOverrideKind kind)
        {
            if (activationRuleName == null)
            {
                if (AllActivationRules != EntityAnalysisModelOverrideKind.Force)
                {
                    AllActivationRules = kind;
                }

                return;
            }

            if (ActivationRules.TryGetValue(activationRuleName, out var existing)
                && existing == EntityAnalysisModelOverrideKind.Force)
            {
                return;
            }

            ActivationRules[activationRuleName] = kind;
        }

        public EntityAnalysisModelOverrideKind? KindFor(string? activationRuleName)
        {
            var specific = activationRuleName != null && ActivationRules.TryGetValue(activationRuleName, out var kind)
                ? kind
                : (EntityAnalysisModelOverrideKind?)null;

            if (specific == EntityAnalysisModelOverrideKind.Force
                || AllActivationRules == EntityAnalysisModelOverrideKind.Force)
            {
                return EntityAnalysisModelOverrideKind.Force;
            }

            return specific ?? AllActivationRules;
        }

        public void Merge(EntityAnalysisModelOverride other)
        {
            if (other.AllActivationRules.HasValue)
            {
                Promote(null, other.AllActivationRules.Value);
            }

            foreach (var (activationRuleName, kind) in other.ActivationRules)
            {
                Promote(activationRuleName, kind);
            }
        }
    }
}