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

using Jube.Dto.Repository.CaseSearchFilter;

namespace Jube.Service.Repository.CaseSearchFilter
{
    internal static class CaseSearchFilterMapper
    {
        public static CaseSearchFilterDto ToDto(Data.Poco.CaseSearchFilter source, Guid caseWorkflowGuid,
            string callerUserName) => new()
        {
            Guid = source.Guid,
            CaseWorkflowGuid = caseWorkflowGuid,
            Name = source.Name,
            FilterJson = source.FilterJson,
            SelectJson = source.SelectJson,
            SharedRoleRegistryGuid = source.SharedRoleRegistryGuid,
            Mine = source.CreatedUser == callerUserName,
            CreatedUser = source.CreatedUser,
            CreatedDate = ToOffset(source.CreatedDate),
            UpdatedUser = source.UpdatedUser,
            UpdatedDate = ToOffset(source.UpdatedDate),
            Version = source.Version
        };

        public static Data.Poco.CaseSearchFilter ToPoco(CaseSearchFilterDto source, int caseWorkflowId) => new()
        {
            Guid = source.Guid,
            CaseWorkflowId = caseWorkflowId,
            Name = source.Name?.Trim(),
            FilterJson = source.FilterJson,
            SelectJson = source.SelectJson,
            SharedRoleRegistryGuid = source.SharedRoleRegistryGuid,
            Version = source.Version
        };

        private static DateTimeOffset? ToOffset(DateTime? value) => value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
    }
}
