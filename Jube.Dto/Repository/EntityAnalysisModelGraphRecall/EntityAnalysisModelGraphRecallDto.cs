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

namespace Jube.Dto.Repository.EntityAnalysisModelGraphRecall
{
    [FormEndpoint("EntityAnalysisModelGraphRecall")]
    [FormApproval]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Entity", Order = 20)]
    [FormGroup("Output", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class EntityAnalysisModelGraphRecallDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the model this graph recall entity belongs to. Set from the parent context; not " +
                     "user-editable.")]
        [Key(0)]
        public int EntityAnalysisModelId { get; set; }

        [Description("Server-assigned globally-unique identifier. Read-only.")]
        [Key(1)]
        public Guid Guid { get; set; }

        [Description("The entity name rules read it by, as Graph.{Name}, for example AccountId or Payee. Letters and " +
                     "digits only, starting with a letter; unique within the model.")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Payee")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(2)]
        public string? Name { get; set; }

        [Description("When true, the entity is recalled from the graph on every invocation of the model.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [NewDefault(true)]
        [Key(3)]
        public bool Active { get; set; }

        [Description("When true, this entity is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        [Key(4)]
        public bool Locked { get; set; }

        [Description("The request field of this model, flagged Archive Key, whose value identifies the entity in " +
                     "the graph, such as AccountId or ToAccountId.")]
        [FormField(Group = "Entity", Order = 10, Widget = "select")]
        [ListColumn(Order = 30, Title = "Field")]
        [Lookup("/api/EntityAnalysisModelGraphRecall/Fields", TextField = "label", ValueField = "key", ParentField = nameof(EntityAnalysisModelId))]
        [Key(5)]
        public string? RequestXPathName { get; set; }

        [Description("The graph node key the value is filed under when it differs from the field name, as an edge " +
                     "definition's To Node Key does: a ToAccountId value is filed as an AccountId node. Leave empty " +
                     "to use the field name.")]
        [FormField(Group = "Entity", Order = 20, Widget = "select")]
        [Lookup("/api/EntityAnalysisModelGraphRecall/Fields", TextField = "label", ValueField = "key", ParentField = nameof(EntityAnalysisModelId))]
        [Key(6)]
        public string? NodeKey { get; set; }

        [Description("The name of another entity of this model this one is joined to for edge facts (count, amount, " +
                     "first seen) and bridge detection, such as a Payer entity joined to its Payee. Joins are explicit " +
                     "and never guessed; leave empty for none.")]
        [FormField(Group = "Entity", Order = 30, Widget = "select")]
        [ListColumn(Order = 40, Title = "Counterpart")]
        [Lookup("/api/EntityAnalysisModelGraphRecall/ByEntityAnalysisModelId", TextField = "name", ValueField = "name", ParentField = nameof(EntityAnalysisModelId), AllowEmpty = true)]
        [Key(7)]
        public string? CounterpartName { get; set; }

        [Description("The Graph Registry whose edge definitions scope the edge facts against the counterpart. Leave " +
                     "empty to use every active definition of the tenant.")]
        [FormField(Group = "Entity", Order = 40, Widget = "select")]
        [Lookup("/api/GraphRegistry", TextField = "name", ValueField = "guid")]
        [Key(8)]
        public Guid? GraphRegistryGuid { get; set; }

        [Description("When true, the entity object is returned in the response JSON under Graph. Exclusive with " +
                     "Archive Key: an entity is returned or stored, not both.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        [Key(9)]
        public bool ResponsePayload { get; set; }

        [Description("When true, the entity object is written into the archive JSON for reporting and training. " +
                     "Exclusive with Response Payload.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch", Label = "Archive Key")]
        [NewDefault(true)]
        [Key(10)]
        public bool ReportTable { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing row.")]
        [Key(11)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(12)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this row. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(13)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this row. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(14)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(15)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(16)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this row, if any. Read-only.")]
        [Key(17)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this row was soft-deleted, if any. Read-only.")]
        [Key(18)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
