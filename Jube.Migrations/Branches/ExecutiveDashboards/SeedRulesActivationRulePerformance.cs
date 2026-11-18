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

    [Migration(2026111792000000)]
    public class SeedRulesActivationRulePerformance : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Rules: Activation Rule Performance' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Rules: Activation Rule Performance', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
                            RETURNING "Id" INTO v_registry_id;

                            INSERT INTO "VisualisationRegistryRole" ("Guid", "VisualisationRegistryGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_registry_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'From', 1, 0, v_now, 'Administrator', 0, 1, 4, '30', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'To', 1, 0, v_now, 'Administrator', 0, 1, 4, '0', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Value Field', 1, 0, v_now, 'Administrator', 0, 1, 1, 'AmountEUR', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Rule Name', 1, 0, v_now, 'Administrator', 0, 1, 1, '%', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Activations and hit rate by day', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        h AS (
                            SELECT r."Name" AS rule_name, h."CreatedDate", h."EvaluationCounter" AS ev, h."ActivationCounter" AS act
                            FROM "EntityAnalysisModelActivationRuleCounterHistory" h
                            JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = h."EntityAnalysisModelActivationRuleId"
                            CROSS JOIN w WHERE h."CreatedDate" >= w.f AND h."CreatedDate" < w.t
                        )
                        SELECT to_char(date_trunc('day', h."CreatedDate"), 'YYYY-MM-DD') AS "Day", sum(h.act) AS "Activations", round(100.0 * sum(h.act) / NULLIF(sum(h.ev), 0), 2) AS "Hit rate %"
                        FROM h GROUP BY date_trunc('day', h."CreatedDate") ORDER BY date_trunc('day', h."CreatedDate")$cmd$, 10, 0, 8, 3, v_datasource_guid, 16, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Activations', 3, 2, 2, 1),
                                (v_datasource_id, 'Hit rate %', 3, 3, 27, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Hit rate by rule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        h AS (
                            SELECT r."Name" AS rule_name, h."CreatedDate", h."EvaluationCounter" AS ev, h."ActivationCounter" AS act
                            FROM "EntityAnalysisModelActivationRuleCounterHistory" h
                            JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = h."EntityAnalysisModelActivationRuleId"
                            CROSS JOIN w WHERE h."CreatedDate" >= w.f AND h."CreatedDate" < w.t
                        )
                        SELECT h.rule_name AS "Rule", sum(h.ev) AS "Evaluations", sum(h.act) AS "Activations", round(100.0 * sum(h.act) / NULLIF(sum(h.ev), 0), 2) AS "Hit rate %"
                        FROM h GROUP BY h.rule_name HAVING sum(h.ev) > 0 ORDER BY 4 DESC$cmd$, 20, 1, 4, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 1, 1),
                                (v_datasource_id, 'Evaluations', 3, 2, NULL, NULL),
                                (v_datasource_id, 'Activations', 3, 3, NULL, NULL),
                                (v_datasource_id, 'Hit rate %', 3, 4, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Hit rate by rule and week', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        h AS (
                            SELECT r."Name" AS rule_name, h."CreatedDate", h."EvaluationCounter" AS ev, h."ActivationCounter" AS act
                            FROM "EntityAnalysisModelActivationRuleCounterHistory" h
                            JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = h."EntityAnalysisModelActivationRuleId"
                            CROSS JOIN w WHERE h."CreatedDate" >= w.f AND h."CreatedDate" < w.t
                        )
                        SELECT h.rule_name AS "Rule", to_char(date_trunc('week', h."CreatedDate"), 'IYYY-"W"IW') AS "Week", round(100.0 * sum(h.act) / NULLIF(sum(h.ev), 0), 2) AS "Hit rate %"
                        FROM h GROUP BY h.rule_name, date_trunc('week', h."CreatedDate") HAVING sum(h.ev) > 0 ORDER BY date_trunc('week', h."CreatedDate"), 1$cmd$, 30, 0, 6, 3, v_datasource_guid, 15, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 24, 1),
                                (v_datasource_id, 'Week', 1, 2, 25, 1),
                                (v_datasource_id, 'Hit rate %', 3, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Alerts by rule and day', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', a."CreatedDate"), 'YYYY-MM-DD') AS "Day", k.rule_name AS "Rule", count(*) AS "Alerts"
                        FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                        WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        GROUP BY date_trunc('day', a."CreatedDate"), k.rule_name ORDER BY date_trunc('day', a."CreatedDate"), 2$cmd$, 40, 0, 6, 3, v_datasource_guid, 9, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Rule', 1, 2, 3, 1),
                                (v_datasource_id, 'Alerts', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Prevailing rule against all firings', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT k.rule_name AS "Rule", CASE WHEN a."Json"->>'prevailingEntityAnalysisModelActivationRuleName' = k.rule_name THEN 'Prevailing' ELSE 'Also fired' END AS "Role in the alert", count(*) AS "Alerts"
                        FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                        WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        GROUP BY 1, 2 ORDER BY 1, 2$cmd$, 50, 0, 6, 3, v_datasource_guid, 4, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 1, 1),
                                (v_datasource_id, 'Role in the alert', 1, 2, 3, 1),
                                (v_datasource_id, 'Alerts', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Response elevation by rule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT k.rule_name AS "Rule", round(avg(a."ResponseElevation")::numeric, 2) AS "Mean elevation", round(max(a."ResponseElevation")::numeric, 2) AS "Maximum elevation", count(*) AS "Alerts"
                        FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                        WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        GROUP BY 1 ORDER BY 2 DESC$cmd$, 60, 1, 6, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 1, 1),
                                (v_datasource_id, 'Mean elevation', 3, 2, 2, 1),
                                (v_datasource_id, 'Maximum elevation', 3, 3, 2, 2),
                                (v_datasource_id, 'Alerts', 2, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Value alerted by rule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT k.rule_name AS "Rule", round(sum(CASE WHEN a."Json"->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (a."Json"->'payload'->>@Value_Field)::numeric END), 0) AS "Value alerted"
                        FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                        WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        GROUP BY 1 HAVING sum(CASE WHEN a."Json"->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (a."Json"->'payload'->>@Value_Field)::numeric END) > 0 ORDER BY 2 DESC$cmd$, 70, 0, 6, 3, v_datasource_guid, 26, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 1, 1),
                                (v_datasource_id, 'Value alerted', 3, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Alerts by weekday and hour', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(a."CreatedDate", 'ID') || ' ' || to_char(a."CreatedDate", 'Dy') AS "Weekday", to_char(a."CreatedDate", 'HH24') AS "Hour", count(*) AS "Alerts"
                        FROM "Archive" a CROSS JOIN w WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        GROUP BY 1, 2 ORDER BY 1, 2$cmd$, 80, 0, 6, 3, v_datasource_guid, 15, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Weekday', 1, 1, 24, 1),
                                (v_datasource_id, 'Hour', 1, 2, 25, 1),
                                (v_datasource_id, 'Alerts', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Entities with the most alerts', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT a."Json"->'payload'->>'AccountId' AS "Entity", k.rule_name AS "Rule", count(*) AS "Alerts", max(a."CreatedDate") AS "Latest"
                        FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                        WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t AND k.rule_name LIKE @Rule_Name
                        GROUP BY 1, 2 ORDER BY 3 DESC, 1 LIMIT 50$cmd$, 90, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Entity', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Rule', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Alerts', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Latest', 4, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rules that did not fire', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        fired AS (
                            SELECT DISTINCT k.rule_name FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        )
                        SELECT r."Name" AS "Rule", r."Version" AS "Version", r."CreatedUser" AS "Made by", r."CreatedDate" AS "Created", CASE WHEN r."EnableCaseWorkflow" = 1 THEN 'Opens cases' ELSE 'Does not open cases' END AS "Configured"
                        FROM "EntityAnalysisModelActivationRule" r
                        WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL) AND r."Active" = 1 AND coalesce(r."Shadow", 0) = 0 AND NOT EXISTS (SELECT 1 FROM fired f WHERE f.rule_name = r."Name") ORDER BY r."Name"$cmd$, 100, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Made by', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Created', 4, 4, NULL, NULL),
                                (v_datasource_id, 'Configured', 1, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rule league table', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        cs AS (
                            SELECT c."Id", c."CreatedDate", c."ClosedDate", (c."ClosedStatusId" = 3) AS closed, c."ClosedReasonId", c."ClosedUser", c."CaseKeyValue", c."LockedUser",
                                   c."EntityAnalysisModelActivationRuleGuid" AS rule_guid, c."EntityAnalysisModelInstanceEntryGuid" AS entry_guid, c."CaseWorkflowStatusGuid" AS status_guid,
                                   wf."ResolutionTargetMinutes" AS resolution_target, wf."FirstViewTargetMinutes" AS first_view_target, wf."OwnershipTargetMinutes" AS ownership_target,
                                   (c."ClosedReasonId" IN (5, 6) OR st."OutcomeId" = 1) AS confirmed,
                                   (c."ClosedReasonId" IN (1, 2) OR st."OutcomeId" = 2) AS fp,
                                   st."Name" AS status_name
                            FROM "Case" c
                            LEFT JOIN "CaseWorkflow" wf ON wf."Guid" = c."CaseWorkflowGuid" AND (wf."Deleted" = 0 OR wf."Deleted" IS NULL)
                            LEFT JOIN "CaseWorkflowStatus" st ON st."Guid" = c."CaseWorkflowStatusGuid" AND (st."Deleted" = 0 OR st."Deleted" IS NULL)
                        ),
                        ar AS (
                            SELECT k.rule_name, a."EntityAnalysisModelInstanceEntryGuid" AS g, a."CreatedDate"
                            FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        ),
                        tx AS (SELECT count(*) AS n FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t)
                        SELECT ar.rule_name AS "Rule", count(*) AS "Alerts", round(100.0 * count(*) / NULLIF(tx.n, 0), 2) AS "Hit rate %", count(cs."Id") AS "Cases", count(*) FILTER (WHERE cs.closed) AS "Closed",
                               count(*) FILTER (WHERE cs.closed AND cs.confirmed) AS "Confirmed", round(100.0 * count(*) FILTER (WHERE cs.closed AND cs.confirmed) / NULLIF(count(*) FILTER (WHERE cs.closed), 0), 1) AS "Precision %",
                               round(100.0 * count(*) FILTER (WHERE cs.closed AND cs.fp) / NULLIF(count(*) FILTER (WHERE cs.closed), 0), 1) AS "False-positive %"
                        FROM ar CROSS JOIN tx LEFT JOIN cs ON cs.entry_guid = ar.g GROUP BY ar.rule_name, tx.n ORDER BY 2 DESC$cmd$, 110, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Alerts', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Hit rate %', 3, 3, NULL, NULL),
                                (v_datasource_id, 'Cases', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Closed', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Confirmed', 2, 6, NULL, NULL),
                                (v_datasource_id, 'Precision %', 3, 7, NULL, NULL),
                                (v_datasource_id, 'False-positive %', 3, 8, NULL, NULL);
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
