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
using Jube.Dto.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsInstance.Models
{
    [FormEndpoint("AskJoobyAnalyticsInstance")]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Allocation", Order = 20)]
    [FormGroup("Output", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class AskJoobyAnalyticsInstanceDto : IUpdated, IActivatable, IGuidIdentified
    {
        [Description("Human readable name of this analytics instance. Must be unique among the live instances " +
                     "of the tenant, ignoring letter case.")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Identifier of the Model this instance is allocated to, or null when it is not allocated. " +
                     "Only the allocate and deallocate operations change it; it is ignored on create and update.")]
        [FormField(Group = "Allocation", Order = 10, ReadOnly = true)]
        public int? EntityAnalysisModelId { get; set; }

        [Description("Whether the live pipeline returns the analytics output in the response payload. Changing it " +
                     "needs the Promote Model permission.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("Whether every live scoring is written to the reporting table. Changing it needs the " +
                     "Promote Model permission.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("Whether the instance is live in the transaction pipeline of the Model it is allocated to. " +
                     "It can only be true while allocated. Changing it needs the Promote Model permission.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("Server assigned globally unique identifier of the instance. Read only.")]
        [FormField(Group = "Identity", Order = 30, ReadOnly = true)]
        public Guid Guid { get; set; }

        [Description("Server assigned numeric identifier. Read only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created the instance. Server assigned. Read only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("When the instance was created, in UTC. Server assigned. Read only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Not stored for this table, so always null. Read only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("When the instance was last changed, in UTC, or null if it never was. Server assigned. " +
                     "Read only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Change counter, starting at 1 and raised by every change. Read only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted the instance, if it was deleted. Server assigned. Read only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("When the instance was deleted, in UTC, if it was. Server assigned. Read only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}