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

using System.Diagnostics.CodeAnalysis;
using Jube.Dto.Query.CaseById;
using Jube.Data.Query.CaseQuery.Dto;

namespace Jube.Service.Query.CaseById
{
    internal static class CaseByIdMapper
    {
        [return: NotNullIfNotNull(nameof(source))]
        public static CaseByIdDto? ToDto(CaseQueryDto? source) => source is null
            ? null
            : new CaseByIdDto
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
                FormattedPayload = source.FormattedPayload?.Select(entry => new CaseByIdFieldEntryDto
                {
                    Name = entry.Name,
                    Value = entry.Value,
                    CellFormatForeColor = entry.CellFormatForeColor,
                    CellFormatForeRow = entry.CellFormatForeRow,
                    CellFormatBackColor = entry.CellFormatBackColor,
                    CellFormatBackRow = entry.CellFormatBackRow,
                    ExistsMatch = entry.ExistsMatch,
                    ConditionalRegularExpressionFormatting = entry.ConditionalRegularExpressionFormatting
                }).ToList(),
                Activation = source.Activation?.Select(activation => new CaseByIdActivationDto
                {
                    Name = activation.Name
                }).ToList(),
                EnableVisualisation = source.EnableVisualisation,
                VisualisationRegistryGuid = source.VisualisationRegistryGuid,
                EntityAnalysisModelId = source.EntityAnalysisModelId
            };
    }
}