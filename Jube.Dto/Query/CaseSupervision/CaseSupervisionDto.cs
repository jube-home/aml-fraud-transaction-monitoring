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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSupervision
{
    [Description("The workload of every analyst who can work a Case Workflow, with the open cases nobody holds.")]
    public class CaseSupervisionDto
    {
        [Description("Guid of the Case Workflow.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("Start of the period (UTC).")]
        public DateTimeOffset From { get; set; }

        [Description("End of the period (UTC).")]
        public DateTimeOffset To { get; set; }

        [Description("Open cases in the workflow that nobody holds.")]
        public int Unassigned { get; set; }

        [Description("Open cases in the workflow that someone holds.")]
        public int Held { get; set; }

        [Description("Every analyst who can work the workflow, busiest first, plus anyone else with activity or a " +
                     "hold in it.")]
        public List<CaseAnalystWorkloadDto> Analysts { get; set; } = [];
    }
}
