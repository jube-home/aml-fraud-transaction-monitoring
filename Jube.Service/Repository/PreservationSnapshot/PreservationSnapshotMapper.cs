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

using Jube.Dto.Repository.PreservationSnapshot;

namespace Jube.Service.Repository.PreservationSnapshot
{
    using SnapshotPoco = Data.Poco.PreservationSnapshot;

    internal static class PreservationSnapshotMapper
    {
        public static PreservationSnapshotDto ToDto(SnapshotPoco snapshot)
        {
            return new PreservationSnapshotDto
            {
                Id = snapshot.Id,
                Guid = snapshot.Guid,
                SnapshotSourceId = (PreservationSnapshotSource)snapshot.SnapshotSourceId,
                Name = snapshot.Name,
                ExportVersion = snapshot.ExportVersion,
                ExportGuid = snapshot.ExportGuid,
                EntityAnalysisModelCount = snapshot.EntityAnalysisModelCount,
                Bytes = snapshot.Bytes,
                InError = snapshot.InError == 1,
                CreatedUser = snapshot.CreatedUser,
                CreatedDate = ToOffset(snapshot.CreatedDate),
                CompletedDate = ToOffset(snapshot.CompletedDate)
            };
        }

        public static List<PreservationSnapshotDto> ToDto(IEnumerable<SnapshotPoco>? source)
        {
            return (source ?? Enumerable.Empty<SnapshotPoco>()).Select(ToDto).ToList();
        }

        private static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}