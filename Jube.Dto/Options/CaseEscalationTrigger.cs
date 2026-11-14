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

namespace Jube.Dto.Options
{
    using System.ComponentModel;

    public enum CaseEscalationTrigger
    {
        [Description("Time in status reaches the threshold in minutes")] TimeInStatus = 1,
        [Description("Case is older than the workflow resolution target")] ResolutionTargetBreached = 2,
        [Description("Case lane is at or more urgent than the threshold lane")] LaneAtLeastUrgent = 3,
        [Description("Case Priority is at or above the threshold")] PriorityAtLeast = 4,
        [Description("The case diary deadline has passed")] DeadlineOverdue = 5
    }
}
