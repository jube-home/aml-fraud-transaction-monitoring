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

using Jube.Dto.EntityAnalysisModelDictionary;

namespace Jube.Service.EntityAnalysisModelDictionary
{
    using DictionaryPoco = Data.Poco.EntityAnalysisModelDictionary;

    internal static class EntityAnalysisModelDictionaryMapper
    {
        public static EntityAnalysisModelDictionaryDto? ToDto(DictionaryPoco? dictionary)
        {
            return dictionary is null
                ? null
                : new EntityAnalysisModelDictionaryDto
                {
                    Id = dictionary.Id,
                    EntityAnalysisModelGuid = dictionary.EntityAnalysisModelGuid,
                    Name = dictionary.Name,
                    DataName = dictionary.DataName,
                    Active = dictionary.Active == 1,
                    Locked = dictionary.Locked == 1,
                    ResponsePayload = dictionary.ResponsePayload == 1,
                    CreatedUser = dictionary.CreatedUser,
                    CreatedDate = ToOffset(dictionary.CreatedDate),
                    UpdatedUser = dictionary.UpdatedUser,
                    UpdatedDate = ToOffset(dictionary.UpdatedDate),
                    Version = dictionary.Version.GetValueOrDefault(),
                    DeletedUser = dictionary.DeletedUser,
                    DeletedDate = ToOffset(dictionary.DeletedDate)
                };
        }

        public static List<EntityAnalysisModelDictionaryDto> ToDto(IEnumerable<DictionaryPoco>? source)
        {
            return (source ?? Enumerable.Empty<DictionaryPoco>()).Select(p => ToDto(p)!).ToList();
        }

        public static DictionaryPoco ToPoco(EntityAnalysisModelDictionaryDto dto)
        {
            return new DictionaryPoco
            {
                Id = dto.Id,
                EntityAnalysisModelGuid = dto.EntityAnalysisModelGuid,
                Name = dto.Name,
                DataName = dto.DataName,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0),
                ResponsePayload = (byte)(dto.ResponsePayload ? 1 : 0)
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