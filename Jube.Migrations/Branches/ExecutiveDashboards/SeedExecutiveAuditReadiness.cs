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

    [Migration(2026111791000005)]
    public class SeedExecutiveAuditReadiness : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Executive: Audit Readiness' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Executive: Audit Readiness', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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
                            VALUES (v_registry_id, 'Readiness checks', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH cs AS (
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
                        silent AS (
                            SELECT count(*) AS n FROM "Archive" a
                            JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = a."EntityAnalysisModelActivationRuleId" AND r."EnableCaseWorkflow" = 1 AND (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= (now() at time zone 'utc') - interval '90 days'
                              AND NOT EXISTS (SELECT 1 FROM "Case" c WHERE c."EntityAnalysisModelInstanceEntryGuid" = a."EntityAnalysisModelInstanceEntryGuid")
                        ),
                        noreason AS (SELECT count(*) AS n FROM cs WHERE cs.closed AND cs."ClosedReasonId" IS NULL),
                        nonote AS (SELECT count(*) AS n FROM cs WHERE cs.closed AND NOT EXISTS (SELECT 1 FROM "CaseNote" n WHERE n."CaseId" = cs."Id")),
                        auto AS (SELECT count(*) AS n FROM cs WHERE cs.closed AND cs."ClosedUser" IN ('Automation', 'System')),
                        pend AS (
                            SELECT count(*) AS n FROM "EntityAnalysisModelActivationRule" r
                            WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL) AND r."Active" = 1
                              AND NOT EXISTS (SELECT 1 FROM "EntityApproval" e WHERE e."EntityApprovalKindId" = 16 AND e."EntityId" = r."Id" AND e."EntityVersion" = r."Version" AND e."StateId" = 1)
                        ),
                        self AS (
                            SELECT count(*) AS n FROM "EntityApproval" e
                            JOIN (SELECT r."Id" AS id, r."Version" AS version, r."CreatedUser" AS maker FROM "EntityAnalysisModelActivationRule" r
                                  UNION ALL SELECT v."EntityAnalysisModelActivationRuleId", v."Version", v."CreatedUser" FROM "EntityAnalysisModelActivationRuleVersion" v) rv
                              ON e."EntityApprovalKindId" = 16 AND rv.id = e."EntityId" AND rv.version = e."EntityVersion" AND rv.maker = e."CreatedUser"
                        ),
                        kpi AS (
                            SELECT 1 AS ord, 'Alerts from case-opening rules with no case' AS label, silent.n::numeric AS cur, NULL::numeric AS prior, 'n' AS kind, CASE WHEN silent.n = 0 THEN 'G' ELSE 'R' END AS rag, 'last 90 days; should be zero' AS note FROM silent
                            UNION ALL SELECT 2, 'Closed cases with no close reason', noreason.n::numeric, NULL, 'n', CASE WHEN noreason.n = 0 THEN 'G' WHEN noreason.n > 5 THEN 'R' ELSE 'A' END, 'should be zero' FROM noreason
                            UNION ALL SELECT 3, 'Closed cases with no note', nonote.n::numeric, NULL, 'n', CASE WHEN nonote.n = 0 THEN 'G' WHEN nonote.n > 50 THEN 'R' ELSE 'A' END, 'no rationale recorded' FROM nonote
                            UNION ALL SELECT 4, 'Cases closed by automation', auto.n::numeric, NULL, 'n', CASE WHEN auto.n = 0 THEN 'G' ELSE 'A' END, 'closed without an analyst' FROM auto
                            UNION ALL SELECT 5, 'Live rules without an approval', pend.n::numeric, NULL, 'n', CASE WHEN pend.n = 0 THEN 'G' ELSE 'R' END, 'current version not signed off' FROM pend
                            UNION ALL SELECT 6, 'Rule changes approved by their maker', self.n::numeric, NULL, 'n', CASE WHEN self.n = 0 THEN 'G' ELSE 'R' END, 'separation of duties; should be zero' FROM self
                            UNION ALL SELECT 7, 'Days of transactions archived', extract(epoch FROM (max(a."CreatedDate") - min(a."CreatedDate"))) / 86400, NULL, 'n', 'N', 'oldest to newest' FROM "Archive" a
                            UNION ALL SELECT 8, 'Locks broken', count(*)::numeric, NULL, 'n', 'N', 'a supervisor took a case from its holder' FROM "CaseEvent" e WHERE e."CaseEventTypeId" = 18
                        )
                        SELECT k.ord AS "Ord", k.label AS "Label",
                               coalesce(CASE k.kind WHEN 'p' THEN to_char(k.cur * 100, 'FM990.0') || '%' WHEN 'd' THEN to_char(k.cur, 'FM990.0') WHEN 'q' THEN to_char(k.cur, 'FM990.000') WHEN 'm' THEN to_char(k.cur, 'FM999,999,990.00') ELSE to_char(k.cur, 'FM999,999,990') END, '—') AS "Display",
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
                            VALUES (v_registry_id, 'Alerts and the cases opened from them', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT coalesce(r."Name", 'Unknown') AS "Rule", CASE WHEN r."EnableCaseWorkflow" = 1 THEN 'Opens cases' ELSE 'Does not open cases' END AS "Configured",
                               count(*) AS "Alerts", count(c."Id") AS "Alerts with a case", count(*) - count(c."Id") AS "Alerts without a case",
                               CASE WHEN r."EnableCaseWorkflow" = 1 AND count(*) > count(c."Id") THEN 'Investigate' WHEN r."EnableCaseWorkflow" = 1 THEN 'Complete' ELSE 'By design' END AS "Finding"
                        FROM "Archive" a
                        JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = a."EntityAnalysisModelActivationRuleId"
                        LEFT JOIN "Case" c ON c."EntityAnalysisModelInstanceEntryGuid" = a."EntityAnalysisModelInstanceEntryGuid"
                        WHERE a."ActivationRuleCount" > 0 AND a."CreatedDate" >= (now() at time zone 'utc') - interval '90 days'
                        GROUP BY r."Name", r."EnableCaseWorkflow" ORDER BY 5 DESC, 1$cmd$, 20, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Configured', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Alerts', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Alerts with a case', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Alerts without a case', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Finding', 1, 6, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rules awaiting approval', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT r."Name" AS "Rule", CASE WHEN r."Shadow" = 1 THEN 'Shadow' WHEN r."Active" = 1 THEN 'Live' ELSE 'Inactive' END AS "State", r."Version" AS "Version", r."CreatedUser" AS "Made by",
                               round(extract(epoch FROM ((now() at time zone 'utc') - r."CreatedDate")) / 86400)::integer AS "Days since created",
                               (SELECT count(*) FROM "EntityApproval" e WHERE e."EntityApprovalKindId" = 16 AND e."EntityId" = r."Id" AND e."EntityVersion" = r."Version" AND e."StateId" = 2) AS "Rejections of this version"
                        FROM "EntityAnalysisModelActivationRule" r
                        WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                          AND NOT EXISTS (SELECT 1 FROM "EntityApproval" e WHERE e."EntityApprovalKindId" = 16 AND e."EntityId" = r."Id" AND e."EntityVersion" = r."Version" AND e."StateId" = 1)
                        ORDER BY r."Active" DESC, r."Name"$cmd$, 30, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'State', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Made by', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Days since created', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Rejections of this version', 2, 6, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Overrides in force', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT CASE o."OverrideKind" WHEN 1 THEN 'Force' ELSE 'Suppress' END AS "Effect", o."EntityAnalysisModelActivationRuleName" AS "Rule", o."OverrideKey" AS "Key", o."OverrideKeyValue" AS "Value",
                               o."CreatedUser" AS "Requested by", o."CreatedDate" AS "Requested", o."DeleteExpiryDate" AS "Expires",
                               (SELECT e."CreatedUser" FROM "EntityApproval" e WHERE e."EntityApprovalKindId" = 23 AND e."EntityId" = o."Id" AND e."StateId" = 1 ORDER BY e."CreatedDate" DESC LIMIT 1) AS "Approved by"
                        FROM "EntityAnalysisModelActivationRuleOverride" o
                        WHERE (o."Deleted" = 0 OR o."Deleted" IS NULL) AND (o."DeleteExpiryDate" IS NULL OR o."DeleteExpiryDate" > (now() at time zone 'utc')) ORDER BY o."DeleteExpiryDate"$cmd$, 40, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Effect', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Rule', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Value', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Requested by', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Requested', 4, 6, NULL, NULL),
                                (v_datasource_id, 'Expires', 4, 7, NULL, NULL),
                                (v_datasource_id, 'Approved by', 1, 8, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Configuration change log', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        rn AS (SELECT r."Id" AS id, r."Name" AS name FROM "EntityAnalysisModelActivationRule" r)
                        SELECT e."CreatedDate" AS "Decided", CASE e."EntityApprovalKindId" WHEN 1 THEN 'Model' WHEN 2 THEN 'List' WHEN 3 THEN 'List value' WHEN 4 THEN 'Dictionary' WHEN 5 THEN 'Dictionary value' WHEN 6 THEN 'Request field'
                                   WHEN 7 THEN 'Inline script' WHEN 8 THEN 'Inline function' WHEN 9 THEN 'Gateway rule' WHEN 10 THEN 'Sanction' WHEN 11 THEN 'Abstraction rule' WHEN 12 THEN 'Abstraction calculation'
                                   WHEN 13 THEN 'TTL counter' WHEN 14 THEN 'HTTP adaptation' WHEN 15 THEN 'Exhaustive model' WHEN 16 THEN 'Activation rule' WHEN 17 THEN 'Tag' WHEN 18 THEN 'Advanced analytics instance'
                                   WHEN 19 THEN 'Response elevation range' WHEN 20 THEN 'Graph recall' WHEN 21 THEN 'Svirfd' WHEN 22 THEN 'Override' WHEN 23 THEN 'Rule override' ELSE 'Other' END AS "Kind",
                               coalesce(rn.name, 'Id ' || e."EntityId") AS "Entity", e."EntityVersion" AS "Version", CASE e."StateId" WHEN 1 THEN 'Approved' ELSE 'Rejected' END AS "Decision",
                               e."CreatedUser" AS "Decided by", e."Note" AS "Note", e."ReturnedDate" AS "Returned", e."ReturnedUser" AS "Returned by"
                        FROM "EntityApproval" e CROSS JOIN w LEFT JOIN rn ON e."EntityApprovalKindId" = 16 AND rn.id = e."EntityId"
                        WHERE e."CreatedDate" >= w.f AND e."CreatedDate" < w.t ORDER BY e."CreatedDate" DESC LIMIT 1000$cmd$, 50, 1, 12, 4, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Decided', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Kind', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Entity', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Decision', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Decided by', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Note', 1, 7, NULL, NULL),
                                (v_datasource_id, 'Returned', 4, 8, NULL, NULL),
                                (v_datasource_id, 'Returned by', 1, 9, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Case suppression requests', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT s."Id" AS "Request", s."CaseKeyValue" AS "Key", CASE s."StateId" WHEN 1 THEN 'Pending' WHEN 2 THEN 'Approved' WHEN 3 THEN 'Rejected' ELSE 'Retracted' END AS "State",
                               s."CreatedUser" AS "Requested by", s."CreatedDate" AS "Requested", s."DecidedUser" AS "Decided by", s."DecidedDate" AS "Decided", s."ExpiryDate" AS "Expires", s."Reason" AS "Reason"
                        FROM "CaseSuppression" s ORDER BY s."CreatedDate" DESC LIMIT 500$cmd$, 60, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Request', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'State', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Requested by', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Requested', 4, 5, NULL, NULL),
                                (v_datasource_id, 'Decided by', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Decided', 4, 7, NULL, NULL),
                                (v_datasource_id, 'Expires', 4, 8, NULL, NULL),
                                (v_datasource_id, 'Reason', 1, 9, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Legal holds placed and lifted', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT e."CreatedDate" AS "When", CASE e."CaseEventTypeId" WHEN 32 THEN 'Placed' ELSE 'Lifted' END AS "Hold", e."CaseId" AS "Case", e."CaseKeyValue" AS "Key", e."CreatedUser" AS "By", e."After" AS "Reason"
                        FROM "CaseEvent" e WHERE e."CaseEventTypeId" IN (32, 33) ORDER BY e."CreatedDate" DESC LIMIT 500$cmd$, 70, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'When', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Hold', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Case', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 4, NULL, NULL),
                                (v_datasource_id, 'By', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Reason', 1, 6, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Cases sampled for quality review by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH cs AS (
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
                        SELECT to_char(date_trunc('month', cs."ClosedDate"), 'YYYY-MM') AS "Month", count(*) AS "Closed",
                               count(*) FILTER (WHERE EXISTS (SELECT 1 FROM "Case" c WHERE c."Id" = cs."Id" AND c."QaSampleStatusId" = 2)) AS "Sampled for review",
                               round(100.0 * count(*) FILTER (WHERE EXISTS (SELECT 1 FROM "Case" c WHERE c."Id" = cs."Id" AND c."QaSampleStatusId" = 2)) / count(*), 1) AS "Sampled %"
                        FROM cs WHERE cs.closed AND cs."ClosedDate" >= date_trunc('month', (now() at time zone 'utc') - interval '11 months')
                        GROUP BY date_trunc('month', cs."ClosedDate") ORDER BY date_trunc('month', cs."ClosedDate")$cmd$, 80, 1, 6, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Closed', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Sampled for review', 2, 3, 2, 1),
                                (v_datasource_id, 'Sampled %', 3, 4, NULL, NULL);
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
