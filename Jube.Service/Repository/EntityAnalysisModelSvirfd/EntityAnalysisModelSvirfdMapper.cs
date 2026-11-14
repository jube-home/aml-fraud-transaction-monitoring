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
using Jube.Dto.Repository.EntityAnalysisModelSvirfd;
using SvirfdPoco = Jube.Data.Poco.EntityAnalysisModelSvirfd;

namespace Jube.Service.Repository.EntityAnalysisModelSvirfd
{
    internal static class EntityAnalysisModelSvirfdMapper
    {
        [return: NotNullIfNotNull(nameof(svirfdPoco))]
        public static EntityAnalysisModelSvirfdDto? ToDto(SvirfdPoco? svirfdPoco) => svirfdPoco is null
            ? null
            : new EntityAnalysisModelSvirfdDto
            {
                Id = svirfdPoco.Id,
                EntityAnalysisModelId = svirfdPoco.EntityAnalysisModelId.GetValueOrDefault(),
                Guid = svirfdPoco.Guid,
                Name = svirfdPoco.Name,
                Active = svirfdPoco.Active == 1,
                Locked = svirfdPoco.Locked == 1,
                RequestXPathName = svirfdPoco.RequestXPathName,
                ResponsePayload = svirfdPoco.ResponsePayload == 1,
                ReportTable = svirfdPoco.ReportTable == 1,
                Narrative = (svirfdPoco.Narrative ?? 1) == 1,
                CreatedDate = ToOffset(svirfdPoco.CreatedDate),
                CreatedUser = svirfdPoco.CreatedUser,
                UpdatedUser = svirfdPoco.UpdatedUser,
                UpdatedDate = ToOffset(svirfdPoco.UpdatedDate),
                Version = svirfdPoco.Version.GetValueOrDefault(),
                DeletedUser = svirfdPoco.DeletedUser,
                DeletedDate = ToOffset(svirfdPoco.DeletedDate)
            };

        public static List<EntityAnalysisModelSvirfdDto> ToDto(IEnumerable<SvirfdPoco> source) =>
            [.. source.Select(svirfdPoco => ToDto(svirfdPoco))];

        public static SvirfdPoco ToPoco(EntityAnalysisModelSvirfdDto dto) => new()
        {
            Version = dto.Version,
            Id = dto.Id,
            EntityAnalysisModelId = dto.EntityAnalysisModelId,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            Locked = (byte)(dto.Locked ? 1 : 0),
            RequestXPathName = dto.RequestXPathName,
            ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0),
            ReportTable = (byte)(dto.ReportTable ? 1 : 0),
            Narrative = (byte)(dto.Narrative ? 1 : 0)
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}
