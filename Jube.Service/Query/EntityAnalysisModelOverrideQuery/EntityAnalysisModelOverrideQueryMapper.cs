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

using Jube.Dto.Overrides;
using Jube.Dto.Query.EntityAnalysisModelOverrideQuery;

namespace Jube.Service.Query.EntityAnalysisModelOverrideQuery
{
    internal static class EntityAnalysisModelOverrideQueryMapper
    {
        private static EntityAnalysisModelOverrideQueryDto ToDto(
            global::Jube.Data.Query.Models.EntityAnalysisModelOverrideQueryRow source)
        {
            return new EntityAnalysisModelOverrideQueryDto
            {
                Name = source.Name,
                EntityAnalysisModelGuid = source.EntityAnalysisModelGuid,
                HasOverride = source.HasOverride,
                OverrideKind = source.OverrideKind == (byte)EntityAnalysisModelOverrideKind.Force
                    ? EntityAnalysisModelOverrideKind.Force
                    : EntityAnalysisModelOverrideKind.Suppress,
                DeleteExpiryDate = source.DeleteExpiryDate,
                OverrideId = source.OverrideId,
                PendingRemoval = source.PendingRemoval
            };
        }

        private static EntityAnalysisModelOverrideValueQueryDto ToDto(
            global::Jube.Data.Query.Models.EntityAnalysisModelOverrideValueQueryRow source)
        {
            return new EntityAnalysisModelOverrideValueQueryDto
            {
                OverrideKeyValue = source.OverrideKeyValue,
                Name = source.Name,
                EntityAnalysisModelGuid = source.EntityAnalysisModelGuid,
                OverrideKind = source.OverrideKind == (byte)EntityAnalysisModelOverrideKind.Force
                    ? EntityAnalysisModelOverrideKind.Force
                    : EntityAnalysisModelOverrideKind.Suppress,
                CreatedUser = source.CreatedUser,
                CreatedDate = source.CreatedDate,
                DeleteExpiryDate = source.DeleteExpiryDate
            };
        }

        private static EntityAnalysisModelOverrideKeyQueryDto ToDto(
            global::Jube.Data.Query.Models.EntityAnalysisModelOverrideKeyQueryRow source)
        {
            return new EntityAnalysisModelOverrideKeyQueryDto
            {
                OverrideKey = source.OverrideKey,
                EnabledOnModels = source.EnabledOnModels,
                Values = source.Values,
                Overrides = source.Overrides,
                Forced = source.Forced,
                ActivationRules = source.ActivationRules,
                AllActivationRules = source.AllActivationRules,
                NextExpiryDate = source.NextExpiryDate,
                LastCreatedDate = source.LastCreatedDate
            };
        }

        public static List<EntityAnalysisModelOverrideKeyQueryDto> ToDto(
            IEnumerable<global::Jube.Data.Query.Models.EntityAnalysisModelOverrideKeyQueryRow> source)
        {
            return source.Select(ToDto).ToList();
        }

        public static List<EntityAnalysisModelOverrideValueQueryDto> ToDto(
            IEnumerable<global::Jube.Data.Query.Models.EntityAnalysisModelOverrideValueQueryRow> source)
        {
            return source.Select(ToDto).ToList();
        }

        public static List<EntityAnalysisModelOverrideQueryDto> ToDto(
            IEnumerable<global::Jube.Data.Query.Models.EntityAnalysisModelOverrideQueryRow> source)
        {
            return source.Select(ToDto).ToList();
        }
    }
}