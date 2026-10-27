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

using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Dto.Repository.VisualisationRegistryParameter;
using System.ComponentModel;
using Jube.Dto.Forms;
using Jube.Dto.Options;
using Jube.Dto.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.VisualisationRegistry
{
    [FormChild(typeof(VisualisationRegistryParameterDto), Label = "Parameters", Hub = "", Order = 10)]
    [FormChild(typeof(VisualisationRegistryDatasourceDto), Label = "Datasources", Hub = "", Order = 20)]
    [FormEndpoint("VisualisationRegistry")]
    [FormRoles]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Layout", Order = 20)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class VisualisationRegistryDto : IUpdated, IActivatable, ILockable, IGuidIdentified
    {
        [Description("Display name of the Visualisation Registry, shown as the link text in the Visualisation " +
                     "Administration list and, when it is shown in the Directory, in the Visualisation Directory. " +
                     "Unique per tenant (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Daily alerts overview")]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, the Visualisation Registry participates in recall: it can be embedded in a " +
                     "Case or listed in the Visualisation Directory, according to where it is shown.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this Visualisation Registry is locked to read-only and cannot be edited or " +
                     "deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Where the Visualisation Registry is shown: 1 = Directory, listed and recalled on its own, " +
                     "2 = Case, embedded as the dashboard of a Case. A Directory registry is seen by the roles it is " +
                     "granted to and recalled by those who subscribe to it; a Case registry is seen by whoever can see the " +
                     "Case. When it is shown in a Case, every " +
                     "datasource receives the built-in SQL parameter @Id, the id of the Case; the Directory has no " +
                     "@Id. Anything else the visualisation needs is traced from that id in its SQL.")]
        [FormField(Group = "Layout", Order = 15, Label = "Shown in", Widget = "select")]
        [Options(typeof(VisualisationContextKind))]
        [NewDefault(1)]
        public int ContextKindId { get; set; }

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