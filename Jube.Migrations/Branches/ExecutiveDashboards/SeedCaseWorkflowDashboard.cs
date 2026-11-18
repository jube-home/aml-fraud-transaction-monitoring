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

    [Migration(2026111793000000)]
    public class SeedCaseWorkflowDashboard : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Case: Workflow Dashboard' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Case: Workflow Dashboard', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 110, 1, v_registry_guid, 2, 0)
                            RETURNING "Id" INTO v_registry_id;

                            INSERT INTO "VisualisationRegistryRole" ("Guid", "VisualisationRegistryGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_registry_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Case at a glance', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT c."Id" AS "Case", c."CaseKey" AS "Key", c."CaseKeyValue" AS "Value", coalesce(st."Name", 'Unknown') AS "Status", coalesce(wf."Name", 'Unknown') AS "Workflow",
                               CASE WHEN c."ClosedStatusId" = 3 THEN 'Closed' ELSE 'Open' END AS "State",
                               round(extract(epoch FROM (coalesce(c."ClosedDate", now() at time zone 'utc') - c."CreatedDate")) / 3600)::integer AS "Age hours", CASE c."RiskLaneId" WHEN 1 THEN 'Live money' WHEN 2 THEN 'Mandatory' WHEN 3 THEN 'Deadline near' ELSE 'Standard' END AS "Lane", c."RiskPriority" AS "Priority", c."Rating" AS "Rating",
                               coalesce(c."LockedUser", 'Unassigned') AS "Assignee", c."RiskNextDeadlineDate" AS "Deadline", CASE WHEN c."LegalHold" = 1 THEN 'Yes' ELSE 'No' END AS "Legal hold",
                               CASE c."QaSampleStatusId" WHEN 2 THEN 'Sampled' ELSE 'Not sampled' END AS "Quality review", CASE c."ClosedReasonId" WHEN 1 THEN 'False positive: known activity' WHEN 2 THEN 'False positive: rule too broad' WHEN 3 THEN 'Duplicate' WHEN 4 THEN 'Insufficient evidence' WHEN 5 THEN 'Suspicion confirmed and reported' WHEN 6 THEN 'Fraud confirmed' WHEN 7 THEN 'Customer explained' WHEN 8 THEN 'Other' ELSE '' END AS "Close reason"
                        FROM "Case" c
                        LEFT JOIN "CaseWorkflowStatus" st ON st."Guid" = c."CaseWorkflowStatusGuid" AND (st."Deleted" = 0 OR st."Deleted" IS NULL)
                        LEFT JOIN "CaseWorkflow" wf ON wf."Guid" = c."CaseWorkflowGuid" AND (wf."Deleted" = 0 OR wf."Deleted" IS NULL)
                        WHERE c."Id" = @Id$cmd$, 10, 0, 12, 2, v_datasource_guid, 29, $tpl$<div class="grid grid-cols-4 gap-2">
                          <template v-for="row in rows">
                            <div class="stat bg-base-200 rounded-box"><div class="stat-title">Status</div><div class="stat-value text-xl">[@ row.Status @]</div><div class="stat-desc"><span class="badge badge-sm [@ row.State == 'Closed' ? 'badge-neutral' : 'badge-info' @]">[@ row.State @]</span> [@ row.Workflow @]</div></div>
                            <div class="stat bg-base-200 rounded-box"><div class="stat-title">[@ row.Key @]</div><div class="stat-value text-xl">[@ row.Value @]</div><div class="stat-desc">case [@ row.Case @]</div></div>
                            <div class="stat bg-base-200 rounded-box"><div class="stat-title">Age</div><div class="stat-value text-xl">[@ row['Age hours'] @] h</div><div class="stat-desc">as at page load</div></div>
                            <div class="stat bg-base-200 rounded-box"><div class="stat-title">Assignee</div><div class="stat-value text-xl">[@ row.Assignee @]</div><div class="stat-desc">[@ row['Quality review'] @]</div></div>
                            <div class="stat bg-base-200 rounded-box"><div class="stat-title">Risk lane</div><div class="stat-value text-xl">[@ row.Lane @]</div><div class="stat-desc">priority [@ row.Priority @], rating [@ row.Rating @]</div></div>
                            <div class="stat bg-base-200 rounded-box"><div class="stat-title">Deadline</div><div class="stat-value text-xl">[@ row.Deadline | date @]</div><div class="stat-desc">next case deadline</div></div>
                            <div class="stat bg-base-200 rounded-box"><div class="stat-title">Legal hold</div><div class="stat-value text-xl">[@ row['Legal hold'] @]</div><div class="stat-desc">retention</div></div>
                            <div class="stat bg-base-200 rounded-box"><div class="stat-title">Outcome</div><div class="stat-value text-xl">[@ row['Close reason'] | default('Not closed') @]</div><div class="stat-desc">close reason</div></div>
                          </template>
                        </div>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Case', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Value', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Status', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Workflow', 1, 5, NULL, NULL),
                                (v_datasource_id, 'State', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Age hours', 2, 7, NULL, NULL),
                                (v_datasource_id, 'Lane', 1, 8, NULL, NULL),
                                (v_datasource_id, 'Priority', 2, 9, NULL, NULL),
                                (v_datasource_id, 'Rating', 5, 10, NULL, NULL),
                                (v_datasource_id, 'Assignee', 1, 11, NULL, NULL),
                                (v_datasource_id, 'Deadline', 4, 12, NULL, NULL),
                                (v_datasource_id, 'Legal hold', 1, 13, NULL, NULL),
                                (v_datasource_id, 'Quality review', 1, 14, NULL, NULL),
                                (v_datasource_id, 'Close reason', 1, 15, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Service levels of the workflow', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH ev AS (
                            SELECT min(e."CreatedDate") FILTER (WHERE e."CaseEventTypeId" IN (2, 4, 16)) AS fv, min(e."CreatedDate") FILTER (WHERE e."CaseEventTypeId" IN (2, 3, 6, 14)) AS own
                            FROM "CaseEvent" e WHERE e."CaseId" = @Id
                        ),
                        x AS (
                            SELECT c."CreatedDate" AS created, c."ClosedDate" AS closed, wf."FirstViewTargetMinutes" AS t1, wf."OwnershipTargetMinutes" AS t2, wf."ResolutionTargetMinutes" AS t3, ev.fv, ev.own, (now() at time zone 'utc') AS nowt
                            FROM "Case" c LEFT JOIN "CaseWorkflow" wf ON wf."Guid" = c."CaseWorkflowGuid" AND (wf."Deleted" = 0 OR wf."Deleted" IS NULL) CROSS JOIN ev WHERE c."Id" = @Id
                        ),
                        d AS (
                            SELECT 1 AS ord, 'First view' AS label, x.t1 AS target, extract(epoch FROM (coalesce(x.fv, x.nowt) - x.created)) / 60 AS elapsed, CASE WHEN x.fv IS NULL THEN 'Waiting' ELSE 'Done' END AS reached FROM x
                            UNION ALL SELECT 2, 'Ownership', x.t2, extract(epoch FROM (coalesce(x.own, x.nowt) - x.created)) / 60, CASE WHEN x.own IS NULL THEN 'Waiting' ELSE 'Done' END FROM x
                            UNION ALL SELECT 3, 'Resolution', x.t3, extract(epoch FROM (coalesce(x.closed, x.nowt) - x.created)) / 60, CASE WHEN x.closed IS NULL THEN 'Open' ELSE 'Closed' END FROM x
                        ),
                        p AS (SELECT d.*, CASE WHEN d.target IS NULL OR d.target = 0 THEN 0 ELSE least(10, greatest(0, floor(d.elapsed / d.target * 10)::integer)) END AS blocks FROM d)
                        SELECT p.label AS "Measure", p.reached AS "Reached", round(p.elapsed::numeric, 0) AS "Elapsed minutes", p.target AS "Target minutes",
                               CASE WHEN p.target IS NULL OR p.target = 0 THEN NULL ELSE round(100.0 * p.elapsed / p.target) END AS "Consumed %",
                               CASE WHEN p.target IS NULL OR p.target = 0 THEN 'N' WHEN p.elapsed >= p.target THEN 'R' WHEN p.elapsed >= p.target * 0.8 THEN 'A' ELSE 'G' END AS "Rag",
                               substr('██████████', 1, p.blocks) || substr('░░░░░░░░░░', 1, 10 - p.blocks) AS "Bar"
                        FROM p ORDER BY p.ord$cmd$, 20, 0, 6, 2, v_datasource_guid, 29, $tpl$<table class="table table-sm w-full">
                          <thead><tr><th>Measure</th><th>Elapsed</th><th>Target</th><th></th><th>Position</th></tr></thead>
                          <tbody>
                            <tr v-for="row in rows">
                              <td class="font-semibold">[@ row.Measure @] <span class="text-xs">[@ row.Reached @]</span></td>
                              <td class="tabular-nums">[@ row['Elapsed minutes'] | number(0) @] min</td>
                              <td class="tabular-nums">[@ row['Target minutes'] | default('none') @]</td>
                              <td class="font-mono">[@ row.Bar @]</td>
                              <td><span class="badge badge-sm [@ row.Rag == 'R' ? 'badge-error' : row.Rag == 'A' ? 'badge-warning' : row.Rag == 'G' ? 'badge-success' : 'badge-neutral' @]">[@ row.Rag == 'R' ? 'Breached' : row.Rag == 'A' ? 'At risk' : row.Rag == 'G' ? 'On track' : 'No target' @]</span></td>
                            </tr>
                          </tbody>
                        </table>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Measure', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Reached', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Elapsed minutes', 3, 3, NULL, NULL),
                                (v_datasource_id, 'Target minutes', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Consumed %', 3, 5, NULL, NULL),
                                (v_datasource_id, 'Rag', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Bar', 1, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Where the case is on the workflow', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT s."Name" AS "Status", CASE WHEN s."Guid" = c."CaseWorkflowStatusGuid" THEN 1 ELSE 0 END AS "Current",
                               CASE s."OutcomeId" WHEN 1 THEN 'Confirmed' WHEN 2 THEN 'Not confirmed' ELSE '' END AS "Outcome", s."MaxDwellMinutes" AS "Dwell limit minutes"
                        FROM "Case" c JOIN "CaseWorkflow" w ON w."Guid" = c."CaseWorkflowGuid" JOIN "CaseWorkflowStatus" s ON s."CaseWorkflowId" = w."Id" AND (s."Deleted" = 0 OR s."Deleted" IS NULL) AND s."Active" = 1
                        WHERE c."Id" = @Id ORDER BY s."Id"$cmd$, 30, 0, 6, 1, v_datasource_guid, 29, $tpl$<div class="flex flex-wrap gap-2 items-center">
                          <div v-for="row in rows" class="card bg-base-200 p-2 min-w-0">
                            <span class="badge [@ row.Current == 1 ? 'badge-primary' : 'badge-outline' @]">[@ row.Status @]</span>
                            <span class="text-xs">[@ row.Outcome @]</span>
                            <span class="text-xs">[@ row['Dwell limit minutes'] == null ? '' : 'dwell limit ' + row['Dwell limit minutes'] + ' min' @]</span>
                          </div>
                        </div>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Status', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Current', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Outcome', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Dwell limit minutes', 2, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Time in the status and what escalates it', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH lm AS (SELECT max(e."CreatedDate") AS at FROM "CaseEvent" e WHERE e."CaseId" = @Id AND e."CaseEventTypeId" IN (8, 9, 17))
                        SELECT st."Name" AS "Status", round(extract(epoch FROM (coalesce(c."ClosedDate", now() at time zone 'utc') - coalesce(lm.at, c."CreatedDate"))) / 60)::integer AS "Minutes in status", st."MaxDwellMinutes" AS "Dwell limit",
                               CASE st."EscalationTriggerId" WHEN 1 THEN 'time in status' WHEN 2 THEN 'resolution target breached' WHEN 3 THEN 'risk lane at least urgent' WHEN 4 THEN 'priority at least the threshold' WHEN 5 THEN 'deadline overdue' ELSE 'no automatic escalation' END AS "Escalates on",
                               st."EscalationThreshold" AS "Threshold", tg."Name" AS "Escalates to",
                               CASE WHEN st."MaxDwellMinutes" IS NULL THEN 'N' WHEN extract(epoch FROM (coalesce(c."ClosedDate", now() at time zone 'utc') - coalesce(lm.at, c."CreatedDate"))) / 60 >= st."MaxDwellMinutes" THEN 'R'
                                    WHEN extract(epoch FROM (coalesce(c."ClosedDate", now() at time zone 'utc') - coalesce(lm.at, c."CreatedDate"))) / 60 >= st."MaxDwellMinutes" * 0.8 THEN 'A' ELSE 'G' END AS "Rag"
                        FROM "Case" c CROSS JOIN lm
                        LEFT JOIN "CaseWorkflowStatus" st ON st."Guid" = c."CaseWorkflowStatusGuid" AND (st."Deleted" = 0 OR st."Deleted" IS NULL)
                        LEFT JOIN "CaseWorkflowStatus" tg ON tg."Guid" = st."EscalationTargetCaseWorkflowStatusGuid" AND (tg."Deleted" = 0 OR tg."Deleted" IS NULL)
                        WHERE c."Id" = @Id$cmd$, 40, 0, 6, 1, v_datasource_guid, 29, $tpl$<div v-for="row in rows" class="stats bg-base-200 rounded-box w-full">
                          <div class="stat"><div class="stat-title">In [@ row.Status @] for</div><div class="stat-value text-xl">[@ row['Minutes in status'] | number(0) @] min</div>
                            <div class="stat-desc"><span class="badge badge-sm [@ row.Rag == 'R' ? 'badge-error' : row.Rag == 'A' ? 'badge-warning' : row.Rag == 'G' ? 'badge-success' : 'badge-neutral' @]">[@ row['Dwell limit'] == null ? 'no dwell limit' : 'limit ' + row['Dwell limit'] + ' min' @]</span></div></div>
                          <div class="stat"><div class="stat-title">Escalates on</div><div class="stat-value text-base">[@ row['Escalates on'] @]</div>
                            <div class="stat-desc">[@ row['Escalates to'] == null ? '' : 'to ' + row['Escalates to'] @] [@ row.Threshold == null ? '' : '(threshold ' + row.Threshold + ')' @]</div></div>
                        </div>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Status', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Minutes in status', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Dwell limit', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Escalates on', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Threshold', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Escalates to', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Rag', 1, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Queue position', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH me AS (SELECT c."CaseWorkflowGuid" AS wf, c."CaseWorkflowStatusGuid" AS st, c."RiskPriority" AS pr, c."CreatedDate" AS cd FROM "Case" c WHERE c."Id" = @Id)
                        SELECT count(*) AS "Open in this status", count(*) FILTER (WHERE coalesce(p."RiskPriority", 0) > coalesce(me.pr, 0) OR (coalesce(p."RiskPriority", 0) = coalesce(me.pr, 0) AND p."CreatedDate" < me.cd)) + 1 AS "Position",
                               round(extract(epoch FROM ((now() at time zone 'utc') - min(p."CreatedDate"))) / 3600)::integer AS "Oldest hours", count(*) FILTER (WHERE p."LockedUser" IS NULL) AS "Unassigned"
                        FROM me JOIN "Case" p ON p."CaseWorkflowGuid" = me.wf AND p."CaseWorkflowStatusGuid" = me.st AND p."ClosedStatusId" <> 3$cmd$, 50, 0, 6, 1, v_datasource_guid, 29, $tpl$<div class="stats bg-base-200 rounded-box w-full" v-for="row in rows">
                          <div class="stat"><div class="stat-title">Open in this status</div><div class="stat-value text-xl">[@ row['Open in this status'] @]</div><div class="stat-desc">[@ row.Unassigned @] unassigned</div></div>
                          <div class="stat"><div class="stat-title">Position by risk</div><div class="stat-value text-xl">[@ row.Position @]</div><div class="stat-desc">oldest waiting [@ row['Oldest hours'] @] h</div></div>
                        </div>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Open in this status', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Position', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Oldest hours', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Unassigned', 2, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rule that opened the case', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT r."Name" AS "Rule", r."ResponseElevation" AS "Response elevation", r."Version" AS "Version", CASE WHEN r."Shadow" = 1 THEN 'Shadow' WHEN r."Active" = 1 THEN 'Live' ELSE 'Inactive' END AS "State",
                               ap."CreatedUser" AS "Approved by", ap."CreatedDate" AS "Approved on", r."ResponseElevationContent" AS "Advice"
                        FROM "Case" c JOIN "EntityAnalysisModelActivationRule" r ON r."Guid" = c."EntityAnalysisModelActivationRuleGuid"
                        LEFT JOIN LATERAL (SELECT e."CreatedUser", e."CreatedDate" FROM "EntityApproval" e WHERE e."EntityApprovalKindId" = 16 AND e."EntityId" = r."Id" AND e."StateId" = 1 ORDER BY e."CreatedDate" DESC LIMIT 1) ap ON true
                        WHERE c."Id" = @Id$cmd$, 60, 0, 6, 2, v_datasource_guid, 29, $tpl$<dl class="grid grid-cols-2 gap-x-4 gap-y-1" v-for="row in rows">
                          <dt class="text-xs uppercase">Rule</dt><dd class="font-semibold">[@ row.Rule @] <span class="badge badge-sm [@ row.State == 'Live' ? 'badge-success' : row.State == 'Shadow' ? 'badge-info' : 'badge-neutral' @]">[@ row.State @]</span></dd>
                          <dt class="text-xs uppercase">Version</dt><dd>[@ row.Version @]</dd>
                          <dt class="text-xs uppercase">Response elevation</dt><dd>[@ row['Response elevation'] @]</dd>
                          <dt class="text-xs uppercase">Approved</dt><dd>[@ row['Approved by'] | default('not approved') @] [@ row['Approved on'] | date @]</dd>
                          <dt class="text-xs uppercase">Advice</dt><dd>[@ row.Advice | default('none') @]</dd>
                        </dl>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Response elevation', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 3, NULL, NULL),
                                (v_datasource_id, 'State', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Approved by', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Approved on', 4, 6, NULL, NULL),
                                (v_datasource_id, 'Advice', 1, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Transaction that opened the case', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT c."Json"->'payload'->>'TxnId' AS "Transaction", c."Json"->'payload'->>'AmountEUR' AS "Amount", c."Json"->'payload'->>'BillingCountry' AS "Country", c."Json"->'payload'->>'ChannelId' AS "Channel",
                               c."Json"->'responseElevation'->>'value' AS "Response elevation",
                               (SELECT string_agg(k, ', ' ORDER BY k) FROM jsonb_object_keys(COALESCE(c."Json"->'activation', '{}'::jsonb)) AS k) AS "Rules fired",
                               (SELECT string_agg(t, ', ' ORDER BY t) FROM jsonb_array_elements_text(CASE WHEN jsonb_typeof(c."Json"->'tag') = 'array' THEN c."Json"->'tag' ELSE '[]'::jsonb END) AS t) AS "Tags",
                               (SELECT string_agg(j.key || ' (distance ' || j.value || ')', ', ') FROM jsonb_each_text(COALESCE(c."Json"->'sanction', '{}'::jsonb)) AS j) AS "Sanction matches"
                        FROM "Case" c WHERE c."Id" = @Id$cmd$, 70, 0, 6, 2, v_datasource_guid, 29, $tpl$<dl class="grid grid-cols-2 gap-x-4 gap-y-1" v-for="row in rows">
                          <dt class="text-xs uppercase">Transaction</dt><dd class="font-mono">[@ row.Transaction @]</dd>
                          <dt class="text-xs uppercase">Amount</dt><dd>[@ row.Amount @] [@ row.Country @] [@ row.Channel @]</dd>
                          <dt class="text-xs uppercase">Response elevation</dt><dd>[@ row['Response elevation'] @]</dd>
                          <dt class="text-xs uppercase">Rules fired</dt><dd>[@ row['Rules fired'] | default('none') @]</dd>
                          <dt class="text-xs uppercase">Tags</dt><dd>[@ row.Tags | default('none') @]</dd>
                          <dt class="text-xs uppercase">Sanctions</dt><dd>[@ row['Sanction matches'] | default('no match') @]</dd>
                        </dl>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Transaction', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Amount', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Country', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Channel', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Response elevation', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Rules fired', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Tags', 1, 7, NULL, NULL),
                                (v_datasource_id, 'Sanction matches', 1, 8, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'What happened to the case', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT e."CreatedDate" AS "When", CASE e."CaseEventTypeId" WHEN 1 THEN 'Automatic unlock' WHEN 2 THEN 'Skim' WHEN 3 THEN 'Automatic lock' WHEN 4 THEN 'Full open' WHEN 5 THEN 'Closed' WHEN 6 THEN 'Manual lock' WHEN 7 THEN 'Diary'
                            WHEN 8 THEN 'Workflow change' WHEN 9 THEN 'Status change' WHEN 10 THEN 'Diary date change' WHEN 11 THEN 'Rating change' WHEN 12 THEN 'Suspend closed' WHEN 13 THEN 'Suspend open' WHEN 14 THEN 'Allocation'
                            WHEN 15 THEN 'Expired case processed' WHEN 16 THEN 'Peek' WHEN 17 THEN 'Automatic escalation' WHEN 18 THEN 'Lock broken' WHEN 19 THEN 'Diary cleared' WHEN 20 THEN 'Quality sample'
                            WHEN 21 THEN 'Checklist shown' WHEN 22 THEN 'Checklist hidden' WHEN 23 THEN 'Checklist item checked' WHEN 24 THEN 'Checklist item unchecked' WHEN 25 THEN 'Gate blocked' WHEN 26 THEN 'Gate blocked'
                            WHEN 27 THEN 'Gate overridden' WHEN 28 THEN 'SLA breached' WHEN 29 THEN 'Triage recommended' WHEN 30 THEN 'Triage accepted' WHEN 31 THEN 'Triage overridden' WHEN 32 THEN 'Legal hold placed'
                            WHEN 33 THEN 'Legal hold lifted' WHEN 34 THEN 'Assessment recorded' WHEN 35 THEN 'Assessment withdrawn' WHEN 36 THEN 'Case linked' WHEN 37 THEN 'Case unlinked' ELSE 'Other' END AS "Activity", coalesce(e."CreatedUser", 'System') AS "By", e."Before" AS "Before", e."After" AS "After"
                        FROM "CaseEvent" e WHERE e."CaseId" = @Id AND e."CaseEventTypeId" <> 16 ORDER BY e."CreatedDate" DESC LIMIT 30$cmd$, 80, 0, 6, 4, v_datasource_guid, 29, $tpl$<ul class="list">
                          <li class="list-row" v-for="row in rows">
                            <div class="tabular-nums text-xs whitespace-nowrap">[@ row.When | dateTime @]</div>
                            <div><span class="font-semibold">[@ row.Activity @]</span> <span class="text-xs">[@ row.By @]</span></div>
                          </li>
                        </ul>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'When', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Activity', 1, 2, NULL, NULL),
                                (v_datasource_id, 'By', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Before', 1, 4, NULL, NULL),
                                (v_datasource_id, 'After', 1, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Notes', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT n."CreatedDate" AS "When", n."CreatedUser" AS "By", CASE n."KindId" WHEN 2 THEN 'Narrative' WHEN 4 THEN 'Document' ELSE 'Note' END AS "Kind", n."Note" AS "Note", n."IsFinding" AS "Finding"
                        FROM "CaseNote" n WHERE n."CaseId" = @Id ORDER BY n."CreatedDate" DESC LIMIT 20$cmd$, 90, 0, 6, 2, v_datasource_guid, 29, $tpl$<ul class="list">
                          <li class="list-row" v-for="row in rows">
                            <div class="text-xs whitespace-nowrap">[@ row.When | dateTime @]<br/>[@ row.By @]</div>
                            <div><span class="badge badge-sm badge-outline">[@ row.Kind @]</span> [@ row.Note | truncate(240) @]</div>
                          </li>
                        </ul>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'When', 4, 1, NULL, NULL),
                                (v_datasource_id, 'By', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Kind', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Note', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Finding', 5, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'History of this key', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH k0 AS (SELECT c."CaseKey" AS key, c."CaseKeyValue" AS val FROM "Case" c WHERE c."Id" = @Id)
                        SELECT to_char(date_trunc('month', a."CreatedDate"), 'YYYY-MM') AS "Month", count(*) AS "Transactions", count(*) FILTER (WHERE a."ActivationRuleCount" > 0) AS "Alerts"
                        FROM k0 JOIN "ArchiveKey" k ON k."ProcessingTypeId" = 1 AND k."Key" = k0.key AND k."KeyValueString" = k0.val
                        JOIN "Archive" a ON a."EntityAnalysisModelInstanceEntryGuid" = k."EntityAnalysisModelInstanceEntryGuid"
                        WHERE a."CreatedDate" >= date_trunc('month', (now() at time zone 'utc') - interval '11 months') GROUP BY date_trunc('month', a."CreatedDate") ORDER BY date_trunc('month', a."CreatedDate")$cmd$, 100, 0, 6, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Transactions', 2, 2, 2, 1),
                                (v_datasource_id, 'Alerts', 2, 3, 2, 2);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Fields shown by the workflow', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT x."Name" AS "Field", c."Json"->'payload'->>x."Name" AS "Value"
                        FROM "Case" c JOIN "CaseWorkflow" w ON w."Guid" = c."CaseWorkflowGuid" JOIN "CaseWorkflowXPath" x ON x."CaseWorkflowId" = w."Id" AND (x."Deleted" = 0 OR x."Deleted" IS NULL) AND x."Active" = 1
                        WHERE c."Id" = @Id AND coalesce(c."Json"->'payload'->>x."Name", '') <> '' ORDER BY x."Name" LIMIT 14$cmd$, 110, 0, 6, 3, v_datasource_guid, 29, $tpl$<dl class="grid grid-cols-2 gap-x-4 gap-y-1">
                          <template v-for="row in rows"><dt class="text-xs uppercase">[@ row.Field @]</dt><dd>[@ row.Value | default('empty') @]</dd></template>
                        </dl>$tpl$)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Field', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Value', 1, 2, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Other cases for this key', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT o."Id" AS "Case", coalesce(st."Name", 'Unknown') AS "Status", o."CreatedDate" AS "Opened", CASE WHEN o."ClosedStatusId" = 3 THEN o."ClosedDate" END AS "Closed",
                               CASE o."ClosedReasonId" WHEN 1 THEN 'False positive: known activity' WHEN 2 THEN 'False positive: rule too broad' WHEN 3 THEN 'Duplicate' WHEN 4 THEN 'Insufficient evidence' WHEN 5 THEN 'Reported' WHEN 6 THEN 'Fraud confirmed' WHEN 7 THEN 'Customer explained' WHEN 8 THEN 'Other' ELSE '' END AS "Outcome"
                        FROM "Case" c JOIN "Case" o ON o."CaseKey" = c."CaseKey" AND o."CaseKeyValue" = c."CaseKeyValue" AND o."Id" <> c."Id"
                        LEFT JOIN "CaseWorkflowStatus" st ON st."Guid" = o."CaseWorkflowStatusGuid" AND (st."Deleted" = 0 OR st."Deleted" IS NULL)
                        WHERE c."Id" = @Id ORDER BY o."CreatedDate" DESC LIMIT 100$cmd$, 120, 1, 8, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Case', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Status', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Opened', 4, 3, NULL, NULL),
                                (v_datasource_id, 'Closed', 4, 4, NULL, NULL),
                                (v_datasource_id, 'Outcome', 1, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Network signals for this key', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT n."Degree" AS "Connections", n."ComponentSize" AS "Component size", round(n."PageRank"::numeric, 6) AS "PageRank", n."DistanceToConfirmed" AS "Distance to a confirmed case", n."ConfirmedWithin2" AS "Confirmed within 2 hops"
                        FROM "Case" c JOIN "GraphNode" n ON n."Key" = c."CaseKey" AND n."Value" = c."CaseKeyValue" WHERE c."Id" = @Id LIMIT 1$cmd$, 130, 1, 4, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Connections', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Component size', 2, 2, NULL, NULL),
                                (v_datasource_id, 'PageRank', 3, 3, NULL, NULL),
                                (v_datasource_id, 'Distance to a confirmed case', 5, 4, NULL, NULL),
                                (v_datasource_id, 'Confirmed within 2 hops', 2, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Suppressions on this case', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT s."Id" AS "Request", CASE s."StateId" WHEN 1 THEN 'Pending' WHEN 2 THEN 'Approved' WHEN 3 THEN 'Rejected' ELSE 'Retracted' END AS "State", s."OverrideKey" AS "Key", s."OverrideKeyValue" AS "Value",
                               s."CreatedUser" AS "Requested by", s."DecidedUser" AS "Decided by", s."ExpiryDate" AS "Expires", s."Reason" AS "Reason"
                        FROM "CaseSuppression" s WHERE s."CaseId" = @Id ORDER BY s."CreatedDate" DESC$cmd$, 140, 1, 12, 2, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Request', 2, 1, NULL, NULL),
                                (v_datasource_id, 'State', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Value', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Requested by', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Decided by', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Expires', 4, 7, NULL, NULL),
                                (v_datasource_id, 'Reason', 1, 8, NULL, NULL);
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
