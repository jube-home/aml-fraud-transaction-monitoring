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

using System.ComponentModel;
using Jube.Dto.Overrides;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.EntityAnalysisModelActivationRuleOverrideQuery
{
    public class EntityAnalysisModelActivationRuleOverrideQueryDto
    {
        [Description(
            "Name of the Activation Rule that has Override enabled for the requested Entity Analysis Model.")]
        public string? Name { get; set; }

        [Description("True when a live (non-deleted, non-expired) override exists for this rule against the " +
                     "requested override key and value.")]
        public bool HasOverride { get; set; }

        [Description("Integer identifier of the Entity Analysis Model Activation Rule (the rule Id, despite the " +
                     "property name, which is retained for wire compatibility).")]
        public int EntityAnalysisModelActivationRuleOverrideId { get; set; }

        [Description("Guid of the Entity Analysis Model the rule belongs to.")]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("Whether this Activation Rule permits an override of kind Force. Where false the rule may " +
                     "only be suppressed, and the Kind may not be set to Force.")]
        public bool EnableForce { get; set; }

        [Description("Whether the override mutes the Activation Rule's consequences or forces them. " +
                     "Meaningless when Override is false.")]
        public EntityAnalysisModelOverrideKind OverrideKind { get; set; }

        [Description(
            "UTC expiry of the active override, or null when no override is active or it never expires.")]
        public DateTimeOffset? DeleteExpiryDate { get; set; }
    }
}