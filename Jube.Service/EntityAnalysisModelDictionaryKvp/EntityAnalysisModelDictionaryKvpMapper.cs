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

using Jube.Dto.EntityAnalysisModelDictionaryKvp;

namespace Jube.Service.EntityAnalysisModelDictionaryKvp
{
    using DictionaryKvpPoco = Data.Poco.EntityAnalysisModelDictionaryKvp;

    internal static class EntityAnalysisModelDictionaryKvpMapper
    {
        public static EntityAnalysisModelDictionaryKvpDto? ToDto(DictionaryKvpPoco? dictionaryKvp)
        {
            return dictionaryKvp is null
                ? null
                : new EntityAnalysisModelDictionaryKvpDto
                {
                    Id = dictionaryKvp.Id,
                    EntityAnalysisModelDictionaryId = dictionaryKvp.EntityAnalysisModelDictionaryId.GetValueOrDefault(),
                    KvpKey = dictionaryKvp.KvpKey,
                    KvpValue = dictionaryKvp.KvpValue,
                    DeleteExpiryDate = ToOffset(dictionaryKvp.DeleteExpiryDate),
                    CreatedUser = dictionaryKvp.CreatedUser,
                    CreatedDate = ToOffset(dictionaryKvp.CreatedDate),
                    UpdatedUser = dictionaryKvp.UpdatedUser,
                    UpdatedDate = ToOffset(dictionaryKvp.UpdatedDate),
                    Version = dictionaryKvp.Version.GetValueOrDefault(),
                    DeletedUser = dictionaryKvp.DeletedUser,
                    DeletedDate = ToOffset(dictionaryKvp.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelDictionaryKvpDto> ToDto(IEnumerable<DictionaryKvpPoco>? source)
        {
            return (source ?? Enumerable.Empty<DictionaryKvpPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static DictionaryKvpPoco ToPoco(EntityAnalysisModelDictionaryKvpDto dto)
        {
            return new DictionaryKvpPoco
            {
                Id = dto.Id,
                EntityAnalysisModelDictionaryId = dto.EntityAnalysisModelDictionaryId,
                KvpKey = dto.KvpKey,
                KvpValue = dto.KvpValue,
                DeleteExpiryDate = dto.DeleteExpiryDate?.UtcDateTime
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