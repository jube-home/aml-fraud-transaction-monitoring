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

    [Migration(2026111791000003)]
    public class SeedExecutiveRuleAndModelGovernance : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Executive: Rule and Model Governance' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Executive: Rule and Model Governance', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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
                            VALUES (v_registry_id, 'Review Interval Days', 1, 0, v_now, 'Administrator', 0, 1, 2, '180', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Score stability', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        cur AS (SELECT width_bucket(least(greatest(a."ResponseElevation", 0), 3.999), 0, 4, 20) AS b, count(*) AS n FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t GROUP BY 1),
                        prv AS (SELECT width_bucket(least(greatest(a."ResponseElevation", 0), 3.999), 0, 4, 20) AS b, count(*) AS n FROM "Archive" a CROSS JOIN pw WHERE a."CreatedDate" >= pw.f AND a."CreatedDate" < pw.t GROUP BY 1),
                        tc AS (SELECT sum(n) AS t FROM cur), tp AS (SELECT sum(n) AS t FROM prv),
                        psi AS (
                            SELECT sum((coalesce(c.n, 0)::numeric / tc.t + 0.0001 - (coalesce(p.n, 0)::numeric / tp.t + 0.0001)) * ln((coalesce(c.n, 0)::numeric / tc.t + 0.0001) / (coalesce(p.n, 0)::numeric / tp.t + 0.0001))) AS v
                            FROM generate_series(1, 20) bn(b) LEFT JOIN cur c ON c.b = bn.b LEFT JOIN prv p ON p.b = bn.b CROSS JOIN tc CROSS JOIN tp WHERE tc.t > 0 AND tp.t > 0
                        ),
                        kpi AS (
                            SELECT 1 AS ord, 'Score stability (population stability index)' AS label, psi.v AS cur, NULL::numeric AS prior, 'q' AS kind,
                                   CASE WHEN psi.v IS NULL THEN 'N' WHEN psi.v >= 0.25 THEN 'R' WHEN psi.v >= 0.10 THEN 'A' ELSE 'G' END AS rag,
                                   CASE WHEN psi.v IS NULL THEN 'not enough data' WHEN psi.v >= 0.25 THEN 'significant shift in the score distribution' WHEN psi.v >= 0.10 THEN 'moderate shift' ELSE 'stable against the prior window' END AS note
                            FROM psi
                        )
                        SELECT k.ord AS "Ord", k.label AS "Label",
                               coalesce(CASE k.kind WHEN 'p' THEN to_char(k.cur * 100, 'FM990.0') || '%' WHEN 'd' THEN to_char(k.cur, 'FM990.0') WHEN 'q' THEN to_char(k.cur, 'FM990.000') WHEN 'm' THEN to_char(k.cur, 'FM999,999,990.00') ELSE to_char(k.cur, 'FM999,999,990') END, '—') AS "Display",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN '▬' WHEN k.cur > k.prior * 1.005 THEN '▲' WHEN k.cur < k.prior * 0.995 THEN '▼' ELSE '▬' END AS "Arrow",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN 'no prior' ELSE to_char(abs(k.cur - k.prior) / k.prior * 100, 'FM999,990') || '% vs prior' END AS "Change",
                               k.rag AS "Rag", k.note AS "Note"
                        FROM kpi k ORDER BY k.ord$cmd$, 10, 0, 4, 1, v_datasource_guid, 29, $tpl$<div class="grid grid-cols-4 gap-2">
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
                            VALUES (v_registry_id, 'Maker-checker integrity', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        rv AS (
                            SELECT r."Id" AS id, r."Version" AS version, r."CreatedUser" AS maker FROM "EntityAnalysisModelActivationRule" r
                            UNION ALL SELECT v."EntityAnalysisModelActivationRuleId", v."Version", v."CreatedUser" FROM "EntityAnalysisModelActivationRuleVersion" v
                        ),
                        rvt AS (SELECT v."EntityAnalysisModelActivationRuleId" AS id, v."Version" AS version, v."CreatedDate" AS made FROM "EntityAnalysisModelActivationRuleVersion" v),
                        ap AS (
                            SELECT e."StateId", e."ReturnedDate", e."CreatedUser" AS checker, e."CreatedDate", e."EntityApprovalKindId",
                                   (SELECT rv.maker FROM rv WHERE e."EntityApprovalKindId" = 16 AND rv.id = e."EntityId" AND rv.version = e."EntityVersion" LIMIT 1) AS maker,
                                   (SELECT rvt.made FROM rvt WHERE e."EntityApprovalKindId" = 16 AND rvt.id = e."EntityId" AND rvt.version = e."EntityVersion" LIMIT 1) AS made
                            FROM "EntityApproval" e CROSS JOIN w WHERE e."CreatedDate" >= w.f AND e."CreatedDate" < w.t
                        ),
                        kpi AS (
                            SELECT 1 AS ord, 'Changes approved' AS label, count(*) FILTER (WHERE ap."StateId" = 1)::numeric AS cur, NULL::numeric AS prior, 'n' AS kind, 'N' AS rag, 'in the window' AS note FROM ap
                            UNION ALL SELECT 2, 'Changes rejected', count(*) FILTER (WHERE ap."StateId" = 2)::numeric, NULL, 'n', 'N', 'sent back by the approver' FROM ap
                            UNION ALL SELECT 3, 'Returned by the requester', count(*) FILTER (WHERE ap."ReturnedDate" IS NOT NULL)::numeric, NULL, 'n', 'N', 'retracted or resubmitted' FROM ap
                            UNION ALL SELECT 4, 'Rule changes approved by their maker', count(*) FILTER (WHERE ap.maker IS NOT NULL AND ap.maker = ap.checker)::numeric, NULL, 'n',
                                   CASE WHEN count(*) FILTER (WHERE ap.maker IS NOT NULL AND ap.maker = ap.checker) = 0 THEN 'G' ELSE 'R' END, 'separation of duties; should be zero' FROM ap
                            UNION ALL SELECT 5, 'Median hours to approve a rule change', (percentile_cont(0.5) WITHIN GROUP (ORDER BY extract(epoch FROM (ap."CreatedDate" - ap.made)) / 3600) FILTER (WHERE ap.made IS NOT NULL))::numeric, NULL, 'd',
                                   'N', 'superseded rule versions approved in the window' FROM ap
                        )
                        SELECT k.ord AS "Ord", k.label AS "Label",
                               coalesce(CASE k.kind WHEN 'p' THEN to_char(k.cur * 100, 'FM990.0') || '%' WHEN 'd' THEN to_char(k.cur, 'FM990.0') WHEN 'q' THEN to_char(k.cur, 'FM990.000') WHEN 'm' THEN to_char(k.cur, 'FM999,999,990.00') ELSE to_char(k.cur, 'FM999,999,990') END, '—') AS "Display",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN '▬' WHEN k.cur > k.prior * 1.005 THEN '▲' WHEN k.cur < k.prior * 0.995 THEN '▼' ELSE '▬' END AS "Arrow",
                               CASE WHEN k.prior IS NULL OR k.prior = 0 THEN 'no prior' ELSE to_char(abs(k.cur - k.prior) / k.prior * 100, 'FM999,990') || '% vs prior' END AS "Change",
                               k.rag AS "Rag", k.note AS "Note"
                        FROM kpi k ORDER BY k.ord$cmd$, 20, 0, 8, 2, v_datasource_guid, 29, $tpl$<div class="grid grid-cols-4 gap-2">
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
                            VALUES (v_registry_id, 'Governance matrix', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH thr AS (SELECT @Review_Interval_Days::numeric AS days),
                        ent AS (
                            SELECT 'Rule' AS kind, r."Id" AS id, r."Name" AS name, CASE WHEN r."Shadow" = 1 THEN 'Shadow' WHEN r."Active" = 1 THEN 'Live' ELSE 'Inactive' END AS state, r."Version" AS version, 16 AS approval_kind
                            FROM "EntityAnalysisModelActivationRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL
                            SELECT 'Model', m."Id", m."Name", CASE WHEN m."Active" = 1 THEN 'Live' ELSE 'Inactive' END, m."Version", 1
                            FROM "EntityAnalysisModel" m WHERE (m."Deleted" = 0 OR m."Deleted" IS NULL)
                            UNION ALL
                            SELECT 'Exhaustive model', x."Id", x."Name", CASE WHEN x."Active" = 1 THEN 'Live' ELSE 'Inactive' END, x."Version", 15
                            FROM "ExhaustiveSearchInstance" x WHERE (x."Deleted" = 0 OR x."Deleted" IS NULL)
                        ),
                        bt AS (
                            SELECT b."RuleId", max(b."CompletedDate") AS last_backtest
                            FROM "EntityAnalysisModelBacktestInstance" b WHERE b."RuleType" = 'Activation' AND b."Status" = 3 AND (b."Deleted" = 0 OR b."Deleted" IS NULL) GROUP BY b."RuleId"
                        ),
                        m AS (
                            SELECT ent.*, la."CreatedDate" AS approved_on, la."CreatedUser" AS approved_by,
                                   NOT EXISTS (SELECT 1 FROM "EntityApproval" e WHERE e."EntityApprovalKindId" = ent.approval_kind AND e."EntityId" = ent.id AND e."EntityVersion" = ent.version AND e."StateId" = 1) AS awaiting,
                                   bt.last_backtest
                            FROM ent
                            LEFT JOIN LATERAL (SELECT e."CreatedDate", e."CreatedUser" FROM "EntityApproval" e WHERE e."EntityApprovalKindId" = ent.approval_kind AND e."EntityId" = ent.id AND e."StateId" = 1 ORDER BY e."CreatedDate" DESC LIMIT 1) la ON true
                            LEFT JOIN bt ON ent.approval_kind = 16 AND bt."RuleId" = ent.id
                        )
                        SELECT m.kind AS "Kind", m.name AS "Name", m.state AS "State", m.version AS "Version", m.approved_on AS "Last approved", coalesce(m.approved_by, '') AS "Approved by",
                               round(extract(epoch FROM ((now() at time zone 'utc') - m.approved_on)) / 86400)::integer AS "Days since approval",
                               CASE WHEN m.approved_on IS NULL THEN 'Not approved'
                                    WHEN extract(epoch FROM ((now() at time zone 'utc') - m.approved_on)) / 86400 > thr.days THEN 'Overdue'
                                    WHEN extract(epoch FROM ((now() at time zone 'utc') - m.approved_on)) / 86400 > thr.days * 0.8 THEN 'Due soon' ELSE 'Current' END AS "Review",
                               CASE WHEN m.awaiting THEN 'Yes' ELSE 'No' END AS "Awaiting approval", m.last_backtest AS "Last backtest"
                        FROM m CROSS JOIN thr
                        ORDER BY CASE WHEN m.awaiting THEN 0 ELSE 1 END, m.kind, m.name$cmd$, 30, 1, 12, 4, v_datasource_guid, 29, $tpl$<table class="table table-xs w-full">
                          <thead><tr><th>Kind</th><th>Name</th><th>State</th><th>Ver</th><th>Last approved</th><th>By</th><th>Days</th><th>Review</th><th>Awaiting</th><th>Last backtest</th></tr></thead>
                          <tbody>
                            <tr v-for="row in rows">
                              <td>[@ row.Kind @]</td>
                              <td class="font-semibold">[@ row.Name @]</td>
                              <td><span class="badge badge-sm [@ row.State == 'Live' ? 'badge-success' : row.State == 'Shadow' ? 'badge-info' : 'badge-ghost' @]">[@ row.State @]</span></td>
                              <td class="tabular-nums">[@ row.Version @]</td>
                              <td>[@ row['Last approved'] | date @]</td>
                              <td>[@ row['Approved by'] @]</td>
                              <td class="tabular-nums">[@ row['Days since approval'] @]</td>
                              <td><span class="badge badge-sm [@ row.Review == 'Overdue' || row.Review == 'Not approved' ? 'badge-error' : row.Review == 'Due soon' ? 'badge-warning' : 'badge-success' @]">[@ row.Review @]</span></td>
                              <td><span class="badge badge-sm [@ row['Awaiting approval'] == 'Yes' ? 'badge-warning' : 'badge-ghost' @]">[@ row['Awaiting approval'] @]</span></td>
                              <td>[@ row['Last backtest'] | date @]</td>
                            </tr>
                          </tbody>
                        </table>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Kind', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Name', 1, 2, NULL, NULL),
                                (v_datasource_id, 'State', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Last approved', 4, 5, NULL, NULL),
                                (v_datasource_id, 'Approved by', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Days since approval', 2, 7, NULL, NULL),
                                (v_datasource_id, 'Review', 1, 8, NULL, NULL),
                                (v_datasource_id, 'Awaiting approval', 1, 9, NULL, NULL),
                                (v_datasource_id, 'Last backtest', 4, 10, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Changes by month and kind', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms)
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month", CASE e."EntityApprovalKindId" WHEN 1 THEN 'Model' WHEN 2 THEN 'List' WHEN 3 THEN 'List value' WHEN 4 THEN 'Dictionary' WHEN 5 THEN 'Dictionary value' WHEN 6 THEN 'Request field'
                            WHEN 7 THEN 'Inline script' WHEN 8 THEN 'Inline function' WHEN 9 THEN 'Gateway rule' WHEN 10 THEN 'Sanction' WHEN 11 THEN 'Abstraction rule' WHEN 12 THEN 'Abstraction calculation'
                            WHEN 13 THEN 'TTL counter' WHEN 14 THEN 'HTTP adaptation' WHEN 15 THEN 'Exhaustive model' WHEN 16 THEN 'Activation rule' WHEN 17 THEN 'Tag' WHEN 18 THEN 'Advanced analytics instance'
                            WHEN 19 THEN 'Response elevation range' WHEN 20 THEN 'Graph recall' WHEN 21 THEN 'Svirfd' WHEN 22 THEN 'Override' WHEN 23 THEN 'Rule override' ELSE 'Other' END AS "Kind", count(*) AS "Changes"
                        FROM m JOIN "EntityApproval" e ON e."CreatedDate" >= m.ms AND e."CreatedDate" < m.ms + interval '1 month'
                        GROUP BY m.ms, 2 ORDER BY m.ms, 2$cmd$, 40, 0, 6, 3, v_datasource_guid, 3, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Kind', 1, 2, 3, 1),
                                (v_datasource_id, 'Changes', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Score stability by week', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH wk AS (SELECT generate_series(date_trunc('week', now() at time zone 'utc') - interval '11 weeks', date_trunc('week', now() at time zone 'utc'), interval '1 week') AS ws),
                        base AS (SELECT width_bucket(least(greatest(a."ResponseElevation", 0), 3.999), 0, 4, 20) AS b, count(*) AS n FROM "Archive" a WHERE a."CreatedDate" >= (now() at time zone 'utc') - interval '365 days' AND a."CreatedDate" < (now() at time zone 'utc') - interval '84 days' GROUP BY 1),
                        bt AS (SELECT sum(n) AS t FROM base),
                        wc AS (SELECT wk.ws, width_bucket(least(greatest(a."ResponseElevation", 0), 3.999), 0, 4, 20) AS b, count(*) AS n FROM wk JOIN "Archive" a ON a."CreatedDate" >= wk.ws AND a."CreatedDate" < wk.ws + interval '1 week' GROUP BY 1, 2),
                        cur AS (SELECT wk.ws, bn.b, coalesce(wc.n, 0) AS n FROM wk CROSS JOIN generate_series(1, 20) bn(b) LEFT JOIN wc ON wc.ws = wk.ws AND wc.b = bn.b),
                        ct AS (SELECT ws, sum(n) AS t FROM cur GROUP BY ws)
                        SELECT to_char(cur.ws, 'IYYY-"W"IW') AS "Week",
                               round(sum((cur.n::numeric / ct.t + 0.0001 - (coalesce(base.n, 0)::numeric / bt.t + 0.0001)) * ln((cur.n::numeric / ct.t + 0.0001) / (coalesce(base.n, 0)::numeric / bt.t + 0.0001))), 4) AS "Population stability index"
                        FROM cur JOIN ct ON ct.ws = cur.ws CROSS JOIN bt LEFT JOIN base ON base.b = cur.b
                        WHERE ct.t > 0 AND bt.t > 0 GROUP BY cur.ws ORDER BY cur.ws$cmd$, 50, 0, 6, 3, v_datasource_guid, 7, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Week', 1, 1, 1, 1),
                                (v_datasource_id, 'Population stability index', 3, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rule hit rate by week', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT r."Name" AS "Rule", to_char(date_trunc('week', h."CreatedDate"), 'IYYY-"W"IW') AS "Week",
                               round(100.0 * sum(h."ActivationCounter") / NULLIF(sum(h."EvaluationCounter"), 0), 2) AS "Hit rate %"
                        FROM "EntityAnalysisModelActivationRuleCounterHistory" h
                        JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = h."EntityAnalysisModelActivationRuleId"
                        WHERE h."CreatedDate" >= (now() at time zone 'utc') - interval '84 days'
                        GROUP BY r."Name", date_trunc('week', h."CreatedDate") ORDER BY date_trunc('week', h."CreatedDate"), r."Name"$cmd$, 60, 0, 6, 3, v_datasource_guid, 7, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 3, 1),
                                (v_datasource_id, 'Week', 1, 2, 1, 1),
                                (v_datasource_id, 'Hit rate %', 3, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'AI triage oversight', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms)
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month",
                               count(*) FILTER (WHERE e."CaseEventTypeId" = 29) AS "Recommended",
                               count(*) FILTER (WHERE e."CaseEventTypeId" = 30) AS "Accepted",
                               count(*) FILTER (WHERE e."CaseEventTypeId" = 31) AS "Overridden",
                               round(100.0 * count(*) FILTER (WHERE e."CaseEventTypeId" = 30) / NULLIF(count(*) FILTER (WHERE e."CaseEventTypeId" IN (30, 31)), 0), 1) AS "Acceptance %"
                        FROM m LEFT JOIN "CaseEvent" e ON e."CaseEventTypeId" IN (29, 30, 31) AND e."CreatedDate" >= m.ms AND e."CreatedDate" < m.ms + interval '1 month'
                        GROUP BY m.ms ORDER BY m.ms$cmd$, 70, 0, 6, 3, v_datasource_guid, 16, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Recommended', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Accepted', 2, 3, 2, 1),
                                (v_datasource_id, 'Overridden', 2, 4, 2, 2),
                                (v_datasource_id, 'Acceptance %', 3, 5, 27, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Machine-learning models', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT x."Name" AS "Model", CASE x."StatusId" WHEN 16 THEN 'Finished' WHEN 19 THEN 'Stopped' WHEN 20 THEN 'No data' ELSE 'Status ' || x."StatusId" END AS "Status",
                               x."Models" AS "Models tried", round(x."Score"::numeric, 3) AS "Score", x."CreatedDate" AS "Started", x."CompletedDate" AS "Completed",
                               p.tp AS "True positives", p.fp AS "False positives", p.fn AS "False negatives", p.tn AS "True negatives",
                               round(100.0 * p.tp / NULLIF(p.tp + p.fp, 0), 1) AS "Precision %", round(100.0 * p.tp / NULLIF(p.tp + p.fn, 0), 1) AS "Recall %",
                               round(extract(epoch FROM ((now() at time zone 'utc') - x."CompletedDate")) / 86400)::integer AS "Days since trained"
                        FROM "ExhaustiveSearchInstance" x
                        LEFT JOIN LATERAL (
                            SELECT pt."TruePositive" AS tp, pt."FalsePositive" AS fp, pt."FalseNegative" AS fn, pt."TrueNegative" AS tn
                            FROM "ExhaustiveSearchInstancePromotedTrialInstance" pt
                            JOIN "ExhaustiveSearchInstanceTrialInstance" ti ON ti."Id" = pt."ExhaustiveSearchInstanceTrialInstanceId"
                            WHERE ti."ExhaustiveSearchInstanceId" = x."Id" AND (pt."Deleted" = 0 OR pt."Deleted" IS NULL) ORDER BY pt."Id" DESC LIMIT 1
                        ) p ON true
                        WHERE (x."Deleted" = 0 OR x."Deleted" IS NULL) ORDER BY x."Name"$cmd$, 80, 1, 12, 2, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Model', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Status', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Models tried', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Score', 3, 4, NULL, NULL),
                                (v_datasource_id, 'Started', 4, 5, NULL, NULL),
                                (v_datasource_id, 'Completed', 4, 6, NULL, NULL),
                                (v_datasource_id, 'True positives', 2, 7, NULL, NULL),
                                (v_datasource_id, 'False positives', 2, 8, NULL, NULL),
                                (v_datasource_id, 'False negatives', 2, 9, NULL, NULL),
                                (v_datasource_id, 'True negatives', 2, 10, NULL, NULL),
                                (v_datasource_id, 'Precision %', 3, 11, NULL, NULL),
                                (v_datasource_id, 'Recall %', 3, 12, NULL, NULL),
                                (v_datasource_id, 'Days since trained', 2, 13, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Backtests', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT r."Name" AS "Rule", b."CreatedDate" AS "Run", b."CreatedUser" AS "Run by",
                               CASE b."Status" WHEN 1 THEN 'Pending' WHEN 2 THEN 'Running' WHEN 3 THEN 'Succeeded' WHEN 4 THEN 'Failed' WHEN 5 THEN 'Cancelling' ELSE 'Cancelled' END AS "Outcome",
                               b."Scanned" AS "Scanned", (b."Result"->>'Fired')::bigint AS "Fired", (b."Result"->>'TruePositives')::bigint AS "True positives", (b."Result"->>'FalsePositives')::bigint AS "False positives",
                               (b."Result"->>'FalseNegatives')::bigint AS "False negatives",
                               round(100.0 * (b."Result"->>'TruePositives')::numeric / NULLIF((b."Result"->>'TruePositives')::numeric + (b."Result"->>'FalsePositives')::numeric, 0), 1) AS "Precision %",
                               round(100.0 * (b."Result"->>'TruePositives')::numeric / NULLIF((b."Result"->>'TruePositives')::numeric + (b."Result"->>'FalseNegatives')::numeric, 0), 1) AS "Recall %"
                        FROM "EntityAnalysisModelBacktestInstance" b
                        LEFT JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = b."RuleId"
                        WHERE (b."Deleted" = 0 OR b."Deleted" IS NULL) ORDER BY b."CreatedDate" DESC LIMIT 200$cmd$, 90, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Run', 4, 2, NULL, NULL),
                                (v_datasource_id, 'Run by', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Outcome', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Scanned', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Fired', 2, 6, NULL, NULL),
                                (v_datasource_id, 'True positives', 2, 7, NULL, NULL),
                                (v_datasource_id, 'False positives', 2, 8, NULL, NULL),
                                (v_datasource_id, 'False negatives', 2, 9, NULL, NULL),
                                (v_datasource_id, 'Precision %', 3, 10, NULL, NULL),
                                (v_datasource_id, 'Recall %', 3, 11, NULL, NULL);
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
