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
using Jube.Dto.Options;
using Jube.Dto.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.VisualisationRegistryDatasource
{
    [FormEndpoint("VisualisationRegistryDatasource")]
    [FormRoles]
    [FormKeys(Id = nameof(Id), Parent = nameof(VisualisationRegistryId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [DatasourceEditor(Command = nameof(Command), DisplayKind = nameof(DisplayKindId), Grid = nameof(IncludeGrid),
        Template = nameof(TemplateText), Mapping = nameof(Mapping))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Query", Order = 20)]
    [FormGroup("Layout", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class VisualisationRegistryDatasourceDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the parent Visualisation Registry this Datasource belongs to. Set from the " +
                     "parent context; not user-editable.")]
        public int VisualisationRegistryId { get; set; }

        [Description("Internal correlation identifier used to relate this Datasource to its Role assignments. " +
                     "Not surfaced on the current page.")]
        public Guid Guid { get; set; }

        [Description("Display name of this Datasource. Unique within the parent Visualisation Registry " +
                     "(case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, this Datasource is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("When true, the rows returned by Command are also shown as a data grid alongside any " +
                     "Display visualisation.")]
        [FormField(Group = "Query", Order = 20, Widget = "switch")]
        [NewDefault(true)]
        public bool IncludeGrid { get; set; }

        [Description("When true, this Datasource participates in the parent Visualisation Registry's dashboard.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(true)]
        public bool Active { get; set; }

        [Description("Legacy free-text description field. Not surfaced on the current page and not populated " +
                     "by it -- see the migration report.")]
        public string? Description { get; set; }

        [Description("The SQL SELECT statement this Datasource runs against the reporting connection to produce " +
                     "its rows. May reference parent Visualisation Registry Parameters as '@Parameter_Name' " +
                     "tokens. Validated against the reporting database on Create/Update; a SELECT-only " +
                     "restriction applies when the server enforces it.")]
        [FormField(Group = "Query", Order = 10, Widget = "code")]
        public string? Command { get; set; }

        [Description("Ordering of this Datasource's tiles within the parent Visualisation Registry's dashboard; " +
                     "lower values render first.")]
        [FormField(Group = "Layout", Order = 10, Widget = "number")]
        [NewDefault(0)]
        public double Priority { get; set; }

        [Description("Legacy free-text column count field, distinct from the parent Visualisation Registry's " +
                     "own Columns setting. Not surfaced on the current page and not populated by it -- see the " +
                     "migration report.")]
        public int Columns { get; set; }

        [Description("Number of dashboard grid rows this Datasource's tile spans.")]
        [FormField(Group = "Layout", Order = 20, Widget = "number")]
        [NewDefault(0)]
        public int RowSpan { get; set; }

        [Description("Number of dashboard grid columns this Datasource's tile spans.")]
        [FormField(Group = "Layout", Order = 30, Widget = "number")]
        [NewDefault(0)]
        public int ColumnSpan { get; set; }

        [Description("Which chart this Datasource is drawn as, from DatasourceDisplayKind: 1=column, 2=bar, " +
                     "3=stacked column, 4=stacked bar, 5=percent stacked column, 6=percent stacked bar, 7=line, " +
                     "8=area, 9=stacked area, 10=percent stacked area, 11=pie, 12=donut, 13=scatter or bubble, " +
                     "14=histogram, 15=heatmap, 16=combo, 17=box plot, 18=candlestick, 19=waterfall, 20=funnel, " +
                     "21=radar, 22=polar, 23=gauge, 24=bullet, 25=sparkline, 26=treemap, 27=gantt, 28=map. 0 draws " +
                     "no chart, leaving the grid alone if IncludeGrid is on. A chart is drawn on the server from the " +
                     "columns of Command, each filling a chart role by position unless Mapping says otherwise. A " +
                     "Datasource must show a chart, a grid or both.")]
        [FormField(Group = "Query", Order = 30, Widget = "select")]
        [Options(typeof(DatasourceDisplayKind))]
        public int DisplayKindId { get; set; }

        [Description("The template the Datasource is drawn from when DisplayKindId is the Template kind (29): " +
                     "markup in a small Vue-style dialect, rendered on the server and escaped.")]
        [FormField(Group = "Query", Order = 40, Widget = "code")]
        public string? TemplateText { get; set; }

        [Description("Explicit assignments of result columns to chart roles, each with a role from " +
                     "DatasourceChartRole and the column names that fill it in order. A role left out takes its " +
                     "columns from the result by position. A column fills at most one role. Populated when a single " +
                     "Datasource is read, not in lists.")]
        [FormField(Group = "Query", Order = 50)]
        public List<DatasourceMappingEntryDto> Mapping { get; set; } = [];

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("Timestamp (UTC) this Datasource was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Datasource. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Datasource was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("User who created this Datasource. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Datasource, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Datasource was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}