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

    [Migration(20263108360000)]
    public class CreateGraphExplorationTable : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE TABLE public."GraphExploration" (
                            "Id" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "TenantRegistryId" integer NOT NULL,
                            "User" text NOT NULL,
                            "Name" text NOT NULL,
                            "GraphRegistryGuid" uuid NOT NULL,
                            "Address" text NOT NULL,
                            "Pinned" jsonb NULL,
                            "CreatedDate" timestamp without time zone NOT NULL,
                            "UpdatedDate" timestamp without time zone NULL,
                            "Deleted" smallint NOT NULL DEFAULT 0,
                            "DeletedDate" timestamp without time zone NULL
                        );
                        CREATE INDEX "IX_GraphExploration_Tenant_User" ON public."GraphExploration" ("TenantRegistryId", "User", "Deleted");
                        COMMENT ON TABLE public."GraphExploration" IS 'A saved exploration on the Graph page, per user: the seed, window, layout, edge filter and expanded nodes as an address string, like CaseSearchFilter. Never versioned and never approved.';
                        COMMENT ON COLUMN public."GraphExploration"."User" IS 'The user who saved it; explorations are private.';
                        COMMENT ON COLUMN public."GraphExploration"."Address" IS 'The Graph page address (query string) that restores the view.';
                        COMMENT ON COLUMN public."GraphExploration"."Pinned" IS 'Optional pinned node positions as JSON, passed to the layout as anchors.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
