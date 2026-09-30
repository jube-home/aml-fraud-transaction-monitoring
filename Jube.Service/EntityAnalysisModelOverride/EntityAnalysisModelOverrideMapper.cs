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
using Jube.Dto.EntityAnalysisModelOverride;
using Jube.Dto.Overrides;

namespace Jube.Service.EntityAnalysisModelOverride
{
    using OverridePoco = Data.Poco.EntityAnalysisModelOverride;

    internal static class EntityAnalysisModelOverrideMapper
    {
        [return: NotNullIfNotNull(nameof(overrideRow))]
        public static EntityAnalysisModelOverrideDto? ToDto(OverridePoco? overrideRow)
        {
            return overrideRow is null
                ? null
                : new EntityAnalysisModelOverrideDto
                {
                    Id = overrideRow.Id,
                    EntityAnalysisModelGuid = overrideRow.EntityAnalysisModelGuid,
                    OverrideKey = overrideRow.OverrideKey,
                    OverrideKeyValue = overrideRow.OverrideKeyValue,
                    CreatedUser = overrideRow.CreatedUser,
                    CreatedDate = ToOffset(overrideRow.CreatedDate),
                    Version = overrideRow.Version.GetValueOrDefault(),
                    DeletedUser = overrideRow.DeletedUser,
                    DeletedDate = ToOffset(overrideRow.DeletedDate),
                    DeleteExpiryDate = ToOffset(overrideRow.DeleteExpiryDate),
                    OverrideKind = overrideRow.OverrideKind == (byte)EntityAnalysisModelOverrideKind.Force
                        ? EntityAnalysisModelOverrideKind.Force
                        : EntityAnalysisModelOverrideKind.Suppress
                };
        }

        public static List<EntityAnalysisModelOverrideDto> ToDto(IEnumerable<OverridePoco>? source)
        {
            return (source ?? Enumerable.Empty<OverridePoco>()).Select(p => ToDto(p)).ToList();
        }

        public static OverridePoco ToPoco(EntityAnalysisModelOverrideDto dto)
        {
            return new OverridePoco
            {
                Id = dto.Id,
                EntityAnalysisModelGuid = dto.EntityAnalysisModelGuid,
                OverrideKey = dto.OverrideKey,
                OverrideKeyValue = dto.OverrideKeyValue,
                OverrideKind = (byte)dto.OverrideKind
            };
        }

        private static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}