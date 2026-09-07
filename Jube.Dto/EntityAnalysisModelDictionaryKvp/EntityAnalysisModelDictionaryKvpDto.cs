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

namespace Jube.Dto.EntityAnalysisModelDictionaryKvp
{
    [FormEndpoint("EntityAnalysisModelDictionaryKvp")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelDictionaryId))]
    [FormGroup("Value", Order = 10)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelDictionaryKvpDto : IUpdated, ITreeChild
    {
        [Description("Id of the Dictionary this Key Value Pair belongs to. Set from the parent Dictionary " +
                     "context; not user-editable.")]
        public int EntityAnalysisModelDictionaryId { get; set; }

        [Description("The key matched against the value extracted from the payload's Lookup field during model " +
                     "invocation.")]
        [FormField(Group = "Value", Order = 10)]
        [ListColumn(Order = 10, Title = "Key")]
        public string? KvpKey { get; set; }

        [Description("The value returned when KvpKey matches the payload's Lookup field value during model " +
                     "invocation.")]
        [FormField(Group = "Value", Order = 20, Widget = "number")]
        [ListColumn(Order = 20, Title = "Value")]
        public double? KvpValue { get; set; }

        [Description("Timestamp (UTC) after which this Key Value Pair is automatically excluded from lookups " +
                     "and no longer visible, without an explicit delete. Leave unset for a pair that never " +
                     "expires. Must be in the future.")]
        [FormField(Group = "Value", Order = 30, Widget = "date")]
        [ListColumn(Order = 30, Title = "Delete Expiry Date")]
        public DateTimeOffset? DeleteExpiryDate { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this Key Value Pair. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Key Value Pair was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Key Value Pair. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Key Value Pair was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Key Value Pair, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Key Value Pair was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}