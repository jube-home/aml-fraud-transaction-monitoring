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

    [Migration(2026111792000002)]
    public class SeedRulesOverlapAndRedundancy : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Rules: Overlap and Redundancy' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Rules: Overlap and Redundancy', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
                            RETURNING "Id" INTO v_registry_id;

                            INSERT INTO "VisualisationRegistryRole" ("Guid", "VisualisationRegistryGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_registry_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'From', 1, 0, v_now, 'Administrator', 0, 1, 4, '14', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'To', 1, 0, v_now, 'Administrator', 0, 1, 4, '0', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rules that fire together', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        fired AS (
                            SELECT a."Id", k.rule_name FROM "Archive" a CROSS JOIN w
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        )
                        SELECT f1.rule_name AS "Rule", f2.rule_name AS "Also fired", count(*) AS "Alerts" FROM fired f1 JOIN fired f2 ON f1."Id" = f2."Id" AND f1.rule_name <> f2.rule_name GROUP BY 1, 2 ORDER BY 1, 2$cmd$, 10, 0, 6, 3, v_datasource_guid, 15, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 24, 1),
                                (v_datasource_id, 'Also fired', 1, 2, 25, 1),
                                (v_datasource_id, 'Alerts', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Alerts only this rule raised', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        fired AS (
                            SELECT a."Id", k.rule_name FROM "Archive" a CROSS JOIN w
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        ),
                        n AS (SELECT "Id", count(*) AS c FROM fired GROUP BY "Id")
                        SELECT f.rule_name AS "Rule", count(*) AS "Alerts", count(*) FILTER (WHERE n.c = 1) AS "Unique alerts", round(100.0 * count(*) FILTER (WHERE n.c = 1) / count(*), 1) AS "Unique %"
                        FROM fired f JOIN n ON n."Id" = f."Id" GROUP BY f.rule_name ORDER BY 3 DESC$cmd$, 20, 1, 6, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 1, 1),
                                (v_datasource_id, 'Alerts', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Unique alerts', 2, 3, 2, 1),
                                (v_datasource_id, 'Unique %', 3, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Share of each rule''s alerts also caught by another rule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        fired AS (
                            SELECT a."Id", k.rule_name FROM "Archive" a CROSS JOIN w
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        ),
                        n AS (SELECT "Id", count(*) AS c FROM fired GROUP BY "Id")
                        SELECT f.rule_name AS "Rule", count(*) AS "Alerts", count(*) FILTER (WHERE n.c > 1) AS "Also caught by another", round(100.0 * count(*) FILTER (WHERE n.c > 1) / count(*), 1) AS "Overlap %"
                        FROM fired f JOIN n ON n."Id" = f."Id" GROUP BY f.rule_name ORDER BY 4 DESC$cmd$, 30, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Alerts', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Also caught by another', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Overlap %', 3, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rule pairs ranked by overlap', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        fired AS (
                            SELECT a."Id", k.rule_name FROM "Archive" a CROSS JOIN w
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        ),
                        t AS (SELECT rule_name, count(*) AS n FROM fired GROUP BY rule_name),
                        pr AS (SELECT f1.rule_name AS a, f2.rule_name AS b, count(*) AS both_n FROM fired f1 JOIN fired f2 ON f1."Id" = f2."Id" AND f1.rule_name < f2.rule_name GROUP BY 1, 2)
                        SELECT pr.a AS "Rule", pr.b AS "Other rule", pr.both_n AS "Both fired", round(100.0 * pr.both_n / (ta.n + tb.n - pr.both_n), 1) AS "Overlap of the two %",
                               CASE WHEN 100.0 * pr.both_n / (ta.n + tb.n - pr.both_n) > 90 THEN 'Candidate to retire' ELSE 'Keep' END AS "Finding"
                        FROM pr JOIN t ta ON ta.rule_name = pr.a JOIN t tb ON tb.rule_name = pr.b ORDER BY 4 DESC LIMIT 25$cmd$, 40, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Other rule', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Both fired', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Overlap of the two %', 3, 4, NULL, NULL),
                                (v_datasource_id, 'Finding', 1, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Alerts by number of rules fired', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        fired AS (
                            SELECT a."Id", k.rule_name FROM "Archive" a CROSS JOIN w
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        ),
                        n AS (SELECT "Id", count(*) AS c FROM fired GROUP BY "Id")
                        SELECT n.c || CASE WHEN n.c = 1 THEN ' rule' ELSE ' rules' END AS "Rules fired", count(*) AS "Alerts" FROM n GROUP BY n.c ORDER BY n.c$cmd$, 50, 0, 6, 2, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rules fired', 1, 1, 1, 1),
                                (v_datasource_id, 'Alerts', 2, 2, 2, 1);
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
