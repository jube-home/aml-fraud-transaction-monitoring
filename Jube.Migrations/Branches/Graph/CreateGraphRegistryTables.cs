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

    [Migration(20263108300000)]
    public class CreateGraphRegistryTables : Migration
    {
        public override void Up()
        {
            Create.Table("GraphRegistry")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("ContextKindId").AsByte().Nullable()
                .WithColumn("DefaultLayoutId").AsByte().Nullable()
                .WithColumn("DefaultHops").AsByte().Nullable()
                .WithColumn("DefaultWindowDays").AsInt32().Nullable()
                .WithColumn("RenderCap").AsInt32().Nullable()
                .WithColumn("HubPolicyId").AsByte().Nullable()
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

            Create.Index().OnTable("GraphRegistry")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("Deleted").Ascending();

            Create.Index().OnTable("GraphRegistry")
                .OnColumn("Guid").Ascending();

            Create.Table("GraphRegistryVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("GraphRegistryId").AsInt32().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("ContextKindId").AsByte().Nullable()
                .WithColumn("DefaultLayoutId").AsByte().Nullable()
                .WithColumn("DefaultHops").AsByte().Nullable()
                .WithColumn("DefaultWindowDays").AsInt32().Nullable()
                .WithColumn("RenderCap").AsInt32().Nullable()
                .WithColumn("HubPolicyId").AsByte().Nullable()
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

            Create.Index().OnTable("GraphRegistryVersion")
                .OnColumn("GraphRegistryId").Ascending();

            Create.Table("GraphRegistryRole")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("GraphRegistryGuid").AsGuid().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("RoleRegistryGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("GraphRegistryRole")
                .OnColumn("GraphRegistryGuid").Ascending()
                .OnColumn("Deleted").Ascending();

            Execute.Sql("""
                        COMMENT ON TABLE public."GraphRegistry" IS
                        'A named graph configuration of a tenant: which kinds of edge are derived from the archived report keys, and how the graph is shown by default. It is a standalone tool, listed in the Graph directory, and a Case Workflow may reference one so that the case page shows the network around the case key. Mirrors VisualisationRegistry.';
                        COMMENT ON COLUMN public."GraphRegistry"."Id" IS 'Server-assigned identity of the graph registry.';
                        COMMENT ON COLUMN public."GraphRegistry"."Guid" IS 'Globally unique identifier used by Case Workflows and role grants to address the registry.';
                        COMMENT ON COLUMN public."GraphRegistry"."TenantRegistryId" IS 'The tenant the registry belongs to.';
                        COMMENT ON COLUMN public."GraphRegistry"."Name" IS 'Display name, unique per tenant (case-insensitive).';
                        COMMENT ON COLUMN public."GraphRegistry"."Active" IS '1 when the registry is in use: its edges are built and it can be shown.';
                        COMMENT ON COLUMN public."GraphRegistry"."Locked" IS '1 when the registry is read-only.';
                        COMMENT ON COLUMN public."GraphRegistry"."ContextKindId" IS 'Where the registry is shown, as VisualisationContextKind: 1 Directory, 2 Case.';
                        COMMENT ON COLUMN public."GraphRegistry"."DefaultLayoutId" IS 'Layout the canvas opens with, as GraphLayout: 1 Force, 2 Radial, 3 Flow, 4 Hierarchy.';
                        COMMENT ON COLUMN public."GraphRegistry"."DefaultHops" IS 'How many hops the neighbourhood opens with, 1 to 3.';
                        COMMENT ON COLUMN public."GraphRegistry"."DefaultWindowDays" IS 'The time window in days the canvas opens with.';
                        COMMENT ON COLUMN public."GraphRegistry"."RenderCap" IS 'The most nodes drawn before clusters collapse into one node; at most 150.';
                        COMMENT ON COLUMN public."GraphRegistry"."HubPolicyId" IS 'How hubs are treated on the canvas, as GraphHubPolicy: 1 Hidden, 2 Collapsed.';
                        COMMENT ON TABLE public."GraphRegistryVersion" IS 'Version history of GraphRegistry: a copy of the row as it was before each update.';
                        COMMENT ON TABLE public."GraphRegistryRole" IS 'Role grants on a GraphRegistry: which roles may see it in the directory, on a case, and may see its restricted edges.';
                        COMMENT ON COLUMN public."GraphRegistryRole"."GraphRegistryGuid" IS 'The registry granted, matching Guid on GraphRegistry.';
                        COMMENT ON COLUMN public."GraphRegistryRole"."RoleRegistryGuid" IS 'The role granted, matching Guid on RoleRegistry.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
