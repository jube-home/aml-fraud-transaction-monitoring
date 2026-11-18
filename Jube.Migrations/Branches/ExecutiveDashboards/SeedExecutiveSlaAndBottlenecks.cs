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

    [Migration(2026111791000001)]
    public class SeedExecutiveSlaAndBottlenecks : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Executive: SLA and Bottlenecks' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Executive: SLA and Bottlenecks', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Watch list', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH cs AS (
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
                        last_touch AS (
                            SELECT e."CaseId", max(e."CreatedDate") AS last_event FROM "CaseEvent" e WHERE e."CaseEventTypeId" NOT IN (16) GROUP BY e."CaseId"
                        ),
                        lg AS (SELECT count(*) AS n FROM "Case" c WHERE c."LegalHold" = 1),
                        ph AS (SELECT count(*) AS n FROM "Case" c WHERE c."Diary" = 1 AND c."DiaryTypeId" = 4 AND c."ClosedStatusId" <> 3),
                        dd AS (SELECT count(*) AS n FROM "Case" c WHERE c."ClosedStatusId" <> 3 AND c."RiskNextDeadlineDate" IS NOT NULL AND c."RiskNextDeadlineDate" < (now() at time zone 'utc') + interval '7 days'),
                        un AS (SELECT count(*) AS n FROM cs LEFT JOIN last_touch lt ON lt."CaseId" = cs."Id" WHERE NOT cs.closed AND coalesce(lt.last_event, cs."CreatedDate") < (now() at time zone 'utc') - interval '7 days'),
                        kpi AS (
                            SELECT 1 AS ord, 'Untouched for 7 days' AS label, un.n::numeric AS cur, NULL::numeric AS prior, 'n' AS kind, CASE WHEN un.n = 0 THEN 'G' WHEN un.n > 10 THEN 'R' ELSE 'A' END AS rag, 'open cases with no activity' AS note FROM un
                            UNION ALL SELECT 2, 'Deadlines due in 7 days', dd.n::numeric, NULL, 'n', CASE WHEN dd.n = 0 THEN 'G' WHEN dd.n > 15 THEN 'R' ELSE 'A' END, 'open cases at or past their deadline' FROM dd
                            UNION ALL SELECT 3, 'Payment holds in force', ph.n::numeric, NULL, 'n', 'N', 'open cases holding a payment' FROM ph
                            UNION ALL SELECT 4, 'Legal holds in force', lg.n::numeric, NULL, 'n', 'N', 'cases kept beyond retention' FROM lg
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
                            VALUES (v_registry_id, 'Open cases by age and status', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH cs AS (
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
                        op AS (
                            SELECT coalesce(cs.status_name, 'Unknown') AS status_name,
                                   CASE WHEN (now() at time zone 'utc') - cs."CreatedDate" < interval '24 hours' THEN '1: under 24 hours'
                                        WHEN (now() at time zone 'utc') - cs."CreatedDate" < interval '3 days' THEN '2: 1 to 3 days'
                                        WHEN (now() at time zone 'utc') - cs."CreatedDate" < interval '7 days' THEN '3: 3 to 7 days'
                                        ELSE '4: over 7 days' END AS bucket
                            FROM cs WHERE NOT cs.closed
                        )
                        SELECT op.status_name AS "Status", op.bucket AS "Age", count(*) AS "Cases" FROM op GROUP BY 1, 2 ORDER BY 1, 2$cmd$, 20, 0, 6, 3, v_datasource_guid, 4, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Status', 1, 1, 1, 1),
                                (v_datasource_id, 'Age', 1, 2, 3, 1),
                                (v_datasource_id, 'Cases', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Response times against targets', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
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
                        ev AS (
                            SELECT e."CaseId", min(e."CreatedDate") FILTER (WHERE e."CaseEventTypeId" = 4) AS first_view,
                                   min(e."CreatedDate") FILTER (WHERE e."CaseEventTypeId" IN (6, 14)) AS owned
                            FROM "CaseEvent" e WHERE e."CaseEventTypeId" IN (4, 6, 14) GROUP BY e."CaseId"
                        ),
                        x AS (
                            SELECT extract(epoch FROM (ev.first_view - cs."CreatedDate")) / 60 AS fv, extract(epoch FROM (ev.owned - cs."CreatedDate")) / 60 AS ow,
                                   CASE WHEN cs.closed THEN extract(epoch FROM (cs."ClosedDate" - cs."CreatedDate")) / 60 END AS res,
                                   cs.first_view_target, cs.ownership_target, cs.resolution_target
                            FROM cs JOIN ev ON ev."CaseId" = cs."Id" CROSS JOIN w WHERE cs."CreatedDate" >= w.f AND cs."CreatedDate" < w.t
                        )
                        SELECT t.label AS "Measure", round(t.val::numeric, 0) AS "Minutes", round(t.target::numeric, 0) AS "Target"
                        FROM (
                            SELECT 1 AS o, 'First view, median' AS label, percentile_cont(0.5) WITHIN GROUP (ORDER BY fv) AS val, avg(first_view_target) AS target FROM x
                            UNION ALL SELECT 2, 'First view, 90th percentile', percentile_cont(0.9) WITHIN GROUP (ORDER BY fv), avg(first_view_target) FROM x
                            UNION ALL SELECT 3, 'Ownership, median', percentile_cont(0.5) WITHIN GROUP (ORDER BY ow), avg(ownership_target) FROM x
                            UNION ALL SELECT 4, 'Ownership, 90th percentile', percentile_cont(0.9) WITHIN GROUP (ORDER BY ow), avg(ownership_target) FROM x
                            UNION ALL SELECT 5, 'Resolution, median', percentile_cont(0.5) WITHIN GROUP (ORDER BY res), avg(resolution_target) FROM x
                            UNION ALL SELECT 6, 'Resolution, 90th percentile', percentile_cont(0.9) WITHIN GROUP (ORDER BY res), avg(resolution_target) FROM x
                        ) t WHERE t.val IS NOT NULL ORDER BY t.o$cmd$, 30, 1, 6, 3, v_datasource_guid, 24, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Measure', 1, 1, 1, 1),
                                (v_datasource_id, 'Minutes', 3, 2, 2, 1),
                                (v_datasource_id, 'Target', 3, 3, 23, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Targets missed by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms),
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
                        ev AS (
                            SELECT e."CaseId", min(e."CreatedDate") FILTER (WHERE e."CaseEventTypeId" = 4) AS first_view,
                                   min(e."CreatedDate") FILTER (WHERE e."CaseEventTypeId" IN (6, 14)) AS owned
                            FROM "CaseEvent" e WHERE e."CaseEventTypeId" IN (4, 6, 14) GROUP BY e."CaseId"
                        ),
                        x AS (
                            SELECT cs."CreatedDate", extract(epoch FROM (ev.first_view - cs."CreatedDate")) / 60 > cs.first_view_target AS b_fv,
                                   extract(epoch FROM (ev.owned - cs."CreatedDate")) / 60 > cs.ownership_target AS b_ow,
                                   CASE WHEN cs.closed THEN extract(epoch FROM (cs."ClosedDate" - cs."CreatedDate")) / 60 > cs.resolution_target
                                        ELSE extract(epoch FROM ((now() at time zone 'utc') - cs."CreatedDate")) / 60 > cs.resolution_target END AS b_res
                            FROM cs LEFT JOIN ev ON ev."CaseId" = cs."Id"
                        )
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month", d.dimension AS "Target missed",
                               count(*) FILTER (WHERE (d.dimension = 'First view' AND x.b_fv) OR (d.dimension = 'Ownership' AND x.b_ow) OR (d.dimension = 'Resolution' AND x.b_res)) AS "Cases"
                        FROM m CROSS JOIN (VALUES ('First view'), ('Ownership'), ('Resolution')) AS d(dimension)
                        JOIN x ON x."CreatedDate" >= m.ms AND x."CreatedDate" < m.ms + interval '1 month'
                        GROUP BY m.ms, d.dimension ORDER BY m.ms, d.dimension$cmd$, 40, 0, 6, 3, v_datasource_guid, 3, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Target missed', 1, 2, 3, 1),
                                (v_datasource_id, 'Cases', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Time in each status', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        mv AS (
                            SELECT e."CaseId", e."CreatedDate", e."Before",
                                   e."CreatedDate" - coalesce(lag(e."CreatedDate") OVER (PARTITION BY e."CaseId" ORDER BY e."CreatedDate"), c."CreatedDate") AS dwell
                            FROM "CaseEvent" e JOIN "Case" c ON c."Id" = e."CaseId"
                            WHERE e."CaseEventTypeId" IN (9, 17)
                        ),
                        st AS (SELECT s."Guid"::text AS guid, s."Name" FROM "CaseWorkflowStatus" s WHERE (s."Deleted" = 0 OR s."Deleted" IS NULL))
                        SELECT st."Name" AS "Status",
                               round(min(extract(epoch FROM mv.dwell) / 60)::numeric, 0) AS "Minimum",
                               round((percentile_cont(0.25) WITHIN GROUP (ORDER BY extract(epoch FROM mv.dwell) / 60))::numeric, 0) AS "First quartile",
                               round((percentile_cont(0.5) WITHIN GROUP (ORDER BY extract(epoch FROM mv.dwell) / 60))::numeric, 0) AS "Median",
                               round((percentile_cont(0.75) WITHIN GROUP (ORDER BY extract(epoch FROM mv.dwell) / 60))::numeric, 0) AS "Third quartile",
                               round(max(extract(epoch FROM mv.dwell) / 60)::numeric, 0) AS "Maximum"
                        FROM mv CROSS JOIN w JOIN st ON st.guid = mv."Before"
                        WHERE mv."CreatedDate" >= w.f AND mv."CreatedDate" < w.t
                        GROUP BY st."Name" ORDER BY st."Name"$cmd$, 50, 1, 6, 3, v_datasource_guid, 17, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Status', 1, 1, 1, 1),
                                (v_datasource_id, 'Minimum', 3, 2, 11, 1),
                                (v_datasource_id, 'First quartile', 3, 3, 12, 1),
                                (v_datasource_id, 'Median', 3, 4, 13, 1),
                                (v_datasource_id, 'Third quartile', 3, 5, 14, 1),
                                (v_datasource_id, 'Maximum', 3, 6, 15, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Queue by assignee', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH cs AS (
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
                        op AS (
                            SELECT coalesce(cs."LockedUser", 'Unassigned') AS who, cs."CreatedDate", cs.resolution_target
                            FROM cs WHERE NOT cs.closed
                        )
                        SELECT op.who AS "Assignee", count(*) AS "Open",
                               count(*) FILTER (WHERE op.resolution_target IS NOT NULL AND extract(epoch FROM ((now() at time zone 'utc') - op."CreatedDate")) / 60 > op.resolution_target) AS "Over target",
                               round(max(extract(epoch FROM ((now() at time zone 'utc') - op."CreatedDate")) / 3600)::numeric, 0) AS "Oldest hours",
                               CASE WHEN count(*) FILTER (WHERE op.resolution_target IS NOT NULL AND extract(epoch FROM ((now() at time zone 'utc') - op."CreatedDate")) / 60 > op.resolution_target) > 3 THEN 'R'
                                    WHEN count(*) FILTER (WHERE op.resolution_target IS NOT NULL AND extract(epoch FROM ((now() at time zone 'utc') - op."CreatedDate")) / 60 > op.resolution_target) > 0 THEN 'A' ELSE 'G' END AS "Rag"
                        FROM op GROUP BY op.who ORDER BY 3 DESC, 2 DESC$cmd$, 60, 1, 6, 3, v_datasource_guid, 29, $tpl$<table class="table table-sm w-full">
                          <thead><tr><th>Assignee</th><th>Open</th><th>Over target</th><th>Oldest (hours)</th><th>Status</th></tr></thead>
                          <tbody>
                            <tr v-for="row in rows">
                              <td>[@ row.Assignee @]</td>
                              <td class="tabular-nums">[@ row.Open @]</td>
                              <td class="tabular-nums">[@ row['Over target'] @]</td>
                              <td class="tabular-nums">[@ row['Oldest hours'] @]</td>
                              <td><span class="badge badge-sm [@ row.Rag == 'R' ? 'badge-error' : row.Rag == 'A' ? 'badge-warning' : 'badge-success' @]">[@ row.Rag == 'R' ? 'Behind' : row.Rag == 'A' ? 'At risk' : 'On track' @]</span></td>
                            </tr>
                          </tbody>
                        </table>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Assignee', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Open', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Over target', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Oldest hours', 3, 4, NULL, NULL),
                                (v_datasource_id, 'Rag', 1, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Closures by analyst by week', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH wk AS (SELECT generate_series(date_trunc('week', now() at time zone 'utc') - interval '11 weeks', date_trunc('week', now() at time zone 'utc'), interval '1 week') AS ws),
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
                        SELECT coalesce(cs."ClosedUser", 'Unknown') AS "Analyst", to_char(wk.ws, 'IYYY-"W"IW') AS "Week", count(*) AS "Closed"
                        FROM wk JOIN cs ON cs.closed AND cs."ClosedDate" >= wk.ws AND cs."ClosedDate" < wk.ws + interval '1 week'
                        WHERE cs."ClosedUser" IS NOT NULL
                        GROUP BY 1, wk.ws ORDER BY wk.ws, 1$cmd$, 70, 0, 6, 3, v_datasource_guid, 15, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Analyst', 1, 1, 24, 1),
                                (v_datasource_id, 'Week', 1, 2, 25, 1),
                                (v_datasource_id, 'Closed', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Automatic escalations by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms)
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month", count(e."Id") AS "Automatic escalations"
                        FROM m LEFT JOIN "CaseEvent" e ON e."CaseEventTypeId" = 17 AND e."CreatedDate" >= m.ms AND e."CreatedDate" < m.ms + interval '1 month'
                        GROUP BY m.ms ORDER BY m.ms$cmd$, 80, 0, 6, 2, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Automatic escalations', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Checklist gates and SLA breaches by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms)
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month",
                               CASE e."CaseEventTypeId" WHEN 26 THEN 'Gate blocked' WHEN 27 THEN 'Gate overridden' ELSE 'SLA breached' END AS "Event", count(*) AS "Count"
                        FROM m JOIN "CaseEvent" e ON e."CaseEventTypeId" IN (26, 27, 28) AND e."CreatedDate" >= m.ms AND e."CreatedDate" < m.ms + interval '1 month'
                        GROUP BY m.ms, e."CaseEventTypeId" ORDER BY m.ms, 2$cmd$, 90, 0, 6, 2, v_datasource_guid, 3, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Event', 1, 2, 3, 1),
                                (v_datasource_id, 'Count', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Deadlines falling due', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT c."Id" AS "Case", c."CaseKeyValue" AS "Key", coalesce(st."Name", 'Unknown') AS "Status", c."RiskNextDeadlineDate" AS "Deadline",
                               round(extract(epoch FROM (c."RiskNextDeadlineDate" - (now() at time zone 'utc'))) / 3600)::integer AS "Hours remaining",
                               coalesce(c."LockedUser", 'Unassigned') AS "Assignee",
                               CASE c."RiskLaneId" WHEN 1 THEN 'Live money' WHEN 2 THEN 'Mandatory' WHEN 3 THEN 'Deadline near' ELSE 'Standard' END AS "Lane"
                        FROM "Case" c
                        LEFT JOIN "CaseWorkflowStatus" st ON st."Guid" = c."CaseWorkflowStatusGuid" AND (st."Deleted" = 0 OR st."Deleted" IS NULL)
                        WHERE c."ClosedStatusId" <> 3 AND c."RiskNextDeadlineDate" IS NOT NULL AND c."RiskNextDeadlineDate" < (now() at time zone 'utc') + interval '7 days'
                        ORDER BY c."RiskNextDeadlineDate" LIMIT 200$cmd$, 100, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Case', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Status', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Deadline', 4, 4, NULL, NULL),
                                (v_datasource_id, 'Hours remaining', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Assignee', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Lane', 1, 7, NULL, NULL);
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
