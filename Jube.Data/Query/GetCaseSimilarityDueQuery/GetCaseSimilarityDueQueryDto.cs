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

namespace Jube.Data.Query.GetCaseSimilarityDueQuery
{
    using System;

    public class GetCaseSimilarityDueQueryDto
    {
        public int CaseId { get; set; }
        public string CaseKey { get; set; }
        public string CaseKeyValue { get; set; }
        public Guid CaseWorkflowGuid { get; set; }
        public int? EntityAnalysisModelId { get; set; }
        public int? TenantRegistryId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public byte? ClosedStatusId { get; set; }
        public byte? RiskLaneId { get; set; }
        public double? RiskValue { get; set; }
    }
}
