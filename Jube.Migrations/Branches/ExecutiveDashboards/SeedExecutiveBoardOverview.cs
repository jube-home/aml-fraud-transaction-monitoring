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

    [Migration(2026111791000000)]
    public class SeedExecutiveBoardOverview : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Executive: Board Compliance Overview' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Executive: Board Compliance Overview', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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
                            VALUES (v_registry_id, 'Analyst Hourly Cost', 1, 0, v_now, 'Administrator', 0, 1, 3, '45', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Minutes Per Case', 1, 0, v_now, 'Administrator', 0, 1, 3, '30', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Health strip', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
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
                        a AS (
                            SELECT count(*) FILTER (WHERE x."CreatedDate" >= w.f AND x."CreatedDate" < w.t) AS txns,
                                   count(*) FILTER (WHERE x."CreatedDate" >= w.f AND x."CreatedDate" < w.t AND x."ActivationRuleCount" > 0) AS alerts,
                                   count(*) FILTER (WHERE x."CreatedDate" >= pw.f AND x."CreatedDate" < pw.t) AS ptxns,
                                   count(*) FILTER (WHERE x."CreatedDate" >= pw.f AND x."CreatedDate" < pw.t AND x."ActivationRuleCount" > 0) AS palerts
                            FROM "Archive" x CROSS JOIN w CROSS JOIN pw
                            WHERE x."CreatedDate" >= pw.f AND x."CreatedDate" < w.t
                        ),
                        c AS (
                            SELECT count(*) FILTER (WHERE cs.closed AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t) AS closed,
                                   count(*) FILTER (WHERE cs.closed AND cs."ClosedDate" >= pw.f AND cs."ClosedDate" < pw.t) AS pclosed,
                                   count(*) FILTER (WHERE cs.closed AND cs.fp AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t) AS fp,
                                   count(*) FILTER (WHERE cs.closed AND cs.fp AND cs."ClosedDate" >= pw.f AND cs."ClosedDate" < pw.t) AS pfp,
                                   count(*) FILTER (WHERE cs.closed AND cs.resolution_target IS NOT NULL AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t) AS targeted,
                                   count(*) FILTER (WHERE cs.closed AND cs.resolution_target IS NOT NULL AND cs."ClosedDate" >= pw.f AND cs."ClosedDate" < pw.t) AS ptargeted,
                                   count(*) FILTER (WHERE cs.closed AND cs.resolution_target IS NOT NULL AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t AND extract(epoch FROM (cs."ClosedDate" - cs."CreatedDate")) / 60 > cs.resolution_target) AS breach,
                                   count(*) FILTER (WHERE cs.closed AND cs.resolution_target IS NOT NULL AND cs."ClosedDate" >= pw.f AND cs."ClosedDate" < pw.t AND extract(epoch FROM (cs."ClosedDate" - cs."CreatedDate")) / 60 > cs.resolution_target) AS pbreach,
                                   percentile_cont(0.5) WITHIN GROUP (ORDER BY extract(epoch FROM (cs."ClosedDate" - cs."CreatedDate")) / 86400.0) FILTER (WHERE cs.closed AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t) AS med_days,
                                   percentile_cont(0.5) WITHIN GROUP (ORDER BY extract(epoch FROM (cs."ClosedDate" - cs."CreatedDate")) / 86400.0) FILTER (WHERE cs.closed AND cs."ClosedDate" >= pw.f AND cs."ClosedDate" < pw.t) AS pmed_days,
                                   count(*) FILTER (WHERE cs.closed AND cs."ClosedReasonId" = 5 AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t) AS reported,
                                   count(*) FILTER (WHERE cs.closed AND cs."ClosedReasonId" = 5 AND cs."ClosedDate" >= pw.f AND cs."ClosedDate" < pw.t) AS preported,
                                   count(*) FILTER (WHERE cs."CreatedDate" < w.f AND (NOT cs.closed OR cs."ClosedDate" >= w.f)) AS open_start,
                                   count(*) FILTER (WHERE NOT cs.closed) AS open_now
                            FROM cs CROSS JOIN w CROSS JOIN pw
                        ),
                        ap AS (
                            SELECT count(*) AS pending FROM "EntityAnalysisModelActivationRule" r
                            WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                              AND NOT EXISTS (SELECT 1 FROM "EntityApproval" e WHERE e."EntityApprovalKindId" = 16 AND e."EntityId" = r."Id" AND e."EntityVersion" = r."Version" AND e."StateId" = 1)
                        ),
                        kpi AS (
                            SELECT 1 AS ord, 'Alerts' AS label, a.alerts::numeric AS cur, a.palerts::numeric AS prior, 'n' AS kind,
                                   CASE WHEN a.palerts = 0 THEN 'N' WHEN a.alerts > a.palerts * 1.5 THEN 'R' WHEN a.alerts > a.palerts * 1.25 THEN 'A' ELSE 'G' END AS rag,
                                   'transactions that fired a rule' AS note FROM a
                            UNION ALL SELECT 2, 'False-positive rate', c.fp::numeric / NULLIF(c.closed, 0), c.pfp::numeric / NULLIF(c.pclosed, 0), 'p',
                                   CASE WHEN c.closed = 0 THEN 'N' WHEN c.fp::numeric / c.closed > 0.85 THEN 'R' WHEN c.fp::numeric / c.closed > 0.70 THEN 'A' ELSE 'G' END,
                                   'of ' || c.closed || ' closed cases' FROM c
                            UNION ALL SELECT 3, 'Median days to close', c.med_days::numeric, c.pmed_days::numeric, 'd',
                                   CASE WHEN c.med_days IS NULL THEN 'N' WHEN c.med_days > 3 THEN 'R' WHEN c.med_days > 2 THEN 'A' ELSE 'G' END, 'closed in the window' FROM c
                            UNION ALL SELECT 4, 'Open backlog', c.open_now::numeric, c.open_start::numeric, 'n',
                                   CASE WHEN c.open_now > 100 THEN 'R' WHEN c.open_now > 50 THEN 'A' ELSE 'G' END, 'open now against open at the start' FROM c
                            UNION ALL SELECT 5, 'Resolution target breached', c.breach::numeric / NULLIF(c.targeted, 0), c.pbreach::numeric / NULLIF(c.ptargeted, 0), 'p',
                                   CASE WHEN c.targeted = 0 THEN 'N' WHEN c.breach::numeric / c.targeted > 0.25 THEN 'R' WHEN c.breach::numeric / c.targeted > 0.10 THEN 'A' ELSE 'G' END,
                                   'of closed cases with a target' FROM c
                            UNION ALL SELECT 6, 'Reported', c.reported::numeric, c.preported::numeric, 'n', 'N', 'closed as suspicion confirmed and reported' FROM c
                            UNION ALL SELECT 7, 'Rule changes awaiting approval', ap.pending::numeric, NULL, 'n', CASE WHEN ap.pending = 0 THEN 'G' WHEN ap.pending > 2 THEN 'R' ELSE 'A' END, 'live rule versions without an approval' FROM ap
                            UNION ALL SELECT 8, 'Alerts per 1,000 transactions', a.alerts * 1000.0 / NULLIF(a.txns, 0), a.palerts * 1000.0 / NULLIF(a.ptxns, 0), 'd',
                                   CASE WHEN a.txns = 0 THEN 'N' WHEN a.alerts * 1000.0 / a.txns > 120 THEN 'R' WHEN a.alerts * 1000.0 / a.txns > 90 THEN 'A' ELSE 'G' END, 'alert rate' FROM a
                        )
                        SELECT k.ord AS "Ord", k.label AS "Label",
                               coalesce(CASE k.kind WHEN 'p' THEN to_char(k.cur * 100, 'FM990.0') || '%' WHEN 'd' THEN to_char(k.cur, 'FM990.0') WHEN 'm' THEN to_char(k.cur, 'FM999,999,990.00') ELSE to_char(k.cur, 'FM999,999,990') END, '—') AS "Display",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN '▬' WHEN k.cur > k.prior * 1.005 THEN '▲' WHEN k.cur < k.prior * 0.995 THEN '▼' ELSE '▬' END AS "Arrow",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN 'no prior' ELSE to_char(abs(k.cur - k.prior) / k.prior * 100, 'FM999,990') || '% vs prior' END AS "Change",
                               k.rag AS "Rag", k.note AS "Note"
                        FROM kpi k ORDER BY k.ord$cmd$, 10, 0, 12, 2, v_datasource_guid, 29, $tpl$<div class="grid grid-cols-4 gap-2">
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
                            VALUES (v_registry_id, 'Detection funnel', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        t AS (SELECT count(*) AS n FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t),
                        al AS (SELECT a."EntityAnalysisModelInstanceEntryGuid" AS g FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t AND a."ActivationRuleCount" > 0),
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
                        cohort AS (SELECT cs.* FROM cs JOIN al ON al.g = cs.entry_guid)
                        SELECT 'Transactions' AS "Stage", t.n AS "Count" FROM t
                        UNION ALL SELECT 'Alerts', count(*) FROM al
                        UNION ALL SELECT 'Cases opened', count(*) FROM cohort
                        UNION ALL SELECT 'Closed', count(*) FILTER (WHERE closed) FROM cohort
                        UNION ALL SELECT 'Confirmed', count(*) FILTER (WHERE closed AND confirmed) FROM cohort
                        UNION ALL SELECT 'Reported', count(*) FILTER (WHERE closed AND "ClosedReasonId" = 5) FROM cohort$cmd$, 20, 0, 4, 3, v_datasource_guid, 20, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Stage', 1, 1, 1, 1),
                                (v_datasource_id, 'Count', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Alerts, cases and false-positive rate by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms),
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
                        )
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month",
                               (SELECT count(*) FROM "Archive" a WHERE a."CreatedDate" >= m.ms AND a."CreatedDate" < m.ms + interval '1 month' AND a."ActivationRuleCount" > 0) AS "Alerts",
                               (SELECT count(*) FROM cs WHERE cs."CreatedDate" >= m.ms AND cs."CreatedDate" < m.ms + interval '1 month') AS "Cases opened",
                               (SELECT round(100.0 * count(*) FILTER (WHERE cs.fp) / NULLIF(count(*), 0), 1) FROM cs WHERE cs.closed AND cs."ClosedDate" >= m.ms AND cs."ClosedDate" < m.ms + interval '1 month') AS "False-positive rate %"
                        FROM m ORDER BY m.ms$cmd$, 30, 1, 8, 3, v_datasource_guid, 16, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Alerts', 2, 2, 2, 1),
                                (v_datasource_id, 'Cases opened', 2, 3, 2, 2),
                                (v_datasource_id, 'False-positive rate %', 3, 4, 27, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Backlog by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms),
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
                        )
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month",
                               (SELECT count(*) FROM cs WHERE cs."CreatedDate" >= m.ms AND cs."CreatedDate" < m.ms + interval '1 month') AS "Opened",
                               (SELECT count(*) FROM cs WHERE cs.closed AND cs."ClosedDate" >= m.ms AND cs."ClosedDate" < m.ms + interval '1 month') AS "Closed",
                               (SELECT count(*) FROM cs WHERE cs."CreatedDate" < m.ms + interval '1 month' AND (NOT cs.closed OR cs."ClosedDate" >= m.ms + interval '1 month')) AS "Open at month end"
                        FROM m ORDER BY m.ms$cmd$, 40, 0, 6, 3, v_datasource_guid, 16, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Opened', 2, 2, 2, 1),
                                (v_datasource_id, 'Closed', 2, 3, 2, 2),
                                (v_datasource_id, 'Open at month end', 2, 4, 26, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Outcome mix by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms),
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
                        )
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month", CASE cs."ClosedReasonId" WHEN 1 THEN 'False positive: known activity' WHEN 2 THEN 'False positive: rule too broad' WHEN 3 THEN 'Duplicate' WHEN 4 THEN 'Insufficient evidence' WHEN 5 THEN 'Suspicion confirmed and reported' WHEN 6 THEN 'Fraud confirmed' WHEN 7 THEN 'Customer explained' WHEN 8 THEN 'Other' ELSE 'No reason recorded' END AS "Outcome", count(*) AS "Cases"
                        FROM m JOIN cs ON cs.closed AND cs."ClosedDate" >= m.ms AND cs."ClosedDate" < m.ms + interval '1 month'
                        GROUP BY m.ms, 2 ORDER BY m.ms, 2$cmd$, 50, 0, 6, 3, v_datasource_guid, 5, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Outcome', 1, 2, 3, 1),
                                (v_datasource_id, 'Cases', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Efficiency scorecard', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
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
                        a AS (
                            SELECT count(*) FILTER (WHERE x."CreatedDate" >= w.f AND x."CreatedDate" < w.t) AS txns,
                                   count(*) FILTER (WHERE x."CreatedDate" >= w.f AND x."CreatedDate" < w.t AND x."ActivationRuleCount" > 0) AS alerts
                            FROM "Archive" x CROSS JOIN w WHERE x."CreatedDate" >= w.f AND x."CreatedDate" < w.t
                        ),
                        c AS (
                            SELECT count(*) FILTER (WHERE cs."CreatedDate" >= w.f AND cs."CreatedDate" < w.t) AS opened,
                                   count(*) FILTER (WHERE cs.closed AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t) AS closed,
                                   count(*) FILTER (WHERE cs.closed AND cs.confirmed AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t) AS confirmed
                            FROM cs CROSS JOIN w
                        ),
                        tr AS (
                            SELECT count(*) FILTER (WHERE e."CaseEventTypeId" = 30) AS accepted, count(*) FILTER (WHERE e."CaseEventTypeId" = 31) AS overridden
                            FROM "CaseEvent" e CROSS JOIN w WHERE e."CaseEventTypeId" IN (30, 31) AND e."CreatedDate" >= w.f AND e."CreatedDate" < w.t
                        ),
                        kpi AS (
                            SELECT 1 AS ord, 'Estimated cost per alert' AS label, (c.opened::numeric / NULLIF(a.alerts, 0)) * @Minutes_Per_Case * @Analyst_Hourly_Cost / 60.0 AS cur, NULL::numeric AS prior, 'm' AS kind, 'N' AS rag,
                                   'investigation effort only, from the two cost parameters' AS note FROM a, c
                            UNION ALL SELECT 2, 'Estimated cost per confirmed case', c.closed * @Minutes_Per_Case * @Analyst_Hourly_Cost / 60.0 / NULLIF(c.confirmed, 0), NULL, 'm', 'N', 'all closed effort over confirmed cases' FROM c
                            UNION ALL SELECT 3, 'Alerts per 1,000 transactions', a.alerts * 1000.0 / NULLIF(a.txns, 0), NULL, 'd', 'N', 'alert rate' FROM a
                            UNION ALL SELECT 4, 'Triage recommendations accepted', tr.accepted::numeric / NULLIF(tr.accepted + tr.overridden, 0), NULL, 'p',
                                   CASE WHEN tr.accepted + tr.overridden = 0 THEN 'N' WHEN tr.accepted::numeric / (tr.accepted + tr.overridden) < 0.6 THEN 'R' WHEN tr.accepted::numeric / (tr.accepted + tr.overridden) < 0.8 THEN 'A' ELSE 'G' END,
                                   'of ' || (tr.accepted + tr.overridden) || ' decided' FROM tr
                        )
                        SELECT k.ord AS "Ord", k.label AS "Label",
                               coalesce(CASE k.kind WHEN 'p' THEN to_char(k.cur * 100, 'FM990.0') || '%' WHEN 'd' THEN to_char(k.cur, 'FM990.0') WHEN 'm' THEN to_char(k.cur, 'FM999,999,990.00') ELSE to_char(k.cur, 'FM999,999,990') END, '—') AS "Display",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN '▬' WHEN k.cur > k.prior * 1.005 THEN '▲' WHEN k.cur < k.prior * 0.995 THEN '▼' ELSE '▬' END AS "Arrow",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN 'no prior' ELSE to_char(abs(k.cur - k.prior) / k.prior * 100, 'FM999,990') || '% vs prior' END AS "Change",
                               k.rag AS "Rag", k.note AS "Note"
                        FROM kpi k ORDER BY k.ord$cmd$, 60, 0, 12, 1, v_datasource_guid, 29, $tpl$<div class="grid grid-cols-4 gap-2">
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
                            VALUES (v_registry_id, 'Largest movements by rule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        ar AS (
                            SELECT x."CreatedDate", k.rule_name, (x."CreatedDate" >= w.f) AS is_current
                            FROM "Archive" x CROSS JOIN w CROSS JOIN pw
                            CROSS JOIN LATERAL jsonb_object_keys(COALESCE(x."Json"->'activation', '{}'::jsonb)) AS k(rule_name)
                            WHERE x."ActivationRuleCount" > 0 AND x."CreatedDate" >= pw.f AND x."CreatedDate" < w.t
                        )
                        SELECT ar.rule_name AS "Rule",
                               count(*) FILTER (WHERE ar.is_current) AS "Alerts",
                               count(*) FILTER (WHERE NOT ar.is_current) AS "Prior alerts",
                               count(*) FILTER (WHERE ar.is_current) - count(*) FILTER (WHERE NOT ar.is_current) AS "Change",
                               round(100.0 * (count(*) FILTER (WHERE ar.is_current) - count(*) FILTER (WHERE NOT ar.is_current)) / NULLIF(count(*) FILTER (WHERE NOT ar.is_current), 0), 0) AS "Change %"
                        FROM ar GROUP BY ar.rule_name
                        ORDER BY abs(count(*) FILTER (WHERE ar.is_current) - count(*) FILTER (WHERE NOT ar.is_current)) DESC, ar.rule_name
                        LIMIT 15$cmd$, 70, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Alerts', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Prior alerts', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Change', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Change %', 3, 5, NULL, NULL);
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
