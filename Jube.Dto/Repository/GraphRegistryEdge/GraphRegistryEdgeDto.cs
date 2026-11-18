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
using MessagePack;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.GraphRegistryEdge
{
    [FormEndpoint("GraphRegistryEdge")]
    [FormKeys(Id = nameof(Id), Parent = nameof(GraphRegistryId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Keys", Order = 20)]
    [FormGroup("Icons", Order = 25)]
    [FormGroup("Weight", Order = 30, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    [MessagePackObject]
    public class GraphRegistryEdgeDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the parent Graph Registry this edge definition belongs to. Set from the parent " +
                     "context; not user-editable.")]
        [Key(0)]
        public int GraphRegistryId { get; set; }

        [Description("Server-assigned globally-unique identifier; derived edges carry it. Read-only.")]
        [Key(1)]
        public Guid Guid { get; set; }

        [Description("Edge type as shown in the legend and the edge filter, such as Shares device or Pays. Unique " +
                     "within the parent registry (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Shares device")]
        [ListColumn(Order = 10, Title = "Name")]
        [Key(2)]
        public string? Name { get; set; }

        [Description("When true, edges of this kind are built and shown.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        [NewDefault(true)]
        [Key(3)]
        public bool Active { get; set; }

        [Description("When true, this edge definition is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        [Key(4)]
        public bool Locked { get; set; }

        [Description("Kind of edge: 1 Identity link (undirected: the from key shares the to identifier, such as a " +
                     "device or IP), 2 Money flow (directed: from pays to, carries an amount), 3 Ownership (directed: " +
                     "from owns or directs to). Resolution edges are written by the entity resolver and are never " +
                     "configured.")]
        [FormField(Group = "Identity", Order = 40, Widget = "select")]
        [ListColumn(Order = 30, Title = "Kind")]
        [Options(typeof(GraphEdgeKind), GraphEdgeKind.IdentityLink, GraphEdgeKind.MoneyFlow, GraphEdgeKind.Ownership)]
        [NewDefault(1)]
        [Key(5)]
        public int KindId { get; set; }

        [Description("The report key (an Archive Key name, which is a request field flagged Archive Key on a " +
                     "model) whose value is the from node, usually the account or customer key.")]
        [FormField(Group = "Keys", Order = 10, Widget = "select")]
        [ListColumn(Order = 40, Title = "From")]
        [Lookup("/api/GraphRegistryEdge/Keys", TextField = "label", ValueField = "key")]
        [Key(6)]
        public string? FromKey { get; set; }

        [Description("The report key whose value is the to node, such as a device id, an IP or a beneficiary " +
                     "account.")]
        [FormField(Group = "Keys", Order = 20, Widget = "select")]
        [ListColumn(Order = 50, Title = "To")]
        [Lookup("/api/GraphRegistryEdge/Keys", TextField = "label", ValueField = "key")]
        [Key(7)]
        public string? ToKey { get; set; }

        [Description("The node type the from value is stored under; leave empty to use the from key itself. Use it " +
                     "when two report keys name the same kind of thing, so the nodes join.")]
        [FormField(Group = "Keys", Order = 12, Widget = "select")]
        [Lookup("/api/GraphRegistryEdge/Keys", TextField = "label", ValueField = "key")]
        [Key(8)]
        public string? FromNodeKey { get; set; }

        [Description("The node type the to value is stored under; leave empty to use the to key itself. For a " +
                     "payment, set it to the account key so that a beneficiary account and a paying account are one " +
                     "node and chains connect.")]
        [FormField(Group = "Keys", Order = 22, Widget = "select")]
        [Lookup("/api/GraphRegistryEdge/Keys", TextField = "label", ValueField = "key")]
        [Key(9)]
        public string? ToNodeKey { get; set; }

        [Description("Icon drawn on the from node and in the legend for its node type, chosen from the Lucide set. " +
                     "Leave empty to let the graph pick one from the key name. When two edge definitions give the " +
                     "same node type different icons, the first by identifier wins.")]
        [FormField(Group = "Icons", Order = 10, Widget = "select")]
        [Lookup("/api/GraphRegistryEdge/Icons", TextField = "label", ValueField = "name")]
        [Key(26)]
        public string? FromNodeIcon { get; set; }

        [Description("Icon drawn on the to node and in the legend for its node type, chosen from the Lucide set. " +
                     "Leave empty to let the graph pick one from the key name.")]
        [FormField(Group = "Icons", Order = 20, Widget = "select")]
        [Lookup("/api/GraphRegistryEdge/Icons", TextField = "label", ValueField = "name")]
        [Key(27)]
        public string? ToNodeIcon { get; set; }

        [Description("Icon shown on the link itself in the graph and in the legend, chosen from the Lucide set. " +
                     "Leave empty to show the link as a plain line.")]
        [FormField(Group = "Icons", Order = 30, Widget = "select")]
        [Lookup("/api/GraphRegistryEdge/Icons", TextField = "label", ValueField = "name")]
        [Key(25)]
        public string? IconName { get; set; }

        [Description("For money flow, the report key holding the amount of the transaction. The field must itself be " +
                     "flagged Archive Key on each model that should feed the graph.")]
        [FormField(Group = "Keys", Order = 30, Widget = "select")]
        [VisibleWhen(nameof(KindId), 2)]
        [RequiredWhen(nameof(KindId), 2)]
        [Lookup("/api/GraphRegistryEdge/Keys", TextField = "label", ValueField = "key")]
        [Key(10)]
        public string? AmountKey { get; set; }

        [Description("Money flow below this amount does not make an edge. Zero keeps everything.")]
        [FormField(Group = "Keys", Order = 40, Widget = "number")]
        [VisibleWhen(nameof(KindId), 2)]
        [NewDefault(0)]
        [Key(11)]
        public double MinimumAmount { get; set; }

        [Description("Half-life in days of the recency decay on the edge weight: an event this many days old " +
                     "counts half. 90 suits money flow, 180 suits identity links.")]
        [FormField(Group = "Weight", Order = 10, Widget = "number")]
        [NewDefault(90)]
        [Key(12)]
        public int HalfLifeDays { get; set; }

        [Description("Prior strength of this edge kind in weights and risk propagation, 0 to 1: 1 for a hard " +
                     "identifier such as a national id, about 0.7 for payments, 0.4 for a shared device, 0.1 for a " +
                     "shared IP.")]
        [FormField(Group = "Weight", Order = 20, Widget = "number")]
        [NewDefault(1.0)]
        [Key(13)]
        public double KindPrior { get; set; }

        [Description("Distinct from-nodes above which a to-node is a hub (a public IP, a payroll account) that " +
                     "traversal does not pass through unless asked. Leave empty for automatic detection at the 99.9th " +
                     "percentile of the degree distribution, recomputed hourly.")]
        [FormField(Group = "Weight", Order = 30, Widget = "number")]
        [Key(14)]
        public int? HubThreshold { get; set; }

        [Description("When true, the link is hard evidence of the same controller (national id, card, bank account, " +
                     "phone) and collapses the two nodes into one resolved entity. When false it is a soft link " +
                     "(device, IP, address) that stays a weighted edge.")]
        [FormField(Group = "Weight", Order = 40, Widget = "switch")]
        [NewDefault(false)]
        [Key(15)]
        public bool Hard { get; set; }

        [Description("When true, edges of this kind are shown only to roles granted on the registry.")]
        [FormField(Group = "Weight", Order = 50, Widget = "switch")]
        [NewDefault(false)]
        [Key(16)]
        public bool Restricted { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing row.")]
        [Key(17)]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [Key(18)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this row. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        [Key(19)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this row. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        [Key(20)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        [Key(21)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        [Key(22)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this row, if any. Read-only.")]
        [Key(23)]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this row was soft-deleted, if any. Read-only.")]
        [Key(24)]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
