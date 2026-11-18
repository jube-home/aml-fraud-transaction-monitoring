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

namespace Jube.Migrations.Branches.ExecutiveDashboards
{
    using FluentMigrator;

    [Migration(2026111792000014)]
    public class SeedNetworkGraph : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_tenant integer;
                            v_role uuid;
                            v_registry_id integer;
                            v_registry_guid uuid := gen_random_uuid();
                            v_datasource_id integer;
                            v_datasource_guid uuid;
                            v_parameter_guid uuid;
                            v_now timestamp := now() at time zone 'utc';
                        BEGIN
                            SELECT "Id" INTO v_tenant FROM "TenantRegistry" WHERE "Id" = 1 AND coalesce("Deleted", 0) = 0;
                            SELECT "Guid" INTO v_role FROM "RoleRegistry" WHERE "Id" = 1 AND coalesce("Deleted", 0) = 0;

                            IF v_tenant IS NULL OR v_role IS NULL THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Network: Graph' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Network: Graph', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
                            RETURNING "Id" INTO v_registry_id;

                            INSERT INTO "VisualisationRegistryRole" ("Guid", "VisualisationRegistryGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_registry_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'From', 1, 0, v_now, 'Administrator', 0, 1, 4, '90', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'To', 1, 0, v_now, 'Administrator', 0, 1, 4, '0', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Communities by size and confirmed rate', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT c."Size" AS "Size", round((c."ConfirmedRate" * 100)::numeric, 1) AS "Confirmed rate %", c."ConfirmedCount" AS "Confirmed" FROM "GraphCommunity" c WHERE (c."Deleted" = 0 OR c."Deleted" IS NULL) AND c."ConfirmedRate" IS NOT NULL LIMIT 2000$cmd$, 10, 0, 6, 3, v_datasource_guid, 13, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Size', 2, 1, 4, 1),
                                (v_datasource_id, 'Confirmed rate %', 3, 2, 5, 1),
                                (v_datasource_id, 'Confirmed', 2, 3, 6, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Community league table', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT c."Label" AS "Community", c."Size" AS "Members", round(c."Density"::numeric, 3) AS "Density", c."ClosedCount" AS "Closed cases", c."ConfirmedCount" AS "Confirmed", round((c."ConfirmedRate" * 100)::numeric, 1) AS "Confirmed rate %",
                               round(c."TenantRateRatio"::numeric, 2) AS "Against the whole tenant", c."ComputedDate" AS "Computed" FROM "GraphCommunity" c WHERE (c."Deleted" = 0 OR c."Deleted" IS NULL) ORDER BY c."ConfirmedRate" DESC NULLS LAST LIMIT 200$cmd$, 20, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Community', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Members', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Density', 3, 3, NULL, NULL),
                                (v_datasource_id, 'Closed cases', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Confirmed', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Confirmed rate %', 3, 6, NULL, NULL),
                                (v_datasource_id, 'Against the whole tenant', 3, 7, NULL, NULL),
                                (v_datasource_id, 'Computed', 4, 8, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Patterns by kind and state', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT CASE p."KindId" WHEN 1 THEN 'Fan-out' WHEN 2 THEN 'Fan-in' WHEN 3 THEN 'Gather-scatter' WHEN 4 THEN 'Scatter-gather' WHEN 5 THEN 'Temporal cycle' WHEN 6 THEN 'Pass-through' WHEN 7 THEN 'Smurfing one' WHEN 8 THEN 'Smurfing two' WHEN 9 THEN 'Structuring window' ELSE 'Dense block' END AS "Pattern",
                               CASE p."StateId" WHEN 1 THEN 'New' WHEN 2 THEN 'Seen' WHEN 3 THEN 'Dismissed' ELSE 'Cased' END AS "State", count(*) AS "Patterns" FROM "GraphPattern" p GROUP BY 1, 2 ORDER BY 1, 2$cmd$, 30, 0, 6, 3, v_datasource_guid, 4, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Pattern', 1, 1, 1, 1),
                                (v_datasource_id, 'State', 1, 2, 3, 1),
                                (v_datasource_id, 'Patterns', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Patterns detected by week', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT to_char(date_trunc('week', p."FirstDetectedDate"), 'IYYY-"W"IW') AS "Week", count(*) AS "Patterns" FROM "GraphPattern" p WHERE p."FirstDetectedDate" >= (now() at time zone 'utc') - interval '84 days' GROUP BY date_trunc('week', p."FirstDetectedDate") ORDER BY date_trunc('week', p."FirstDetectedDate")$cmd$, 40, 0, 6, 3, v_datasource_guid, 7, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Week', 1, 1, 1, 1),
                                (v_datasource_id, 'Patterns', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Pattern register', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT p."FirstDetectedDate" AS "Detected", CASE p."KindId" WHEN 1 THEN 'Fan-out' WHEN 2 THEN 'Fan-in' WHEN 3 THEN 'Gather-scatter' WHEN 4 THEN 'Scatter-gather' WHEN 5 THEN 'Temporal cycle' WHEN 6 THEN 'Pass-through' WHEN 7 THEN 'Smurfing one' WHEN 8 THEN 'Smurfing two' WHEN 9 THEN 'Structuring window' ELSE 'Dense block' END AS "Pattern",
                               round(p."Score"::numeric, 3) AS "Score", p."MemberCount" AS "Members", CASE p."StateId" WHEN 1 THEN 'New' WHEN 2 THEN 'Seen' WHEN 3 THEN 'Dismissed' ELSE 'Cased' END AS "State", p."CaseId" AS "Case", p."DecidedUser" AS "Decided by"
                        FROM "GraphPattern" p ORDER BY p."FirstDetectedDate" DESC LIMIT 500$cmd$, 50, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Detected', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Pattern', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Score', 3, 3, NULL, NULL),
                                (v_datasource_id, 'Members', 2, 4, NULL, NULL),
                                (v_datasource_id, 'State', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Case', 2, 6, NULL, NULL),
                                (v_datasource_id, 'Decided by', 1, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Most connected nodes by PageRank', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT n."Key" AS "Kind of identifier", n."Value" AS "Identifier", n."Degree" AS "Connections", round(n."PageRank"::numeric, 6) AS "PageRank", n."ConfirmedWithin1" AS "Confirmed within 1 hop", n."ConfirmedWithin2" AS "Within 2", n."ConfirmedWithin3" AS "Within 3", CASE WHEN n."Hub" = 1 THEN 'Yes' ELSE 'No' END AS "Hub"
                        FROM "GraphNode" n ORDER BY n."PageRank" DESC NULLS LAST LIMIT 100$cmd$, 60, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Kind of identifier', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Identifier', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Connections', 2, 3, NULL, NULL),
                                (v_datasource_id, 'PageRank', 3, 4, NULL, NULL),
                                (v_datasource_id, 'Confirmed within 1 hop', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Within 2', 2, 6, NULL, NULL),
                                (v_datasource_id, 'Within 3', 2, 7, NULL, NULL),
                                (v_datasource_id, 'Hub', 1, 8, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Nodes near confirmed cases', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT v.label AS "Distance", v.n AS "Nodes" FROM (SELECT count(*) FILTER (WHERE n."ConfirmedWithin1" > 0) AS d1, count(*) FILTER (WHERE n."ConfirmedWithin2" > 0) AS d2, count(*) FILTER (WHERE n."ConfirmedWithin3" > 0) AS d3 FROM "GraphNode" n) x
                        CROSS JOIN LATERAL (VALUES ('Within 1 hop', x.d1), ('Within 2 hops', x.d2), ('Within 3 hops', x.d3)) AS v(label, n)$cmd$, 70, 0, 6, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Distance', 1, 1, 1, 1),
                                (v_datasource_id, 'Nodes', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Edges by kind', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT CASE e."KindId" WHEN 1 THEN 'Identity' WHEN 2 THEN 'Money flow' WHEN 3 THEN 'Ownership' WHEN 4 THEN 'Resolution' ELSE 'Case' END AS "Kind", count(*) AS "Edges", round(sum(e."AmountSum")::numeric, 0) AS "Amount" FROM "GraphEdge" e GROUP BY 1 ORDER BY 2 DESC$cmd$, 80, 1, 6, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Kind', 1, 1, 1, 1),
                                (v_datasource_id, 'Edges', 2, 2, 2, 1),
                                (v_datasource_id, 'Amount', 3, 3, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Component sizes', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT n."ComponentSize" AS "Component size" FROM "GraphNode" n WHERE n."ComponentSize" IS NOT NULL LIMIT 5000$cmd$, 90, 0, 6, 3, v_datasource_guid, 14, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Component size', 2, 1, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);
                        END
                        $seed$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
