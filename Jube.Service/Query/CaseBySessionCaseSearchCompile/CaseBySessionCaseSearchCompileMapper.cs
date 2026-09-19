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

using Jube.Dto.Query.CaseBySessionCaseSearchCompile;
using Jube.Data.Query.CaseQuery.Dto;

namespace Jube.Service.Query.CaseBySessionCaseSearchCompile
{
    internal static class CaseBySessionCaseSearchCompileMapper
    {
        public static CaseBySessionCaseSearchCompileDto ToDto(CaseQueryDto source) => new()
        {
            Id = source.Id,
            EntityAnalysisModelInstanceEntryGuid = source.EntityAnalysisModelInstanceEntryGuid,
            DiaryDate = source.DiaryDate,
            CaseWorkflowGuid = source.CaseWorkflowGuid,
            CaseWorkflowStatusGuid = source.CaseWorkflowStatusGuid,
            CreatedDate = source.CreatedDate,
            Locked = source.Locked,
            LockedUser = source.LockedUser,
            LockedDate = source.LockedDate,
            ClosedStatusId = source.ClosedStatusId,
            ClosedDate = source.ClosedDate,
            ClosedUser = source.ClosedUser,
            CaseKey = source.CaseKey,
            Diary = source.Diary,
            DiaryUser = source.DiaryUser,
            Rating = source.Rating,
            CaseKeyValue = source.CaseKeyValue,
            LastClosedStatus = source.LastClosedStatus,
            ClosedStatusMigrationDate = source.ClosedStatusMigrationDate,
            ForeColor = source.ForeColor,
            BackColor = source.BackColor,
            Json = source.Json,
            FormattedPayload = source.FormattedPayload?.Select(ToDto).ToList(),
            Activation = source.Activation?.Select(a => new CaseBySessionCaseSearchCompileActivationDto
            {
                Name = a.Name
            }).ToList(),
            EnableVisualisation = source.EnableVisualisation,
            VisualisationRegistryGuid = source.VisualisationRegistryGuid,
            EntityAnalysisModelId = source.EntityAnalysisModelId
        };

        private static CaseBySessionCaseSearchCompileFieldEntryDto ToDto(GetCaseByIdFieldEntryDto source) => new()
        {
            Name = source.Name,
            Value = source.Value,
            CellFormatForeColor = source.CellFormatForeColor,
            CellFormatForeRow = source.CellFormatForeRow,
            CellFormatBackColor = source.CellFormatBackColor,
            CellFormatBackRow = source.CellFormatBackRow,
            ExistsMatch = source.ExistsMatch,
            ConditionalRegularExpressionFormatting = source.ConditionalRegularExpressionFormatting
        };
    }
}