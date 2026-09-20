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
namespace Jube.Dto.Repository.VisualisationRegistry
{
    [FormEndpoint("VisualisationRegistry")]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Layout", Order = 20)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class VisualisationRegistryDto : IUpdated, IActivatable, ILockable, IGuidIdentified
    {
        [Description("Display name of the Visualisation Registry, shown as the link text in the Visualisation " +
                     "Administration list and, when Show In Directory is enabled, in the Visualisation Directory. " +
                     "Unique per tenant (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, the Visualisation Registry participates in recall: it can be embedded in a " +
                     "Case and, if Show In Directory is also true, listed in the Visualisation Directory.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this Visualisation Registry is locked to read-only and cannot be edited or " +
                     "deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("When true, the Visualisation Registry is listed on the Visualisation Directory page for " +
                     "direct recall. When false, it is reachable only embedded in a Case.")]
        [FormField(Group = "Layout", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ShowInDirectory { get; set; }

        [Description("The number of columns the recall canvas is divided into, for arranging the registry's " +
                     "Datasources.")]
        [FormField(Group = "Layout", Order = 20, Widget = "number")]
        [NewDefault(0)]
        public int Columns { get; set; }

        [Description("Width, in pixels, of each column on the recall canvas.")]
        [FormField(Group = "Layout", Order = 30, Widget = "number")]
        [NewDefault(0)]
        public int ColumnWidth { get; set; }

        [Description("Height, in pixels, of each row on the recall canvas. Rows are otherwise unbounded in " +
                     "number.")]
        [FormField(Group = "Layout", Order = 40, Widget = "number")]
        [NewDefault(0)]
        public int RowHeight { get; set; }

        [Description("Server-assigned globally-unique identifier of this Visualisation Registry, used to " +
                     "address it directly for embedded recall and role allocation. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing row.")]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this row. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this row. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this row, if any. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this row was soft-deleted, if any. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}