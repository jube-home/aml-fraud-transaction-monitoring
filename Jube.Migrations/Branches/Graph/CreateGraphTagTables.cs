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

    [Migration(20263108390000)]
    public class CreateGraphTagTables : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE TABLE public."CaseTag" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "CaseId" integer NOT NULL,
                            "Name" text NOT NULL,
                            "Version" integer NULL,
                            "CreatedUser" text NULL,
                            "CreatedDate" timestamp without time zone NULL,
                            "Deleted" smallint NULL,
                            "DeletedUser" text NULL,
                            "DeletedDate" timestamp without time zone NULL
                        );
                        CREATE INDEX "IX_CaseTag_Case" ON public."CaseTag" ("CaseId", "Deleted");

                        CREATE TABLE public."GraphNodeTag" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "TenantRegistryId" integer NOT NULL,
                            "GraphNodeId" bigint NOT NULL,
                            "Tag" text NOT NULL,
                            "Distance" smallint NOT NULL,
                            "Within1" integer NOT NULL DEFAULT 0,
                            "Within2" integer NOT NULL DEFAULT 0,
                            "Within3" integer NOT NULL DEFAULT 0,
                            "ComputedDate" timestamp without time zone NOT NULL
                        );
                        CREATE UNIQUE INDEX "IX_GraphNodeTag_Node_Tag" ON public."GraphNodeTag" ("GraphNodeId", "Tag");
                        CREATE INDEX "IX_GraphNodeTag_Tenant_Tag" ON public."GraphNodeTag" ("TenantRegistryId", "Tag", "Distance");

                        COMMENT ON TABLE public."CaseTag" IS 'A tag applied to a case from the case manager, the case-level twin of ArchiveTag; the graph distance cycle seeds proximity-to-tag from both. Soft deleted, never removed.';
                        COMMENT ON COLUMN public."CaseTag"."Name" IS 'The tag name, from the model''s EntityAnalysisModelTag vocabulary.';
                        COMMENT ON TABLE public."GraphNodeTag" IS 'Per node and tag name, the graph distance in hops to the nearest transaction or case carrying that tag, with the counts of tagged nodes within one, two and three hops. Rewritten per tenant by every distance cycle; a node absent for a tag is beyond the distance cap.';
                        COMMENT ON COLUMN public."GraphNodeTag"."Distance" IS 'Hops from the node to the nearest node carrying the tag; 0 when the node itself carries it.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
