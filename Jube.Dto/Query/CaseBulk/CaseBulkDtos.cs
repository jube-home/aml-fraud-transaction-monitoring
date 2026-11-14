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

using System.Collections.Generic;
using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseBulk
{
    [Description("A request to close several cases at once, with one reason for all of them.")]
    [MessagePackObject]
    public class CaseBulkCloseDto
    {
        [Description("Identifiers of the cases to close. Between 1 and 500.")]
        [Key(0)]
        public List<int> CaseIds { get; set; } = [];

        [Description("Why the cases are closed, as a CaseCloseReason: 0 none, 1 to 8 as on a single close.")]
        [Key(1)]
        public byte ClosedReasonId { get; set; }

        [Description("Optional free text, up to 1000 characters, recorded on every case closed.")]
        [Key(2)]
        public string? ClosedReasonNote { get; set; }
    }

    [Description("A request to hand several cases to one analyst at once.")]
    [MessagePackObject]
    public class CaseBulkReassignDto
    {
        [Description("Identifiers of the cases to hand over. Between 1 and 500.")]
        [Key(0)]
        public List<int> CaseIds { get; set; } = [];

        [Description("User name of the analyst who takes the cases. Each case is checked for their eligibility " +
                     "in the same way as a single reassignment.")]
        [Key(1)]
        public string? ToUser { get; set; }
    }

    [Description("What happened to one case in a bulk request.")]
    [MessagePackObject]
    public class CaseBulkItemDto
    {
        [Description("The case.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("True when the action was applied.")]
        [Key(1)]
        public bool Done { get; set; }

        [Description("Why the case was skipped, as a CaseBulkSkipReason; 0 when it was done.")]
        [Key(2)]
        public byte SkipReasonId { get; set; }
    }

    [Description("The outcome of a bulk request: one item per requested case, in request order.")]
    [MessagePackObject]
    public class CaseBulkResultDto
    {
        [Description("One item per distinct requested case.")]
        [Key(0)]
        public List<CaseBulkItemDto> Items { get; set; } = [];

        [Description("How many cases the action was applied to.")]
        [Key(1)]
        public int Done { get; set; }

        [Description("How many cases were skipped.")]
        [Key(2)]
        public int Skipped { get; set; }
    }
}
