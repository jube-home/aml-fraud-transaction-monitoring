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
using Jube.Dto.Options;
using Jube.Dto.Repository.GraphRegistryEdge;
using MessagePack;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.GraphRegistry
{
    [FormChild(typeof(GraphRegistryEdgeDto), Label = "Edges", Hub = "", Order = 10)]
    [FormEndpoint("GraphRegistry")]
    [FormRoles]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Defaults", Order = 20)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class GraphRegistryDto : IUpdated, IActivatable, ILockable, IGuidIdentified
    {
        [Description("Display name of the Graph Registry, shown in the Graph directory and as the link text in " +
                     "Graph administration. Unique per tenant (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Payments ring")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(0)]
        public string? Name { get; set; }

        [Description("When true, the registry's edges are built by the graph jobs and the graph can be shown in " +
                     "the directory or on a case.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [NewDefault(false)]
        [Key(1)]
        public bool Active { get; set; }

        [Description("When true, this Graph Registry is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        [Key(2)]
        public bool Locked { get; set; }

        [Description("Where the graph is shown: 1 = Directory, listed on the Graph page for the roles granted to " +
                     "it, 2 = Case, shown as the Network section of cases whose workflow references it. The edges are " +
                     "built either way.")]
        [FormField(Group = "Identity", Order = 40, Label = "Shown in", Widget = "select")]
        [Options(typeof(VisualisationContextKind))]
        [NewDefault(1)]
        [Key(3)]
        public int ContextKindId { get; set; }

        [Description("The layout the canvas opens with: 1 Force for clusters, 2 Radial for a neighbourhood by hop, " +
                     "3 Flow for money tracing, 4 Hierarchy for ownership.")]
        [FormField(Group = "Defaults", Order = 10, Widget = "select")]
        [Options(typeof(GraphLayout))]
        [NewDefault(1)]
        [Key(4)]
        public int DefaultLayoutId { get; set; }

        [Description("How many hops from the seed the neighbourhood opens with, 1 to 3.")]
        [FormField(Group = "Defaults", Order = 20, Widget = "number")]
        [NewDefault(2)]
        [Key(5)]
        public int DefaultHops { get; set; }

        [Description("The time window in days the canvas opens with.")]
        [FormField(Group = "Defaults", Order = 30, Widget = "number")]
        [NewDefault(30)]
        [Key(6)]
        public int DefaultWindowDays { get; set; }

        [Description("The most nodes drawn on the canvas before clusters collapse into one node each; at most 150.")]
        [FormField(Group = "Defaults", Order = 40, Widget = "number")]
        [NewDefault(150)]
        [Key(7)]
        public int RenderCap { get; set; }

        [Description("How hubs (nodes above their edge's hub threshold, such as a public IP) are treated: 1 Hidden " +
                     "with a count, 2 Collapsed into a single node that can be expanded.")]
        [FormField(Group = "Defaults", Order = 50, Widget = "select")]
        [Options(typeof(GraphHubPolicy))]
        [NewDefault(1)]
        [Key(8)]
        public int HubPolicyId { get; set; }

        [Description("Server-assigned globally-unique identifier of this Graph Registry, used by Case Workflows " +
                     "and role grants to address it. Read-only.")]
        [Key(9)]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing row.")]
        [Key(10)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(11)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this row. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(12)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this row. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(13)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(14)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(15)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this row, if any. Read-only.")]
        [Key(16)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this row was soft-deleted, if any. Read-only.")]
        [Key(17)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
