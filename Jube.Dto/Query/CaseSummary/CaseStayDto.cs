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
using Jube.Dto.Query.CaseSearch;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSummary
{
    [Description("One stay of a case in a Case Workflow Status.")]
    [MessagePackObject]
    public class CaseStayDto
    {
        [Description("Guid of the status.")]
        [Key(0)]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("Name of the status.")]
        [Key(1)]
        public string? StatusName { get; set; }

        [Description("Why the case entered: 1 Created, 2 Status changed, 3 Reopened, 4 Escalated.")]
        [Key(2)]
        public int EnterReason { get; set; }

        [Description("When it entered (UTC).")]
        [Key(3)]
        public DateTimeOffset EnteredDate { get; set; }

        [Description("Who moved it in; null when the engine did.")]
        [Key(4)]
        public string? EnteredUser { get; set; }

        [Description("When it left (UTC); null while it is still there.")]
        [Key(5)]
        public DateTimeOffset? ExitedDate { get; set; }

        [Description("Why it left: 1 Moved to another status, 2 Closed.")]
        [Key(6)]
        public int? ExitReason { get; set; }

        [Description("Minutes spent; for a stay still open, the minutes so far.")]
        [Key(7)]
        public double Minutes { get; set; }

        [Description("The most minutes the status allows, when it has a service level.")]
        [Key(8)]
        public int? MaxMinutes { get; set; }
    }
}
