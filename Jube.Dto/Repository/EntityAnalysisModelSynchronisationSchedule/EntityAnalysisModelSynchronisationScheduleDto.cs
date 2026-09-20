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

using System.ComponentModel;
using Jube.Dto.Forms;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Repository.EntityAnalysisModelSynchronisationSchedule
{
    [FormEndpoint("EntityAnalysisModelSynchronisationSchedule")]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(ScheduleDate))]
    [FormGroup("Schedule", Order = 10)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelSynchronisationScheduleDto
    {
        [Description("Server-assigned row identifier. Read-only.")]
        public int Id { get; set; }

        [Description("The date and time (UTC) that model synchronisation across the cluster is scheduled for. " +
                     "When omitted on create, the server schedules synchronisation for the current time -- an " +
                     "immediate 'Synchronise Now'.")]
        [FormField(Group = "Schedule", Order = 10, Widget = "date")]
        [ListColumn(Order = 10, Title = "Scheduled For")]
        public DateTimeOffset? ScheduleDate { get; set; }

        [Description("Identifier of the tenant this schedule row belongs to. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int TenantRegistryId { get; set; }

        [Description("User who created this row. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [ListColumn(Order = 20, Title = "Created By")]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this row was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [ListColumn(Order = 30, Title = "Created")]
        public DateTimeOffset? CreatedDate { get; set; }
    }
}