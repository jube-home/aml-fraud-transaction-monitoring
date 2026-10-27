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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSummary
{
    [Description("One stay of a case in a Case Workflow Status.")]
    public class CaseStayDto
    {
        [Description("Guid of the status.")]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("Name of the status.")]
        public string? StatusName { get; set; }

        [Description("Why the case entered: 1 Created, 2 Status changed, 3 Reopened, 4 Escalated.")]
        public int EnterReason { get; set; }

        [Description("When it entered (UTC).")]
        public DateTimeOffset EnteredDate { get; set; }

        [Description("Who moved it in; null when the engine did.")]
        public string? EnteredUser { get; set; }

        [Description("When it left (UTC); null while it is still there.")]
        public DateTimeOffset? ExitedDate { get; set; }

        [Description("Why it left: 1 Moved to another status, 2 Closed.")]
        public int? ExitReason { get; set; }

        [Description("Minutes spent; for a stay still open, the minutes so far.")]
        public double Minutes { get; set; }

        [Description("The most minutes the status allows, when it has a service level.")]
        public int? MaxMinutes { get; set; }
    }
}
