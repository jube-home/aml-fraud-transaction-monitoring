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

using FluentMigrator;

namespace Jube.Migrations.Branches.Graph
{
    [Migration(2026111710000000)]
    public class AddGraphRegistryEdgeIcons : Migration
    {
        public override void Up()
        {
            foreach (var table in new[] { "GraphRegistryEdge", "GraphRegistryEdgeVersion" })
            {
                Alter.Table(table)
                    .AddColumn("IconName").AsString().Nullable()
                    .AddColumn("FromNodeIcon").AsString().Nullable()
                    .AddColumn("ToNodeIcon").AsString().Nullable();
            }

            Execute.Sql("""
                        COMMENT ON COLUMN public."GraphRegistryEdge"."IconName" IS 'Lucide icon name drawn on the link in the graph and the legend; null draws a plain line.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."FromNodeIcon" IS 'Lucide icon name for the node type of the from node; null lets the graph choose one from the node key name.';
                        COMMENT ON COLUMN public."GraphRegistryEdge"."ToNodeIcon" IS 'Lucide icon name for the node type of the to node; null lets the graph choose one from the node key name.';
                        """);

            Execute.Sql("""
                        UPDATE "GraphRegistryEdge" e
                        SET "IconName" = v."IconName",
                            "FromNodeIcon" = v."FromNodeIcon",
                            "ToNodeIcon" = v."ToNodeIcon"
                        FROM (VALUES
                            ('Pays', 'banknote', 'landmark', 'landmark'),
                            ('Shares device', 'smartphone', 'landmark', 'smartphone'),
                            ('Shares network', 'wifi', 'landmark', 'router'),
                            ('Shares email', 'mail', 'landmark', 'mail'),
                            ('Shares card', 'credit-card', 'landmark', 'credit-card'),
                            ('In case', 'briefcase', NULL, 'briefcase')
                        ) AS v("Name", "IconName", "FromNodeIcon", "ToNodeIcon")
                        WHERE e."Name" = v."Name"
                          AND e."IconName" IS NULL
                          AND e."FromNodeIcon" IS NULL
                          AND e."ToNodeIcon" IS NULL
                          AND coalesce(e."Deleted", 0) = 0;
                        """);
        }

        public override void Down()
        {
        }
    }
}
