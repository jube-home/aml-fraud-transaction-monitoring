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
using Jube.Dto.Repository.CaseWorkflowStatus;
using RulePoco = Jube.Data.Poco.CaseWorkflowStatus;

namespace Jube.Service.Repository.CaseWorkflowStatus
{
    internal static class CaseWorkflowStatusMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static CaseWorkflowStatusDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new CaseWorkflowStatusDto
            {
                Id = rulePoco.Id,
                Name = rulePoco.Name,
                Active = rulePoco.Active == 1,
                Locked = rulePoco.Locked == 1,
                Priority = rulePoco.Priority.GetValueOrDefault(),
                ForeColor = rulePoco.ForeColor,
                BackColor = rulePoco.BackColor,
                EnableHttpEndpoint = rulePoco.EnableHttpEndpoint == 1,
                HttpEndpoint = rulePoco.HttpEndpoint,
                HttpEndpointTypeId = rulePoco.HttpEndpointTypeId.GetValueOrDefault(),
                EnableNotification = rulePoco.EnableNotification == 1,
                NotificationTypeId = rulePoco.NotificationTypeId.GetValueOrDefault(),
                NotificationDestination = rulePoco.NotificationDestination,
                NotificationSubject = rulePoco.NotificationSubject,
                NotificationBody = rulePoco.NotificationBody,
                CaseWorkflowId = rulePoco.CaseWorkflowId.GetValueOrDefault(),
                Guid = rulePoco.Guid,
                CreatedDate = ToOffset(rulePoco.CreatedDate),
                CreatedUser = rulePoco.CreatedUser,
                UpdatedUser = rulePoco.UpdatedUser,
                UpdatedDate = ToOffset(rulePoco.UpdatedDate),
                Version = rulePoco.Version.GetValueOrDefault(),
                DeletedUser = rulePoco.DeletedUser,
                DeletedDate = ToOffset(rulePoco.DeletedDate)
            };

        public static List<CaseWorkflowStatusDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();

        public static RulePoco ToPoco(CaseWorkflowStatusDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            Locked = (byte)(dto.Locked ? 1 : 0),
            Priority = (byte)dto.Priority,
            ForeColor = dto.ForeColor,
            BackColor = dto.BackColor,
            EnableHttpEndpoint = (byte)(dto.EnableHttpEndpoint ? 1 : 0),
            HttpEndpoint = dto.HttpEndpoint,
            HttpEndpointTypeId = (byte)dto.HttpEndpointTypeId,
            EnableNotification = (byte)(dto.EnableNotification ? 1 : 0),
            NotificationTypeId = (byte)dto.NotificationTypeId,
            NotificationDestination = dto.NotificationDestination,
            NotificationSubject = dto.NotificationSubject,
            NotificationBody = dto.NotificationBody,
            CaseWorkflowId = dto.CaseWorkflowId
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}