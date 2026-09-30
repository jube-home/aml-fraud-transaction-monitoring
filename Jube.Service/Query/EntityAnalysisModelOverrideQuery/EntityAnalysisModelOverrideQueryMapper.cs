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

using Jube.Dto.Query.EntityAnalysisModelOverrideQuery;
using Jube.Dto.Overrides;

namespace Jube.Service.Query.EntityAnalysisModelOverrideQuery
{
    internal static class EntityAnalysisModelOverrideQueryMapper
    {
        private static EntityAnalysisModelOverrideQueryDto ToDto(
            global::Jube.Data.Query.Models.EntityAnalysisModelOverrideQueryRow source) =>
            new()
            {
                Name = source.Name,
                EntityAnalysisModelGuid = source.EntityAnalysisModelGuid,
                HasOverride = source.HasOverride,
                OverrideKind = source.OverrideKind == (byte)EntityAnalysisModelOverrideKind.Force
                    ? EntityAnalysisModelOverrideKind.Force
                    : EntityAnalysisModelOverrideKind.Suppress,
                DeleteExpiryDate = source.DeleteExpiryDate
            };

        private static EntityAnalysisModelOverrideValueQueryDto ToDto(
            global::Jube.Data.Query.Models.EntityAnalysisModelOverrideValueQueryRow source) =>
            new()
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

        private static EntityAnalysisModelOverrideKeyQueryDto ToDto(
            global::Jube.Data.Query.Models.EntityAnalysisModelOverrideKeyQueryRow source) =>
            new()
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

        public static List<EntityAnalysisModelOverrideKeyQueryDto> ToDto(
            IEnumerable<global::Jube.Data.Query.Models.EntityAnalysisModelOverrideKeyQueryRow> source) =>
            source.Select(ToDto).ToList();

        public static List<EntityAnalysisModelOverrideValueQueryDto> ToDto(
            IEnumerable<global::Jube.Data.Query.Models.EntityAnalysisModelOverrideValueQueryRow> source) =>
            source.Select(ToDto).ToList();

        public static List<EntityAnalysisModelOverrideQueryDto> ToDto(
            IEnumerable<global::Jube.Data.Query.Models.EntityAnalysisModelOverrideQueryRow> source) =>
            source.Select(ToDto).ToList();
    }
}