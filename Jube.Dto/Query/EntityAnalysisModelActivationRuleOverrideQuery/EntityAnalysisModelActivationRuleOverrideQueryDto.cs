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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.EntityAnalysisModelActivationRuleOverrideQuery
{
    [MessagePackObject]
    public class EntityAnalysisModelActivationRuleOverrideQueryDto
    {
        [Description(
            "Name of the Activation Rule that has Override enabled for the requested Entity Analysis Model.")]
        [Key(0)]
        public string? Name { get; set; }

        [Description("True when a live (non-deleted, non-expired) override exists for this rule against the " +
                     "requested override key and value.")]
        [Key(1)]
        public bool HasOverride { get; set; }

        [Description("Integer identifier of the Entity Analysis Model Activation Rule (the rule Id, despite the " +
                     "property name, which is retained for wire compatibility).")]
        [Key(2)]
        public int EntityAnalysisModelActivationRuleOverrideId { get; set; }

        [Description("Guid of the Entity Analysis Model the rule belongs to.")]
        [Key(3)]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("Whether this Activation Rule permits an override of kind Force. Where false the rule may " +
                     "only be suppressed, and the Kind may not be set to Force.")]
        [Key(4)]
        public bool EnableForce { get; set; }

        [Description("Whether the override mutes the Activation Rule's consequences or forces them. " +
                     "Meaningless when Override is false.")]
        [Key(5)]
        public EntityAnalysisModelOverrideKind OverrideKind { get; set; }

        [Description(
            "UTC expiry of the active override, or null when no override is active or it never expires.")]
        [Key(6)]
        public DateTimeOffset? DeleteExpiryDate { get; set; }

        [Description("Identifier of the Activation Rule Override row behind this rule's override, used to look " +
                     "up its approval state when the Kind is Force. Zero when there is no override.")]
        [Key(7)]
        public int OverrideId { get; set; }

        [Description("True when a Force override has been removed but the removal is still awaiting approval, " +
                     "so the last approved version is still in force. The override is then listed with " +
                     "HasOverride true and cannot be switched or changed until the removal is approved or " +
                     "retracted.")]
        [Key(8)]
        public bool PendingRemoval { get; set; }
    }
}