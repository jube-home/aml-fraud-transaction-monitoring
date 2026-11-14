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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSummary
{
    [Description("Where the checklist of the status the case is in stands, read as last brought up to date without " +
                 "running any condition. A case whose checklist has never been brought up to date reports zeros.")]
    [MessagePackObject]
    public class CaseChecklistSummaryDto
    {
        [Description("How many items are required now: those that are shown or done.")]
        [Key(0)]
        public int Required { get; set; }

        [Description("How many of the required items are done.")]
        [Key(1)]
        public int Done { get; set; }

        [Description("How many of the required items are still open.")]
        [Key(2)]
        public int Open { get; set; }

        [Description("How many open items have breached their SLA.")]
        [Key(3)]
        public int Breached { get; set; }

        [Description("The earliest SLA due date among the open timed items (UTC); null when none is timed.")]
        [Key(4)]
        public DateTimeOffset? NextDueDate { get; set; }

        [Description("True when no shown item is still open.")]
        [Key(5)]
        public bool Complete { get; set; }
    }
}
