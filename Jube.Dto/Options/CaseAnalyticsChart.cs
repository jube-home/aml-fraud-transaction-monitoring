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

    public enum CaseAnalyticsChart
    {
        [Description("Opened, closed and backlog by day")] Flow = 1,
        [Description("Time to first look")] FirstView = 2,
        [Description("Time to resolution")] Resolution = 3,
        [Description("Open cases by age and lane")] Aging = 4,
        [Description("Time in each status")] StatusDwell = 5,
        [Description("Analyst time lost to false positives by rule")] RuleEffort = 6,
        [Description("Rule precision")] RulePrecision = 7,
        [Description("Confirmed cases found as cases are worked")] Gains = 8,
        [Description("Predicted likelihood against what happened")] Calibration = 9,
        [Description("When cases arrive")] Arrivals = 10,
        [Description("Median time to first look by hour of arrival")] FirstViewByHour = 11,
        [Description("Service level attainment by week")] ServiceLevelWeekly = 12,
        [Description("How often cases change hands")] Handoffs = 13
    }
}
