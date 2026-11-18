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

    [Migration(2026111792000007)]
    public class SeedCasesRegisters : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Cases: Registers' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Cases: Registers', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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
                            VALUES (v_registry_id, 'Open cases by status', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH cs AS (
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
                        SELECT coalesce(cs.status_name, 'Unknown') AS "Status", count(*) AS "Cases" FROM cs WHERE NOT cs.closed GROUP BY 1 ORDER BY 2 DESC$cmd$, 10, 0, 4, 3, v_datasource_guid, 12, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Status', 1, 1, 1, 1),
                                (v_datasource_id, 'Cases', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Open cases by risk lane', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT CASE c."RiskLaneId" WHEN 1 THEN 'Live money' WHEN 2 THEN 'Mandatory' WHEN 3 THEN 'Deadline near' ELSE 'Standard' END AS "Lane", count(*) AS "Cases"
                        FROM "Case" c WHERE c."ClosedStatusId" <> 3 GROUP BY 1 ORDER BY 2 DESC$cmd$, 20, 0, 4, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Lane', 1, 1, 1, 1),
                                (v_datasource_id, 'Cases', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rating of open cases', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT CASE WHEN c."Rating" = 0 THEN 'Unrated' ELSE 'Rating ' || c."Rating" END AS "Rating", count(*) AS "Cases" FROM "Case" c WHERE c."ClosedStatusId" <> 3 GROUP BY 1 ORDER BY 1$cmd$, 30, 0, 4, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rating', 1, 1, 1, 1),
                                (v_datasource_id, 'Cases', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Cases opened and closed by day', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
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
                        d AS (SELECT generate_series(date_trunc('day', (SELECT f FROM w)), date_trunc('day', (SELECT t FROM w)), interval '1 day') AS ds)
                        SELECT to_char(d.ds, 'YYYY-MM-DD') AS "Day", (SELECT count(*) FROM cs WHERE cs."CreatedDate" >= d.ds AND cs."CreatedDate" < d.ds + interval '1 day') AS "Opened",
                               (SELECT count(*) FROM cs WHERE cs.closed AND cs."ClosedDate" >= d.ds AND cs."ClosedDate" < d.ds + interval '1 day') AS "Closed" FROM d ORDER BY d.ds$cmd$, 40, 0, 8, 3, v_datasource_guid, 16, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Opened', 2, 2, 2, 1),
                                (v_datasource_id, 'Closed', 2, 3, 2, 2);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Cases by originating rule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
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
                        SELECT coalesce(rl."Name", 'Unknown') AS "Rule", count(*) AS "Cases" FROM cs CROSS JOIN w LEFT JOIN rl ON rl."Guid" = cs.rule_guid WHERE cs."CreatedDate" >= w.f AND cs."CreatedDate" < w.t GROUP BY 1 ORDER BY 2 DESC$cmd$, 50, 0, 4, 3, v_datasource_guid, 26, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, 1, 1),
                                (v_datasource_id, 'Cases', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Open case register', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH cs AS (
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
                        SELECT c."Id" AS "Case", c."CaseKeyValue" AS "Key", coalesce(cs.status_name, 'Unknown') AS "Status", c."CreatedDate" AS "Opened",
                               round(extract(epoch FROM ((now() at time zone 'utc') - c."CreatedDate")) / 3600)::integer AS "Age hours", coalesce(c."LockedUser", 'Unassigned') AS "Assignee",
                               CASE c."RiskLaneId" WHEN 1 THEN 'Live money' WHEN 2 THEN 'Mandatory' WHEN 3 THEN 'Deadline near' ELSE 'Standard' END AS "Lane", c."RiskPriority" AS "Priority", c."Rating" AS "Rating",
                               c."RiskNextDeadlineDate" AS "Deadline", CASE WHEN c."LegalHold" = 1 THEN 'Yes' ELSE 'No' END AS "Legal hold"
                        FROM "Case" c JOIN cs ON cs."Id" = c."Id" WHERE c."ClosedStatusId" <> 3 ORDER BY c."RiskPriority" DESC NULLS LAST, c."CreatedDate" LIMIT 1000$cmd$, 60, 1, 12, 4, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Case', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Status', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Opened', 4, 4, NULL, NULL),
                                (v_datasource_id, 'Age hours', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Assignee', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Lane', 1, 7, NULL, NULL),
                                (v_datasource_id, 'Priority', 2, 8, NULL, NULL),
                                (v_datasource_id, 'Rating', 5, 9, NULL, NULL),
                                (v_datasource_id, 'Deadline', 4, 10, NULL, NULL),
                                (v_datasource_id, 'Legal hold', 1, 11, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Closed case register', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
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
                        SELECT cs."Id" AS "Case", cs."CaseKeyValue" AS "Key", coalesce(rl."Name", 'Unknown') AS "Rule", cs."CreatedDate" AS "Opened", cs."ClosedDate" AS "Closed", cs."ClosedUser" AS "Closed by",
                               round(extract(epoch FROM (cs."ClosedDate" - cs."CreatedDate")) / 3600)::integer AS "Hours open",
                               CASE cs."ClosedReasonId" WHEN 1 THEN 'False positive: known activity' WHEN 2 THEN 'False positive: rule too broad' WHEN 3 THEN 'Duplicate' WHEN 4 THEN 'Insufficient evidence' WHEN 5 THEN 'Suspicion confirmed and reported'
                                    WHEN 6 THEN 'Fraud confirmed' WHEN 7 THEN 'Customer explained' WHEN 8 THEN 'Other' ELSE 'No reason recorded' END AS "Reason", cs.status_name AS "Closing status"
                        FROM cs CROSS JOIN w LEFT JOIN rl ON rl."Guid" = cs.rule_guid WHERE cs.closed AND cs."ClosedDate" >= w.f AND cs."ClosedDate" < w.t ORDER BY cs."ClosedDate" DESC LIMIT 1000$cmd$, 70, 1, 12, 4, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Case', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Rule', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Opened', 4, 4, NULL, NULL),
                                (v_datasource_id, 'Closed', 4, 5, NULL, NULL),
                                (v_datasource_id, 'Closed by', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Hours open', 2, 7, NULL, NULL),
                                (v_datasource_id, 'Reason', 1, 8, NULL, NULL),
                                (v_datasource_id, 'Closing status', 1, 9, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Diary entries due', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT c."Id" AS "Case", c."CaseKeyValue" AS "Key", CASE c."DiaryTypeId" WHEN 1 THEN 'Review' WHEN 2 THEN 'Customer response' WHEN 3 THEN 'Regulatory deadline' WHEN 4 THEN 'Payment hold' ELSE 'Other' END AS "Diary",
                               c."DiaryDate" AS "Due", c."DiaryUser" AS "Set by" FROM "Case" c WHERE c."Diary" = 1 AND c."ClosedStatusId" <> 3 ORDER BY c."DiaryDate" LIMIT 500$cmd$, 80, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Case', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Diary', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Due', 4, 4, NULL, NULL),
                                (v_datasource_id, 'Set by', 1, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Payment holds in force', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT c."Id" AS "Case", c."CaseKeyValue" AS "Key", c."DiaryDate" AS "Review by", c."DiaryUser" AS "Held by", c."CreatedDate" AS "Case opened"
                        FROM "Case" c WHERE c."Diary" = 1 AND c."DiaryTypeId" = 4 AND c."ClosedStatusId" <> 3 ORDER BY c."DiaryDate" LIMIT 500$cmd$, 90, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Case', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Review by', 4, 3, NULL, NULL),
                                (v_datasource_id, 'Held by', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Case opened', 4, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Legal hold register', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT c."Id" AS "Case", c."CaseKeyValue" AS "Key", c."LegalHoldDate" AS "Placed", c."LegalHoldUser" AS "Placed by", c."LegalHoldReason" AS "Reason", CASE WHEN c."ClosedStatusId" = 3 THEN 'Closed' ELSE 'Open' END AS "Case state"
                        FROM "Case" c WHERE c."LegalHold" = 1 ORDER BY c."LegalHoldDate" DESC LIMIT 500$cmd$, 100, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Case', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Placed', 4, 3, NULL, NULL),
                                (v_datasource_id, 'Placed by', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Reason', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Case state', 1, 6, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Quality sample register', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT c."Id" AS "Case", c."CaseKeyValue" AS "Key", c."QaSampleDate" AS "Sampled", c."ClosedUser" AS "Closed by", c."ClosedDate" AS "Closed"
                        FROM "Case" c CROSS JOIN w WHERE c."QaSampleStatusId" = 2 AND c."QaSampleDate" >= w.f AND c."QaSampleDate" < w.t ORDER BY c."QaSampleDate" DESC LIMIT 500$cmd$, 110, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Case', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Sampled', 4, 3, NULL, NULL),
                                (v_datasource_id, 'Closed by', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Closed', 4, 5, NULL, NULL);
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
