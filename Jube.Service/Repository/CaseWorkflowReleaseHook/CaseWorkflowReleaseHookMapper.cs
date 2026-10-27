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
using Jube.Dto.Repository.CaseWorkflowReleaseHook;
using HookPoco = Jube.Data.Poco.CaseWorkflowReleaseHook;

namespace Jube.Service.Repository.CaseWorkflowReleaseHook
{
    internal static class CaseWorkflowReleaseHookMapper
    {
        [return: NotNullIfNotNull(nameof(hook))]
        public static CaseWorkflowReleaseHookDto? ToDto(HookPoco? hook) => hook is null
            ? null
            : new CaseWorkflowReleaseHookDto
            {
                Id = hook.Id,
                CaseWorkflowId = hook.CaseWorkflowId,
                Name = hook.Name,
                Active = hook.Active == 1,
                Locked = hook.Locked == 1,
                SourceField = hook.SourceField,
                SourceValue = hook.SourceValue,
                ReleaseUrl = hook.ReleaseUrl,
                RefuseUrl = hook.RefuseUrl,
                HttpEndpointTypeId = hook.HttpEndpointTypeId.GetValueOrDefault(),
                HeaderName = hook.HeaderName,
                CredentialVariable = hook.CredentialVariable,
                Body = hook.Body,
                Guid = hook.Guid,
                CreatedDate = ToOffset(hook.CreatedDate),
                CreatedUser = hook.CreatedUser,
                UpdatedUser = hook.UpdatedUser,
                UpdatedDate = ToOffset(hook.UpdatedDate),
                Version = hook.Version.GetValueOrDefault(),
                DeletedUser = hook.DeletedUser,
                DeletedDate = ToOffset(hook.DeletedDate)
            };

        public static List<CaseWorkflowReleaseHookDto> ToDto(IEnumerable<HookPoco> source) =>
            source.Select(hook => ToDto(hook)).ToList();

        public static HookPoco ToPoco(CaseWorkflowReleaseHookDto dto) => new()
        {
            Version = dto.Version,
            Id = dto.Id,
            CaseWorkflowId = dto.CaseWorkflowId,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            Locked = (byte)(dto.Locked ? 1 : 0),
            SourceField = dto.SourceField,
            SourceValue = dto.SourceValue,
            ReleaseUrl = dto.ReleaseUrl,
            RefuseUrl = dto.RefuseUrl,
            HttpEndpointTypeId = (byte)dto.HttpEndpointTypeId,
            HeaderName = dto.HeaderName,
            CredentialVariable = dto.CredentialVariable,
            Body = dto.Body
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}
