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

    [Migration(2026111791000004)]
    public class SeedExecutiveScreeningOverview : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Executive: Screening Overview' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Executive: Screening Overview', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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
                            VALUES (v_registry_id, 'Screening summary', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        en AS (SELECT e."Id", e."SanctionEntrySourceId" AS source_id, e."CreatedDate" FROM "SanctionEntry" e WHERE (e."Deleted" = 0 OR e."Deleted" IS NULL)),
                        im AS (
                            SELECT i."SanctionEntrySourceId" AS source_id, max(i."StartDate") FILTER (WHERE i."Successful" = 1) AS last_ok
                            FROM "SanctionEntryImport" i GROUP BY i."SanctionEntrySourceId"
                        ),
                        hits AS (
                            SELECT count(*) AS n, count(*) FILTER (WHERE a."Json"->'sanction' IS NOT NULL AND a."Json"->'sanction' <> '{}'::jsonb) AS hit
                            FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        ),
                        kpi AS (
                            SELECT 1 AS ord, 'Listed entries' AS label, count(*)::numeric AS cur, NULL::numeric AS prior, 'n' AS kind, 'N' AS rag, 'across ' || count(DISTINCT en.source_id) || ' sources' AS note FROM en
                            UNION ALL SELECT 2, 'Days since the oldest source was refreshed', max(extract(epoch FROM ((now() at time zone 'utc') - im.last_ok)) / 86400)::numeric, NULL, 'd',
                                   CASE WHEN max(extract(epoch FROM ((now() at time zone 'utc') - im.last_ok)) / 86400) > 35 THEN 'R' WHEN max(extract(epoch FROM ((now() at time zone 'utc') - im.last_ok)) / 86400) > 8 THEN 'A' ELSE 'G' END,
                                   'the stalest list governs' FROM im
                            UNION ALL SELECT 3, 'Transactions screened', hits.n::numeric, NULL, 'n', 'N', 'in the window' FROM hits
                            UNION ALL SELECT 4, 'Screening hits', hits.hit::numeric, NULL, 'n', 'N', 'transactions with a sanction match' FROM hits
                        )
                        SELECT k.ord AS "Ord", k.label AS "Label",
                               coalesce(CASE k.kind WHEN 'p' THEN to_char(k.cur * 100, 'FM990.0') || '%' WHEN 'd' THEN to_char(k.cur, 'FM990.0') WHEN 'q' THEN to_char(k.cur, 'FM990.000') WHEN 'm' THEN to_char(k.cur, 'FM999,999,990.00') ELSE to_char(k.cur, 'FM999,999,990') END, '—') AS "Display",
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
                            VALUES (v_registry_id, 'Listed entries by source', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT coalesce(s."Name", 'Unknown') AS "Source", count(*) AS "Entries"
                        FROM (SELECT e."Id", e."SanctionEntrySourceId" AS source_id, e."CreatedDate" FROM "SanctionEntry" e WHERE (e."Deleted" = 0 OR e."Deleted" IS NULL)) e LEFT JOIN "SanctionEntrySource" s ON s."Id" = e.source_id
                        GROUP BY 1 ORDER BY 2 DESC$cmd$, 20, 0, 4, 2, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Source', 1, 1, 1, 1),
                                (v_datasource_id, 'Entries', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Source freshness', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT s."Name" AS "Source", max(i."StartDate") FILTER (WHERE i."Successful" = 1) AS "Last successful import",
                               round(extract(epoch FROM ((now() at time zone 'utc') - max(i."StartDate") FILTER (WHERE i."Successful" = 1))) / 86400)::integer AS "Days since",
                               count(*) FILTER (WHERE i."Successful" = 0 AND i."StartDate" > (now() at time zone 'utc') - interval '90 days') AS "Failed in 90 days",
                               (SELECT i2."RejectedCount" FROM "SanctionEntryImport" i2 WHERE i2."SanctionEntrySourceId" = s."Id" AND i2."Successful" = 1 ORDER BY i2."StartDate" DESC LIMIT 1) AS "Rows rejected last time"
                        FROM "SanctionEntrySource" s LEFT JOIN "SanctionEntryImport" i ON i."SanctionEntrySourceId" = s."Id"
                        GROUP BY s."Id", s."Name" ORDER BY s."Name"$cmd$, 30, 1, 8, 2, v_datasource_guid, 29, $tpl$<table class="table table-sm w-full">
                          <thead><tr><th>Source</th><th>Last successful import</th><th>Days since</th><th>Failed (90 days)</th><th>Rejected last time</th><th>Status</th></tr></thead>
                          <tbody>
                            <tr v-for="row in rows">
                              <td class="font-semibold">[@ row.Source @]</td>
                              <td>[@ row['Last successful import'] | date @]</td>
                              <td class="tabular-nums">[@ row['Days since'] @]</td>
                              <td class="tabular-nums">[@ row['Failed in 90 days'] @]</td>
                              <td class="tabular-nums">[@ row['Rows rejected last time'] @]</td>
                              <td><span class="badge badge-sm [@ row['Days since'] == null || row['Days since'] > 35 ? 'badge-error' : row['Days since'] > 8 ? 'badge-warning' : 'badge-success' @]">[@ row['Days since'] == null ? 'Never imported' : row['Days since'] > 35 ? 'Stale' : row['Days since'] > 8 ? 'Ageing' : 'Fresh' @]</span></td>
                            </tr>
                          </tbody>
                        </table>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Source', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Last successful import', 4, 2, NULL, NULL),
                                (v_datasource_id, 'Days since', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Failed in 90 days', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Rows rejected last time', 2, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Import results by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms)
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month", v.measure AS "Rows", sum(v.n) AS "Count"
                        FROM m JOIN "SanctionEntryImport" i ON i."StartDate" >= m.ms AND i."StartDate" < m.ms + interval '1 month'
                        CROSS JOIN LATERAL (VALUES ('Inserted', i."InsertedCount"), ('Removed', i."RemovedCount"), ('Rejected', i."RejectedCount")) AS v(measure, n)
                        GROUP BY m.ms, v.measure ORDER BY m.ms, v.measure$cmd$, 40, 0, 6, 3, v_datasource_guid, 3, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Rows', 1, 2, 3, 1),
                                (v_datasource_id, 'Count', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rejected rows by reason', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT CASE r."ReasonId" WHEN 1 THEN 'Too few fields' WHEN 2 THEN 'No reference column' WHEN 3 THEN 'Could not be parsed' ELSE 'Other' END AS "Reason", count(*) AS "Rows"
                        FROM "SanctionEntryRejection" r GROUP BY 1 ORDER BY 2 DESC$cmd$, 50, 0, 3, 3, v_datasource_guid, 12, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Reason', 1, 1, 1, 1),
                                (v_datasource_id, 'Rows', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Hits by match distance', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT 'Distance ' || (j.value)::text AS "Distance", count(*) AS "Hits"
                        FROM "Archive" a CROSS JOIN w
                        CROSS JOIN LATERAL jsonb_each(COALESCE(a."Json"->'sanction', '{}'::jsonb)) AS j(key, value)
                        WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        GROUP BY 1 ORDER BY 1$cmd$, 60, 0, 3, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Distance', 1, 1, 1, 1),
                                (v_datasource_id, 'Hits', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Transactions screened and hits by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT to_char(date_trunc('month', a."CreatedDate"), 'YYYY-MM') AS "Month", count(*) AS "Transactions screened",
                               count(*) FILTER (WHERE a."Json"->'sanction' IS NOT NULL AND a."Json"->'sanction' <> '{}'::jsonb) AS "Hits"
                        FROM "Archive" a WHERE a."CreatedDate" >= date_trunc('month', (now() at time zone 'utc') - interval '11 months')
                        GROUP BY date_trunc('month', a."CreatedDate") ORDER BY date_trunc('month', a."CreatedDate")$cmd$, 70, 0, 6, 3, v_datasource_guid, 16, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Transactions screened', 2, 2, 2, 1),
                                (v_datasource_id, 'Hits', 2, 3, 27, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Outcomes of screening cases by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms),
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
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month",
                               CASE WHEN NOT cs.closed THEN 'Open' WHEN cs.confirmed THEN 'Confirmed' WHEN cs.fp THEN 'False positive' ELSE 'Other closed' END AS "Outcome", count(*) AS "Cases"
                        FROM m JOIN csj cs ON cs."CreatedDate" >= m.ms AND cs."CreatedDate" < m.ms + interval '1 month'
                        WHERE cs.json->'sanction' IS NOT NULL AND cs.json->'sanction' <> '{}'::jsonb
                        GROUP BY m.ms, 2 ORDER BY m.ms, 2$cmd$, 80, 0, 6, 3, v_datasource_guid, 6, NULL)
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
                            VALUES (v_registry_id, 'Screening configuration', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT s."Name" AS "Screening", s."MultipartStringDataName" AS "Field screened", s."Distance" AS "Distance", s."MatchAlgorithmId" AS "Algorithm", s."MinimumSimilarity" AS "Minimum similarity",
                               s."MaxDistanceRatio" AS "Maximum distance ratio", CASE WHEN s."Active" = 1 THEN 'Yes' ELSE 'No' END AS "Active", s."Version" AS "Version", s."CreatedUser" AS "Created by"
                        FROM "EntityAnalysisModelSanction" s WHERE (s."Deleted" = 0 OR s."Deleted" IS NULL) ORDER BY s."Name"$cmd$, 90, 1, 12, 2, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Screening', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Field screened', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Distance', 5, 3, NULL, NULL),
                                (v_datasource_id, 'Algorithm', 5, 4, NULL, NULL),
                                (v_datasource_id, 'Minimum similarity', 3, 5, NULL, NULL),
                                (v_datasource_id, 'Maximum distance ratio', 3, 6, NULL, NULL),
                                (v_datasource_id, 'Active', 1, 7, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 8, NULL, NULL),
                                (v_datasource_id, 'Created by', 1, 9, NULL, NULL);
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
