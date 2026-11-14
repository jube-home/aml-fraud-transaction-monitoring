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

namespace Jube.Dto.Repository.CaseSuppression
{
    [MessagePackObject]
    public class CaseSuppressionTargetDto
    {
        [Description("Server-assigned identifier of the target. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Guid of the model the suppression applies to.")]
        [Key(1)]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("The model name when the request was made. Server-assigned and read-only.")]
        [Key(2)]
        public string? EntityAnalysisModelName { get; set; }

        [Description("The activation rule name. Leave empty to suppress or force the whole model.")]
        [Key(3)]
        public string? RuleName { get; set; }

        [Description("How the override acts as an integer: 0 Suppress, 1 Force (needs permission 62).")]
        [Key(4)]
        public int OverrideKindId { get; set; }

        [Description("Id of the override row created, or found already in force, when the request was approved. Null until applied. Read-only.")]
        [Key(5)]
        public int? AppliedOverrideId { get; set; }

        [Description("When it was applied (UTC). Read-only.")]
        [Key(6)]
        public DateTimeOffset? AppliedDate { get; set; }

        [Description("What the override is now as an integer: 1 Pending, 2 Approved, 3 Rejected, 4 Retracted, 5 Expired, 6 Removed. Server-assigned and read-only.")]
        [Key(7)]
        public int StandingId { get; set; }
    }
}
