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

namespace Jube.Data.Query.GetCaseTriagePageQuery
{
    using System;
    using System.Collections.Generic;

    public class GetCaseTriagePageQueryDto
    {
        public long Total { get; set; }
        public List<Row> Rows { get; set; } = [];

        public class Row
        {
            public int CaseId { get; set; }
            public string CaseKey { get; set; }
            public string CaseKeyValue { get; set; }
            public DateTime CreatedDate { get; set; }
            public bool Locked { get; set; }
            public string LockedUser { get; set; }
            public DateTime? LockedDate { get; set; }
            public int ClosedStatusId { get; set; }
            public DateTime? ClosedDate { get; set; }
            public bool Diary { get; set; }
            public DateTime? DiaryDate { get; set; }
            public int DiaryTypeId { get; set; }
            public bool QaSample { get; set; }
            public Guid CaseWorkflowGuid { get; set; }
            public Guid CaseWorkflowStatusGuid { get; set; }
            public int Rating { get; set; }
            public string StatusName { get; set; }
            public string ForeColor { get; set; }
            public string BackColor { get; set; }
            public int? MaxDwellMinutes { get; set; }
            public string WorkflowName { get; set; }
            public int? FirstViewTargetMinutes { get; set; }
            public int? OwnershipTargetMinutes { get; set; }
            public int? ResolutionTargetMinutes { get; set; }
            public int? RiskSequence { get; set; }
            public int? LaneId { get; set; }
            public int? Priority { get; set; }
            public double? LikelihoodProbability { get; set; }
            public int? LikelihoodBandId { get; set; }
            public int? ImpactId { get; set; }
            public double? ExpectedHarm { get; set; }
            public DateTime? NextDeadlineDate { get; set; }
            public bool DeadlineBreached { get; set; }
            public string LaneReason { get; set; }
            public int? MethodId { get; set; }
            public int EvidenceCount { get; set; }
            public DateTime? RiskDate { get; set; }
            public string TopDriver { get; set; }
            public string TopDriverFamily { get; set; }
            public double? Value { get; set; }
            public bool Viewed { get; set; }
            public DateTime? LastViewedDate { get; set; }
            public bool Mine { get; set; }
            public bool AttReopened { get; set; }
            public bool AttEscalated { get; set; }
            public bool AttMoved { get; set; }
            public bool AttDiaryDue { get; set; }
            public int PriorityRise { get; set; }
            public int EventsSince { get; set; }
            public bool Attention { get; set; }
        }
    }
}
