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
    public class EntityAnalysisModelOverrideValueQueryDto
    {
        [Description("The overridden value held against the override key, e.g. a specific card fingerprint.")]
        [Key(0)]
        public string? OverrideKeyValue { get; set; }

        [Description("Name of the Model the override belongs to.")]
        [Key(1)]
        public string? Name { get; set; }

        [Description("Guid of the Model the override belongs to.")]
        [Key(2)]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("Whether the override mutes the Activation Rules' consequences or forces them.")]
        [Key(3)]
        public EntityAnalysisModelOverrideKind OverrideKind { get; set; }

        [Description("User who created the override.")]
        [Key(4)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) the override was created.")]
        [Key(5)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("When set, the override is removed once this date/time (UTC) is reached.")]
        [Key(6)]
        public DateTimeOffset? DeleteExpiryDate { get; set; }
    }
}