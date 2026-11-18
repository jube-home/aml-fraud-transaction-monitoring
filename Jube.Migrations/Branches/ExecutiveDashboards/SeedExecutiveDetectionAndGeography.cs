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

    [Migration(2026111791000002)]
    public class SeedExecutiveDetectionAndGeography : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Executive: Detection Effectiveness and Geography' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Executive: Detection Effectiveness and Geography', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Value Field', 1, 0, v_now, 'Administrator', 0, 1, 1, 'AmountEUR', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Country Field', 1, 0, v_now, 'Administrator', 0, 1, 1, 'BillingCountry', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Detection summary', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        ar AS (
                            SELECT k.rule_name, a."EntityAnalysisModelInstanceEntryGuid" AS g, a."CreatedDate", a."ResponseElevation"
                            FROM "Archive" a CROSS JOIN w
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        ),
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
                        rl AS (SELECT r."Guid", r."Name", r."Shadow", r."Active", r."EnableCaseWorkflow" FROM "EntityAnalysisModelActivationRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)),
                        per AS (
                            SELECT ar.rule_name, count(*) AS alerts, count(cs."Id") AS cases, count(*) FILTER (WHERE cs.closed) AS closed,
                                   count(*) FILTER (WHERE cs.closed AND cs.confirmed) AS confirmed, count(*) FILTER (WHERE cs.closed AND cs.fp) AS fp
                            FROM ar LEFT JOIN cs ON cs.entry_guid = ar.g GROUP BY ar.rule_name
                        ),
                        al AS (SELECT count(*) AS alerts FROM "Archive" a CROSS JOIN w WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t),
                        rv AS (SELECT count(*) AS reviewed FROM "Archive" a CROSS JOIN w JOIN cs ON cs.entry_guid = a."EntityAnalysisModelInstanceEntryGuid" WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t),
                        kpi AS (
                            SELECT 1 AS ord, 'Live rules' AS label, count(*)::numeric AS cur, NULL::numeric AS prior, 'n' AS kind, 'N' AS rag, 'active and not in shadow' AS note FROM rl WHERE rl."Active" = 1 AND coalesce(rl."Shadow", 0) = 0
                            UNION ALL SELECT 2, 'Shadow rules', count(*)::numeric, NULL, 'n', 'N', 'recording matches only' FROM rl WHERE rl."Shadow" = 1
                            UNION ALL SELECT 3, 'Alerts that opened a case', rv.reviewed::numeric / NULLIF(al.alerts, 0), NULL, 'p', CASE WHEN al.alerts = 0 THEN 'N' WHEN rv.reviewed::numeric / al.alerts < 0.7 THEN 'A' ELSE 'G' END, 'of ' || al.alerts || ' alerts' FROM al, rv
                            UNION ALL SELECT 4, 'Rules never confirmed', count(*)::numeric, NULL, 'n', CASE WHEN count(*) = 0 THEN 'G' ELSE 'A' END, 'alerts closed with no confirmed outcome' FROM per WHERE per.closed > 0 AND per.confirmed = 0
                        )
                        SELECT k.ord AS "Ord", k.label AS "Label",
                               coalesce(CASE k.kind WHEN 'p' THEN to_char(k.cur * 100, 'FM990.0') || '%' WHEN 'd' THEN to_char(k.cur, 'FM990.0') WHEN 'm' THEN to_char(k.cur, 'FM999,999,990.00') ELSE to_char(k.cur, 'FM999,999,990') END, '—') AS "Display",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN '▬' WHEN k.cur > k.prior * 1.005 THEN '▲' WHEN k.cur < k.prior * 0.995 THEN '▼' ELSE '▬' END AS "Arrow",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN 'no prior' ELSE to_char(abs(k.cur - k.prior) / k.prior * 100, 'FM999,990') || '% vs prior' END AS "Change",
                               k.rag AS "Rag", k.note AS "Note"
                        FROM kpi k ORDER BY k.ord$cmd$, 10, 0, 12, 1, v_datasource_guid, 29, $tpl$<div class="grid grid-cols-4 gap-2">
                          <div class="stat bg-base-200 rounded-box" v-for="row in rows">
                            <div class="stat-title">[@ row.Label @]</div>
                            <div class="stat-value text-2xl">[@ row.Display @]</div>
                            <div class="stat-desc">
                              <span class="badge badge-sm [@ row.Rag == 'G' ? 'badge-success' : row.Rag == 'A' ? 'badge-warning' : row.Rag == 'R' ? 'badge-error' : 'badge-ghost' @]">[@ row.Arrow @] [@ row.Change @]</span>
                              [@ row.Note @]
                            </div>
                          </div>
                        </div>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Ord', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Label', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Display', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Arrow', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Change', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Rag', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Note', 1, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Hit rate against confirmed rate by rule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        ar AS (
                            SELECT k.rule_name, a."EntityAnalysisModelInstanceEntryGuid" AS g, a."CreatedDate", a."ResponseElevation"
                            FROM "Archive" a CROSS JOIN w
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        ),
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
                        per AS (
                            SELECT ar.rule_name, count(*) AS alerts, count(cs."Id") AS cases, count(*) FILTER (WHERE cs.closed) AS closed,
                                   count(*) FILTER (WHERE cs.closed AND cs.confirmed) AS confirmed, count(*) FILTER (WHERE cs.closed AND cs.fp) AS fp
                            FROM ar LEFT JOIN cs ON cs.entry_guid = ar.g GROUP BY ar.rule_name
                        ),
                        tx AS (SELECT count(*) AS n FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t)
                        SELECT per.rule_name AS "Rule", round(100.0 * per.alerts / NULLIF(tx.n, 0), 2) AS "Hit rate %",
                               round(100.0 * per.confirmed / NULLIF(per.closed, 0), 1) AS "Confirmed rate %", per.cases AS "Cases"
                        FROM per CROSS JOIN tx WHERE per.closed > 0 ORDER BY 1$cmd$, 20, 1, 6, 3, v_datasource_guid, 13, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 3, 1),
                                (v_datasource_id, 'Hit rate %', 3, 2, 4, 1),
                                (v_datasource_id, 'Confirmed rate %', 3, 3, 5, 1),
                                (v_datasource_id, 'Cases', 2, 4, 6, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Alert volume by rule and month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT k.rule_name AS "Rule", to_char(date_trunc('month', a."CreatedDate"), 'YYYY-MM') AS "Month", count(*) AS "Alerts"
                        FROM "Archive" a
                        CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                        WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= date_trunc('month', (now() at time zone 'utc') - interval '11 months')
                        GROUP BY k.rule_name, date_trunc('month', a."CreatedDate") ORDER BY date_trunc('month', a."CreatedDate"), k.rule_name$cmd$, 30, 0, 6, 3, v_datasource_guid, 15, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 24, 1),
                                (v_datasource_id, 'Month', 1, 2, 25, 1),
                                (v_datasource_id, 'Alerts', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Confirmed value by typology and country', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        csj AS (
                            SELECT c."Id", c."Json" AS json, c."CreatedDate", c."ClosedDate", (c."ClosedStatusId" = 3) AS closed, c."ClosedReasonId", c."ClosedUser", c."CaseKeyValue", c."LockedUser",
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
                        SELECT coalesce(rl."Name", 'Unknown') AS "Typology", cs.json->'payload'->>@Country_Field AS "Country",
                               round(sum(CASE WHEN cs.json->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (cs.json->'payload'->>@Value_Field)::numeric END), 0) AS "Confirmed value"
                        FROM csj cs CROSS JOIN w LEFT JOIN rl ON rl."Guid" = cs.rule_guid
                        WHERE cs.closed AND cs.confirmed AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t AND cs.json->'payload'->>@Country_Field IS NOT NULL
                        GROUP BY 1, 2 ORDER BY 1, 2$cmd$, 40, 0, 6, 3, v_datasource_guid, 15, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Typology', 1, 1, 24, 1),
                                (v_datasource_id, 'Country', 1, 2, 25, 1),
                                (v_datasource_id, 'Confirmed value', 3, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Confirmed cases by country', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        csj AS (
                            SELECT c."Id", c."Json" AS json, c."CreatedDate", c."ClosedDate", (c."ClosedStatusId" = 3) AS closed, c."ClosedReasonId", c."ClosedUser", c."CaseKeyValue", c."LockedUser",
                                   c."EntityAnalysisModelActivationRuleGuid" AS rule_guid, c."EntityAnalysisModelInstanceEntryGuid" AS entry_guid, c."CaseWorkflowStatusGuid" AS status_guid,
                                   wf."ResolutionTargetMinutes" AS resolution_target, wf."FirstViewTargetMinutes" AS first_view_target, wf."OwnershipTargetMinutes" AS ownership_target,
                                   (c."ClosedReasonId" IN (5, 6) OR st."OutcomeId" = 1) AS confirmed,
                                   (c."ClosedReasonId" IN (1, 2) OR st."OutcomeId" = 2) AS fp,
                                   st."Name" AS status_name
                            FROM "Case" c
                            LEFT JOIN "CaseWorkflow" wf ON wf."Guid" = c."CaseWorkflowGuid" AND (wf."Deleted" = 0 OR wf."Deleted" IS NULL)
                            LEFT JOIN "CaseWorkflowStatus" st ON st."Guid" = c."CaseWorkflowStatusGuid" AND (st."Deleted" = 0 OR st."Deleted" IS NULL)
                        )
                        SELECT cs.json->'payload'->>@Country_Field AS "Country", count(*) AS "Confirmed cases"
                        FROM csj cs CROSS JOIN w
                        WHERE cs.closed AND cs.confirmed AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t AND cs.json->'payload'->>@Country_Field IS NOT NULL
                        GROUP BY 1 ORDER BY 2 DESC$cmd$, 50, 0, 6, 3, v_datasource_guid, 28, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Country', 1, 1, 18, 1),
                                (v_datasource_id, 'Confirmed cases', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Response elevation of alerts', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT a."ResponseElevation" AS "Response elevation"
                        FROM "Archive" a CROSS JOIN w
                        WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t LIMIT 5000$cmd$, 60, 0, 4, 2, v_datasource_guid, 14, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Response elevation', 3, 1, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Shadow rules against live rules', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        sh AS (
                            SELECT k.rule_name, (a."ActivationRuleCount" > 0) AS live
                            FROM "Archive" a CROSS JOIN w
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'shadowActivation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        )
                        SELECT sh.rule_name AS "Shadow rule", count(*) AS "Matches", count(*) FILTER (WHERE sh.live) AS "Also alerted by a live rule",
                               count(*) FILTER (WHERE NOT sh.live) AS "Net new", round(100.0 * count(*) FILTER (WHERE sh.live) / count(*), 1) AS "Overlap %"
                        FROM sh GROUP BY sh.rule_name ORDER BY 2 DESC$cmd$, 70, 1, 8, 2, v_datasource_guid, NULL, NULL)
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
                            VALUES (v_registry_id, 'Rule review', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        ar AS (
                            SELECT k.rule_name, a."EntityAnalysisModelInstanceEntryGuid" AS g, a."CreatedDate", a."ResponseElevation"
                            FROM "Archive" a CROSS JOIN w
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(a."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        ),
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
                        rl AS (SELECT r."Guid", r."Name", r."Shadow", r."Active", r."EnableCaseWorkflow" FROM "EntityAnalysisModelActivationRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)),
                        per AS (
                            SELECT ar.rule_name, count(*) AS alerts, count(cs."Id") AS cases, count(*) FILTER (WHERE cs.closed) AS closed,
                                   count(*) FILTER (WHERE cs.closed AND cs.confirmed) AS confirmed, count(*) FILTER (WHERE cs.closed AND cs.fp) AS fp
                            FROM ar LEFT JOIN cs ON cs.entry_guid = ar.g GROUP BY ar.rule_name
                        )
                        SELECT rl."Name" AS "Rule", CASE WHEN rl."Shadow" = 1 THEN 'Shadow' WHEN rl."Active" = 1 THEN 'Live' ELSE 'Inactive' END AS "State",
                               coalesce(per.alerts, 0) AS "Alerts", coalesce(per.cases, 0) AS "Cases", coalesce(per.closed, 0) AS "Closed", coalesce(per.confirmed, 0) AS "Confirmed",
                               CASE WHEN rl."Shadow" = 1 THEN 'Shadow rule: matches are recorded and have no effect'
                                    WHEN coalesce(per.alerts, 0) = 0 THEN 'No alerts in the window'
                                    WHEN rl."EnableCaseWorkflow" IS DISTINCT FROM 1 THEN 'Opens no cases'
                                    WHEN coalesce(per.closed, 0) > 0 AND coalesce(per.confirmed, 0) = 0 THEN 'Never confirmed'
                                    ELSE 'Producing confirmed outcomes' END AS "Finding"
                        FROM rl LEFT JOIN per ON per.rule_name = rl."Name"
                        ORDER BY 7, 1$cmd$, 80, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'State', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Alerts', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Cases', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Closed', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Confirmed', 2, 6, NULL, NULL),
                                (v_datasource_id, 'Finding', 1, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'False-positive rate by rule over 30, 90 and 365 days', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH cs AS (
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
                        SELECT coalesce(rl."Name", 'Unknown') AS "Rule",
                               count(*) FILTER (WHERE cs."ClosedDate" >= (now() at time zone 'utc') - interval '30 days') AS "Closed 30 days",
                               round(100.0 * count(*) FILTER (WHERE cs.fp AND cs."ClosedDate" >= (now() at time zone 'utc') - interval '30 days') / NULLIF(count(*) FILTER (WHERE cs."ClosedDate" >= (now() at time zone 'utc') - interval '30 days'), 0), 1) AS "False-positive % 30 days",
                               count(*) FILTER (WHERE cs."ClosedDate" >= (now() at time zone 'utc') - interval '90 days') AS "Closed 90 days",
                               round(100.0 * count(*) FILTER (WHERE cs.fp AND cs."ClosedDate" >= (now() at time zone 'utc') - interval '90 days') / NULLIF(count(*) FILTER (WHERE cs."ClosedDate" >= (now() at time zone 'utc') - interval '90 days'), 0), 1) AS "False-positive % 90 days",
                               count(*) AS "Closed 365 days",
                               round(100.0 * count(*) FILTER (WHERE cs.fp) / NULLIF(count(*), 0), 1) AS "False-positive % 365 days"
                        FROM cs LEFT JOIN rl ON rl."Guid" = cs.rule_guid
                        WHERE cs.closed AND cs."ClosedDate" >= (now() at time zone 'utc') - interval '365 days'
                        GROUP BY 1 ORDER BY 6 DESC$cmd$, 90, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Closed 30 days', 2, 2, NULL, NULL),
                                (v_datasource_id, 'False-positive % 30 days', 3, 3, NULL, NULL),
                                (v_datasource_id, 'Closed 90 days', 2, 4, NULL, NULL),
                                (v_datasource_id, 'False-positive % 90 days', 3, 5, NULL, NULL),
                                (v_datasource_id, 'Closed 365 days', 2, 6, NULL, NULL),
                                (v_datasource_id, 'False-positive % 365 days', 3, 7, NULL, NULL);
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
