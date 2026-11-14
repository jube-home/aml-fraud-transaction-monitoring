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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.EntityAnalysisModelSvirfd
{
    [FormEndpoint("EntityAnalysisModelSvirfd")]
    [FormApproval]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Field", Order = 20)]
    [FormGroup("Output", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class EntityAnalysisModelSvirfdDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the model this Svirfd entry belongs to. Set from the parent context; not " +
                     "user-editable.")]
        [Key(0)]
        public int EntityAnalysisModelId { get; set; }

        [Description("Server-assigned globally-unique identifier. Read-only.")]
        [Key(1)]
        public Guid Guid { get; set; }

        [Description("The entry name rules read the IP intelligence record by, as Svirfd.{Name}.{Member}, for example " +
                     "Svirfd.Customer.Privacy.Vpn. Letters and digits only, starting with a letter; unique within the " +
                     "model.")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Customer")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(2)]
        public string? Name { get; set; }

        [Description("When true, the address is looked up on every invocation of the model.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [NewDefault(true)]
        [Key(3)]
        public bool Active { get; set; }

        [Description("When true, this entry is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        [Key(4)]
        public bool Locked { get; set; }

        [Description("The request field of this model, of data type IP, whose value is the address looked up.")]
        [FormField(Group = "Field", Order = 10, Widget = "select")]
        [ListColumn(Order = 30, Title = "Field")]
        [Lookup("/api/EntityAnalysisModelSvirfd/Fields", TextField = "label", ValueField = "key", ParentField = nameof(EntityAnalysisModelId))]
        [Key(5)]
        public string? RequestXPathName { get; set; }

        [Description("When true, the whole IP intelligence record is returned in the response JSON under Svirfd.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        [Key(6)]
        public bool ResponsePayload { get; set; }

        [Description("When true, the record is written into the archive JSON for reporting, training and the case " +
                     "journal, which compares it with a fresh lookup to show drift.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch", Label = "Archive")]
        [NewDefault(true)]
        [Key(7)]
        public bool ReportTable { get; set; }

        [Description("When true, the plain-English narrative is rendered onto the record, so it is returned in the " +
                     "response and archived with it. Turn off to keep the record to data alone.")]
        [FormField(Group = "Output", Order = 30, Widget = "switch", Label = "Narrative")]
        [NewDefault(true)]
        [Key(16)]
        public bool Narrative { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing row.")]
        [Key(8)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(9)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this row. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(10)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this row. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(11)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(12)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(13)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this row, if any. Read-only.")]
        [Key(14)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this row was soft-deleted, if any. Read-only.")]
        [Key(15)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
