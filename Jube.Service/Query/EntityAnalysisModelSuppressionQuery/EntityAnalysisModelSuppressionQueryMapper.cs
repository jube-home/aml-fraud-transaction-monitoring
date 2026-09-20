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

using Jube.Dto.Query.EntityAnalysisModelSuppressionQuery;

namespace Jube.Service.Query.EntityAnalysisModelSuppressionQuery
{
    internal static class EntityAnalysisModelSuppressionQueryMapper
    {
        public static EntityAnalysisModelSuppressionQueryDto ToDto(
            global::Jube.Data.Query.GetEntityAnalysisModelSuppressionQuery.Dto source) =>
            new()
            {
                Name = source.Name,
                EntityAnalysisModelGuid = source.EntityAnalysisModelGuid,
                Suppression = source.Suppression,
                DeleteExpiryDate = source.DeleteExpiryDate
            };

        public static List<EntityAnalysisModelSuppressionQueryDto> ToDto(
            IEnumerable<global::Jube.Data.Query.GetEntityAnalysisModelSuppressionQuery.Dto> source) =>
            source.Select(ToDto).ToList();
    }
}