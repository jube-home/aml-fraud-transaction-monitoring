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

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.EntityAnalysisModelDictionary
{
    [FormEndpoint("EntityAnalysisModelDictionary")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelGuid), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Lookup", Order = 20)]
    [FormGroup("Output", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelDictionaryDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Guid of the Model this Dictionary is registered against. Set from the parent Model " +
                     "context; not user-editable.")]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("Display name of this Dictionary. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Name of the data element on the parent Model supplying the key to pair against this " +
                     "Dictionary's Key Value Pairs during model invocation.")]
        [FormField(Group = "Lookup", Order = 10, Widget = "select")]
        [Lookup("/api/GetEntityAnalysisPotentialMultiPartStringNames", TextField = "value", ValueField = "value",
            ParentField = nameof(EntityAnalysisModelGuid))]
        [ListColumn(Order = 30, Title = "Data Name")]
        public string? DataName { get; set; }

        [Description("When true, the value looked up from this Dictionary is included in the response payload " +
                     "for a matching transaction.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("When true, this Dictionary is available for lookup during model invocation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, this Dictionary registration is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("Timestamp (UTC) this Dictionary was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Dictionary. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Dictionary was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("User who created this Dictionary. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Dictionary, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Dictionary was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}