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
using Jube.Dto.Repository.Case;
using RulePoco = Jube.Data.Poco.Case;

namespace Jube.Service.Repository.Case
{
    internal static class CaseMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static CaseDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new CaseDto
            {
                Id = rulePoco.Id,
                DiaryDate = ToOffset(rulePoco.DiaryDate),
                CaseWorkflowStatusGuid = rulePoco.CaseWorkflowStatusGuid,
                CreatedDate = ToOffset(rulePoco.CreatedDate),
                Locked = rulePoco.Locked == 1,
                LockedUser = rulePoco.LockedUser,
                LockedDate = ToOffset(rulePoco.LockedDate),
                ClosedStatusId = rulePoco.ClosedStatusId.GetValueOrDefault(),
                ClosedDate = ToOffset(rulePoco.ClosedDate),
                ClosedUser = rulePoco.ClosedUser,
                CaseKey = rulePoco.CaseKey,
                Diary = rulePoco.Diary == 1,
                DiaryUser = rulePoco.DiaryUser,
                Rating = rulePoco.Rating.GetValueOrDefault(),
                Json = rulePoco.Json,
                CaseKeyValue = rulePoco.CaseKeyValue,
                LastClosedStatus = rulePoco.LastClosedStatus.GetValueOrDefault(),
                Payload = null
            };

        public static List<CaseDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();

        public static RulePoco ToPoco(CaseDto caseDto) => new()
        {
            Id = caseDto.Id,
            DiaryDate = caseDto.DiaryDate?.UtcDateTime,
            CaseWorkflowStatusGuid = caseDto.CaseWorkflowStatusGuid,
            Locked = (byte)(caseDto.Locked ? 1 : 0),
            LockedUser = caseDto.LockedUser,
            ClosedStatusId = caseDto.ClosedStatusId,
            CaseKey = caseDto.CaseKey,
            Diary = (byte)(caseDto.Diary ? 1 : 0),
            Rating = caseDto.Rating,
            CaseKeyValue = caseDto.CaseKeyValue
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}