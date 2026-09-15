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

namespace Jube.Dto.OpenTelemetryExclude
{
    [FormEndpoint("OpenTelemetryExclude")]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class OpenTelemetryExcludeDto : IUpdated, IActivatable, ILockable
    {
        [Description("The exact OpenTelemetry instrument/counter name to stop exporting, e.g. " +
                     "\"jube.engine.logs.warn.count\". Exact match only -- no wildcards. Unique. Applies to " +
                     "every instrument, including the ones OpenTelemetryLogCounter creates dynamically: an " +
                     "excluded name there is never even created, so it never starts incrementing. For every " +
                     "other, pre-existing named counter this only takes effect for an instrument not yet " +
                     "actively exporting -- one already exporting keeps doing so until the process restarts.")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, this exclusion is enforced. When false, it is ignored (the instrument is " +
                     "exported normally, subject to any other exclusion).")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, this row is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        public int Id { get; set; }

        [Description("User who created this row. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 10)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this row was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this row. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this row was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number. Read-only.")]
        [FormField(Group = "Audit", Order = 50)]
        public int Version { get; set; }

        [Description("User who removed this row, if soft-deleted. Server-assigned. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this row was removed, if applicable. Server-assigned. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}