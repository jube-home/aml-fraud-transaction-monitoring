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

    [Migration(2026111792000003)]
    public class SeedRulesShadowAndTuningEvidence : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Rules: Shadow and Tuning Evidence' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Rules: Shadow and Tuning Evidence', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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
                            VALUES (v_registry_id, 'Rule Name', 1, 0, v_now, 'Administrator', 0, 1, 1, 'StructuringTuned', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Key', 1, 0, v_now, 'Administrator', 0, 1, 1, 'AmountEUR', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Threshold', 1, 0, v_now, 'Administrator', 0, 1, 3, '3900', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Band Percent', 1, 0, v_now, 'Administrator', 0, 1, 3, '10', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Shadow matches by day', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', a."CreatedDate"), 'YYYY-MM-DD') AS "Day", k.rule_name AS "Shadow rule", count(*) AS "Matches"
                        FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'shadowActivation', '{}'::jsonb)) AS k(rule_name)
                        WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t GROUP BY date_trunc('day', a."CreatedDate"), k.rule_name ORDER BY date_trunc('day', a."CreatedDate"), 2$cmd$, 10, 0, 6, 3, v_datasource_guid, 7, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Shadow rule', 1, 2, 3, 1),
                                (v_datasource_id, 'Matches', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Shadow rules against live rules', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT k.rule_name AS "Shadow rule", count(*) AS "Matches", count(*) FILTER (WHERE a."ActivationRuleCount" > 0) AS "Also alerted by a live rule", count(*) FILTER (WHERE a."ActivationRuleCount" = 0) AS "Net new",
                               round(100.0 * count(*) FILTER (WHERE a."ActivationRuleCount" > 0) / count(*), 1) AS "Overlap %"
                        FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'shadowActivation', '{}'::jsonb)) AS k(rule_name)
                        WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t GROUP BY 1 ORDER BY 2 DESC$cmd$, 20, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Shadow rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Matches', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Also alerted by a live rule', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Net new', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Overlap %', 3, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Shadow matches on entities later confirmed', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
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
                        sh AS (
                            SELECT k.rule_name, a."Json"->'payload'->>'AccountId' AS entity, a."CreatedDate"
                            FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'shadowActivation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        )
                        SELECT sh.rule_name AS "Shadow rule", count(*) AS "Matches", count(*) FILTER (WHERE EXISTS (SELECT 1 FROM cs WHERE cs."CaseKeyValue" = sh.entity AND cs.closed AND cs.confirmed)) AS "On entities with a confirmed case",
                               round(100.0 * count(*) FILTER (WHERE EXISTS (SELECT 1 FROM cs WHERE cs."CaseKeyValue" = sh.entity AND cs.closed AND cs.confirmed)) / count(*), 1) AS "Confirmed share %"
                        FROM sh GROUP BY sh.rule_name ORDER BY 2 DESC$cmd$, 30, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Shadow rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Matches', 2, 2, NULL, NULL),
                                (v_datasource_id, 'On entities with a confirmed case', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Confirmed share %', 3, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Backtest register', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT r."Name" AS "Rule", b."CreatedDate" AS "Run", b."CreatedUser" AS "Run by", b."Scanned" AS "Scanned", (b."Result"->>'Fired')::bigint AS "Fired",
                               (b."Result"->>'TruePositives')::bigint AS "True positives", (b."Result"->>'FalsePositives')::bigint AS "False positives", (b."Result"->>'FalseNegatives')::bigint AS "False negatives",
                               round(100.0 * (b."Result"->>'TruePositives')::numeric / NULLIF((b."Result"->>'TruePositives')::numeric + (b."Result"->>'FalsePositives')::numeric, 0), 1) AS "Precision %",
                               round(100.0 * (b."Result"->>'TruePositives')::numeric / NULLIF((b."Result"->>'TruePositives')::numeric + (b."Result"->>'FalseNegatives')::numeric, 0), 1) AS "Recall %",
                               round(extract(epoch FROM (b."CompletedDate" - b."StartedDate")))::integer AS "Seconds"
                        FROM "EntityAnalysisModelBacktestInstance" b LEFT JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = b."RuleId"
                        WHERE (b."Deleted" = 0 OR b."Deleted" IS NULL) AND b."Status" = 3 ORDER BY b."CreatedDate" DESC LIMIT 200$cmd$, 40, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Run', 4, 2, NULL, NULL),
                                (v_datasource_id, 'Run by', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Scanned', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Fired', 2, 5, NULL, NULL),
                                (v_datasource_id, 'True positives', 2, 6, NULL, NULL),
                                (v_datasource_id, 'False positives', 2, 7, NULL, NULL),
                                (v_datasource_id, 'False negatives', 2, 8, NULL, NULL),
                                (v_datasource_id, 'Precision %', 3, 9, NULL, NULL),
                                (v_datasource_id, 'Recall %', 3, 10, NULL, NULL),
                                (v_datasource_id, 'Seconds', 2, 11, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Backtest precision and recall for one rule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT to_char(b."CreatedDate", 'YYYY-MM-DD') AS "Run",
                               round(100.0 * (b."Result"->>'TruePositives')::numeric / NULLIF((b."Result"->>'TruePositives')::numeric + (b."Result"->>'FalsePositives')::numeric, 0), 1) AS "Precision %",
                               round(100.0 * (b."Result"->>'TruePositives')::numeric / NULLIF((b."Result"->>'TruePositives')::numeric + (b."Result"->>'FalseNegatives')::numeric, 0), 1) AS "Recall %"
                        FROM "EntityAnalysisModelBacktestInstance" b JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = b."RuleId" AND r."Name" = @Rule_Name
                        WHERE (b."Deleted" = 0 OR b."Deleted" IS NULL) AND b."Status" = 3 ORDER BY b."CreatedDate"$cmd$, 50, 0, 6, 3, v_datasource_guid, 7, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Run', 1, 1, 1, 1),
                                (v_datasource_id, 'Precision %', 3, 2, 2, 1),
                                (v_datasource_id, 'Recall %', 3, 3, 2, 2);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Above the line: alert outcomes by rule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
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
                        rl AS (SELECT r."Guid", r."Name", r."Shadow", r."Active", r."EnableCaseWorkflow" FROM "EntityAnalysisModelActivationRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL))
                        SELECT coalesce(rl."Name", 'Unknown') AS "Rule", CASE WHEN NOT cs.closed THEN 'Open' WHEN cs.confirmed THEN 'Confirmed' WHEN cs.fp THEN 'False positive' ELSE 'Other closed' END AS "Outcome", count(*) AS "Alerts"
                        FROM cs CROSS JOIN w LEFT JOIN rl ON rl."Guid" = cs.rule_guid WHERE cs."CreatedDate" >= w.f AND cs."CreatedDate" < w.t GROUP BY 1, 2 ORDER BY 1, 2$cmd$, 60, 0, 6, 3, v_datasource_guid, 5, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 1, 1),
                                (v_datasource_id, 'Outcome', 1, 2, 3, 1),
                                (v_datasource_id, 'Alerts', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Below the line: just under the threshold', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT k."KeyValueFloat" AS "Value"
                        FROM "ArchiveKey" k JOIN "Archive" a ON a."EntityAnalysisModelInstanceEntryGuid" = k."EntityAnalysisModelInstanceEntryGuid" CROSS JOIN w
                        WHERE k."ProcessingTypeId" = 1 AND k."Key" = @Key AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t AND a."ActivationRuleCount" = 0
                          AND k."KeyValueFloat" >= @Threshold * (1 - @Band_Percent / 100.0) AND k."KeyValueFloat" < @Threshold LIMIT 5000$cmd$, 70, 0, 6, 3, v_datasource_guid, 14, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Value', 3, 1, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Near misses by day', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', a."CreatedDate"), 'YYYY-MM-DD') AS "Day", count(*) AS "Near misses"
                        FROM "ArchiveKey" k JOIN "Archive" a ON a."EntityAnalysisModelInstanceEntryGuid" = k."EntityAnalysisModelInstanceEntryGuid" CROSS JOIN w
                        WHERE k."ProcessingTypeId" = 1 AND k."Key" = @Key AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t AND a."ActivationRuleCount" = 0
                          AND k."KeyValueFloat" >= @Threshold * (1 - @Band_Percent / 100.0) AND k."KeyValueFloat" < @Threshold
                        GROUP BY date_trunc('day', a."CreatedDate") ORDER BY date_trunc('day', a."CreatedDate")$cmd$, 80, 0, 6, 2, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Near misses', 2, 2, 2, 1);
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
