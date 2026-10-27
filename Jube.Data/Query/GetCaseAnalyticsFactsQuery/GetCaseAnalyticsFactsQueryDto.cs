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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Data.Query.GetCaseAnalyticsFactsQuery
{
    using System;
    using System.Collections.Generic;

    public class GetCaseAnalyticsFactsQueryDto
    {
        public bool Truncated { get; set; }
        public List<Row> Rows { get; set; } = [];

        public class Row
        {
            public int CaseId { get; set; }
            public Guid CaseWorkflowStatusGuid { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime? ClosedDate { get; set; }
            public bool Closed { get; set; }
            public int OutcomeId { get; set; }
            public Guid? RuleGuid { get; set; }
            public string RuleName { get; set; }
            public DateTime? FirstViewedDate { get; set; }
            public DateTime? FirstOwnedDate { get; set; }
            public int Reopens { get; set; }
            public int Handoffs { get; set; }
            public int? LaneId { get; set; }
            public int? Priority { get; set; }
            public double? Likelihood { get; set; }
            public int? ImpactId { get; set; }
            public int? FirstViewTargetMinutes { get; set; }
            public int? OwnershipTargetMinutes { get; set; }
            public int? ResolutionTargetMinutes { get; set; }
            public DateTime? StayEnteredDate { get; set; }
            public int? MaxDwellMinutes { get; set; }
            public string ClosedUser { get; set; }
            public string LockedUser { get; set; }
            public bool QaSample { get; set; }
        }
    }
}
