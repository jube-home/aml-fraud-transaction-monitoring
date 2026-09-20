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

using Jube.Dto.Repository.SessionCaseSearchCompiledSql;

namespace Jube.Service.Repository.SessionCaseSearchCompiledSql
{
    internal static class SessionCaseSearchCompiledSqlMapper
    {
        public static SessionCaseSearchCompiledSqlDto ToDto(Data.Poco.SessionCaseSearchCompiledSql source) => new()
        {
            Id = source.Id,
            Guid = source.Guid,
            FilterJson = source.FilterJson,
            FilterTokens = source.FilterTokens,
            SelectJson = source.SelectJson,
            Prepared = source.Prepared.GetValueOrDefault(),
            Error = source.Error,
            CaseWorkflowGuid = source.CaseWorkflowGuid,
            CaseWorkflowFilterGuid = source.CaseWorkflowFilterGuid,
            CreatedUser = source.CreatedUser,
            CreatedDate = ToOffset(source.CreatedDate),
            Rebuild = source.Rebuild.GetValueOrDefault(),
            RebuildDate = ToOffset(source.RebuildDate)
        };

        private static DateTimeOffset? ToOffset(DateTime? value) => value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
    }
}