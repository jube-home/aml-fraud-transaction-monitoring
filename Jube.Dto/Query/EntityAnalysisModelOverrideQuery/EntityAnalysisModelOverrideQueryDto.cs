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

namespace Jube.Dto.Query.EntityAnalysisModelOverrideQuery
{
    [MessagePackObject]
    public class EntityAnalysisModelOverrideQueryDto
    {
        [Description("Name of the Entity Analysis Model that has a request XPath named after the queried " +
                     "override key with override enabled.")]
        [Key(0)]
        public string? Name { get; set; }

        [Description("Globally unique identifier of the Entity Analysis Model.")]
        [Key(1)]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("True when an active, unexpired override exists for the queried key and key value on " +
                     "this model.")]
        [Key(2)]
        public bool HasOverride { get; set; }

        [Description("Whether the override mutes the Activation Rules' consequences or forces them. " +
                     "Meaningless when Override is false.")]
        [Key(3)]
        public EntityAnalysisModelOverrideKind OverrideKind { get; set; }

        [Description("UTC instant at which the override expires; null when there is no override or it " +
                     "never expires.")]
        [Key(4)]
        public DateTimeOffset? DeleteExpiryDate { get; set; }

        [Description("Identifier of the Override row behind this model's override, used to look up its approval " +
                     "state when the Kind is Force. Zero when there is no override.")]
        [Key(5)]
        public int OverrideId { get; set; }

        [Description("True when a Force override has been removed but the removal is still awaiting approval, " +
                     "so the last approved version is still in force. The override is then listed with " +
                     "HasOverride true and cannot be switched or changed until the removal is approved or " +
                     "retracted.")]
        [Key(6)]
        public bool PendingRemoval { get; set; }
    }
}