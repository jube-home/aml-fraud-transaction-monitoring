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
    public class CaseSuppressionApprovalDto
    {
        [Description("Server-assigned identifier. Read-only.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("The version of the request the decision applies to.")]
        [Key(1)]
        public int SuppressionVersion { get; set; }

        [Description("The decision as an integer: 1 Approved, 2 Rejected, 3 Retracted, 4 Resubmitted.")]
        [Key(2)]
        public int DecisionId { get; set; }

        [Description("The note recorded with the decision.")]
        [Key(3)]
        public string? Note { get; set; }

        [Description("The user who decided. Read-only.")]
        [Key(4)]
        public string? CreatedUser { get; set; }

        [Description("When they decided (UTC). Read-only.")]
        [Key(5)]
        public DateTimeOffset? CreatedDate { get; set; }
    }
}
