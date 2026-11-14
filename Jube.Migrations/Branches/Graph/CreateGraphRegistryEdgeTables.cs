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

namespace Jube.Migrations.Branches.Graph
{
    using FluentMigrator;

    [Migration(20263108310000)]
    public class CreateGraphRegistryEdgeTables : Migration
    {
        public override void Up()
        {
            Create.Table("GraphRegistryEdge")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("GraphRegistryId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("KindId").AsByte().Nullable()
                .WithColumn("FromKey").AsString().Nullable()
                .WithColumn("ToKey").AsString().Nullable()
                .WithColumn("FromNodeKey").AsString().Nullable()
                .WithColumn("ToNodeKey").AsString().Nullable()
                .WithColumn("AmountKey").AsString().Nullable()
                .WithColumn("HalfLifeDays").AsInt32().Nullable()
                .WithColumn("KindPrior").AsDouble().Nullable()
                .WithColumn("HubThreshold").AsInt32().Nullable()
                .WithColumn("MinimumAmount").AsDouble().Nullable()
                .WithColumn("Hard").AsByte().Nullable()
                .WithColumn("Restricted").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("GraphRegistryEdge").ForeignColumn("GraphRegistryId")
                .ToTable("GraphRegistry").PrimaryColumn("Id");

            Create.Index().OnTable("GraphRegistryEdge")
                .OnColumn("GraphRegistryId").Ascending()
                .OnColumn("Deleted").Ascending();

            Create.Index().OnTable("GraphRegistryEdge")
                .OnColumn("Guid").Ascending();

            Create.Table("GraphRegistryEdgeVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("GraphRegistryEdgeId").AsInt32().Nullable()
                .WithColumn("GraphRegistryId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("KindId").AsByte().Nullable()
                .WithColumn("FromKey").AsString().Nullable()
                .WithColumn("ToKey").AsString().Nullable()
                .WithColumn("FromNodeKey").AsString().Nullable()
                .WithColumn("ToNodeKey").AsString().Nullable()
                .WithColumn("AmountKey").AsString().Nullable()
                .WithColumn("HalfLifeDays").AsInt32().Nullable()
                .WithColumn("KindPrior").AsDouble().Nullable()
                .WithColumn("HubThreshold").AsInt32().Nullable()
                .WithColumn("MinimumAmount").AsDouble().Nullable()
                .WithColumn("Hard").AsByte().Nullable()
                .WithColumn("Restricted").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("GraphRegistryEdgeVersion")
                .OnColumn("GraphRegistryEdgeId").Ascending();

            Execute.Sql("""
                        COMMENT ON TABLE public."GraphRegistryEdge" IS
                        'One kind of edge derived for a GraphRegistry: two report keys (ArchiveKey.Key names) that occur on the same archived transaction make an edge between their values. Everything else about the graph (nodes, weights, hubs, communities, features) is derived from these rows by the graph jobs.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."GraphRegistryId" IS 'The parent registry.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."Guid" IS 'Globally unique identifier; derived GraphEdge rows carry it as GraphRegistryEdgeGuid.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."Name" IS 'Edge type as shown in the legend and the edge filter, such as Shares device or Pays. Unique within the registry.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."KindId" IS 'Kind of edge, as GraphEdgeKind: 1 Identity link (undirected, a key shares an identifier), 2 Money flow (directed, from pays to), 3 Ownership (directed), 4 Resolution (same entity, written by the resolver and never configured).';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."FromKey" IS 'The ArchiveKey.Key whose value is the from node, usually the account or customer key.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."ToKey" IS 'The ArchiveKey.Key whose value is the to node, such as a device id, an IP or a beneficiary account.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."FromNodeKey" IS 'The node type the from value is stored under; null means FromKey. Lets a counterparty field such as ToAccountId land on the same node type as AccountId so chains connect.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."ToNodeKey" IS 'The node type the to value is stored under; null means ToKey.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."AmountKey" IS 'For money flow, the ArchiveKey.Key holding the amount; the field must itself be flagged Report Table on the model.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."HalfLifeDays" IS 'Half-life in days of the recency decay applied to the edge weight.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."KindPrior" IS 'Prior strength of this edge type in weights and propagation, 0 to 1.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."HubThreshold" IS 'Distinct from-nodes above which a to-node is a hub; null means the 99.9th percentile of the degree distribution, recomputed hourly.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."MinimumAmount" IS 'Money flow below this amount is not an edge.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."Hard" IS '1 when the link is hard evidence of the same controller (national id, card, bank account, phone) and collapses into one resolved entity; 0 for a soft link (device, IP, address) that stays a weighted edge.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."Restricted" IS '1 when the edge is visible only to roles granted on the registry.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."Active" IS '1 when edges of this kind are built and shown.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."Locked" IS '1 when the row is read-only.';
                        COMMENT ON TABLE public."GraphRegistryEdgeVersion" IS 'Version history of GraphRegistryEdge: a copy of the row as it was before each update.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
