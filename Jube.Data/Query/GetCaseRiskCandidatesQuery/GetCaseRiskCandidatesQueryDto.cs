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

namespace Jube.Data.Query.GetCaseRiskCandidatesQuery
{
    using System;

    public class GetCaseRiskCandidatesQueryDto
    {
        public int CaseId { get; set; }
        public Guid CaseWorkflowGuid { get; set; }
        public string CaseKey { get; set; }
        public string CaseKeyValue { get; set; }
        public Guid? EntityAnalysisModelActivationRuleGuid { get; set; }
        public byte? Diary { get; set; }
        public DateTime? DiaryDate { get; set; }
        public string Json { get; set; }
        public bool HasAssessment { get; set; }
        public DateTime? RiskCheckedDate { get; set; }
        public byte DiaryTypeId { get; set; }
        public byte? RiskRecheckTriggerId { get; set; }
        public byte QaSampleStatusId { get; set; }
    }
}
