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
using Jube.Dto.Repository.CaseWorkflowChecklistItem;
using ItemPoco = Jube.Data.Poco.CaseWorkflowChecklistItem;

namespace Jube.Service.Repository.CaseWorkflowChecklistItem
{
    internal static class CaseWorkflowChecklistItemMapper
    {
        [return: NotNullIfNotNull(nameof(poco))]
        public static CaseWorkflowChecklistItemDto? ToDto(ItemPoco? poco) => poco is null
            ? null
            : new CaseWorkflowChecklistItemDto
            {
                Id = poco.Id,
                CaseWorkflowId = poco.CaseWorkflowId,
                CaseWorkflowChecklistGuid = poco.CaseWorkflowChecklistGuid,
                Name = poco.Name,
                Priority = poco.Priority.GetValueOrDefault(),
                Active = poco.Active == 1,
                EnableShow = poco.EnableShow == 1,
                ShowSql = poco.ShowSql,
                EnableValidation = poco.EnableValidation == 1,
                ValidateSql = poco.ValidateSql,
                ValidateMessage = poco.ValidateMessage,
                SlaMinutes = poco.SlaMinutes,
                Guid = poco.Guid,
                CreatedDate = ToOffset(poco.CreatedDate),
                CreatedUser = poco.CreatedUser,
                UpdatedUser = poco.UpdatedUser,
                UpdatedDate = ToOffset(poco.UpdatedDate),
                Version = poco.Version.GetValueOrDefault(),
                DeletedUser = poco.DeletedUser,
                DeletedDate = ToOffset(poco.DeletedDate)
            };

        public static List<CaseWorkflowChecklistItemDto> ToDto(IEnumerable<ItemPoco> source) =>
            source.Select(poco => ToDto(poco)).ToList();

        public static ItemPoco ToPoco(CaseWorkflowChecklistItemDto dto) => new()
        {
            Version = dto.Version,
            Id = dto.Id,
            CaseWorkflowId = dto.CaseWorkflowId,
            CaseWorkflowChecklistGuid = dto.CaseWorkflowChecklistGuid,
            Name = dto.Name,
            Priority = dto.Priority,
            Active = (byte)(dto.Active ? 1 : 0),
            EnableShow = (byte)(dto.EnableShow ? 1 : 0),
            ShowSql = Blank(dto.ShowSql),
            EnableValidation = (byte)(dto.EnableValidation ? 1 : 0),
            ValidateSql = Blank(dto.ValidateSql),
            ValidateMessage = Blank(dto.ValidateMessage),
            SlaMinutes = dto.SlaMinutes
        };

        public static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}
