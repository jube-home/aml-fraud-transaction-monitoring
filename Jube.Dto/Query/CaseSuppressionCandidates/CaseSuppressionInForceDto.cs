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
using System;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSuppressionCandidates
{
    [MessagePackObject]
    public class CaseSuppressionInForceDto
    {
        [Description("Id of the override row.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("True for an activation rule override, false for a whole model override.")]
        [Key(1)]
        public bool IsRule { get; set; }

        [Description("The override key.")]
        [Key(2)]
        public string OverrideKey { get; set; } = string.Empty;

        [Description("The override key value.")]
        [Key(3)]
        public string OverrideKeyValue { get; set; } = string.Empty;

        [Description("The model name.")]
        [Key(4)]
        public string? EntityAnalysisModelName { get; set; }

        [Description("The activation rule name for a rule override.")]
        [Key(5)]
        public string? RuleName { get; set; }

        [Description("0 Suppress, 1 Force.")]
        [Key(6)]
        public int OverrideKindId { get; set; }

        [Description("When the override is removed (UTC), or null when it never expires.")]
        [Key(7)]
        public DateTimeOffset? ExpiryDate { get; set; }

        [Description("Who created the override.")]
        [Key(8)]
        public string? CreatedUser { get; set; }

        [Description("When it was created (UTC).")]
        [Key(9)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("The case the override was requested from, or null when it was set elsewhere, for example on the Override page.")]
        [Key(10)]
        public int? OriginCaseId { get; set; }
    }
}
