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

namespace Jube.Migrations.Branches.SmokeTesting
{
    using FluentMigrator;

    [Migration(2026111794000000)]
    public class SeedSmokeTestConfiguration : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_model integer;
                            v_tenant integer;
                            v_role uuid;
                            v_wf1_id integer;
                            v_wf1 uuid;
                            v_wf2_id integer;
                            v_wf2 uuid := gen_random_uuid();
                            v_wf3_id integer;
                            v_wf3 uuid := gen_random_uuid();
                            v_analyst_id integer;
                            v_analyst uuid := gen_random_uuid();
                            v_supervisor_id integer;
                            v_supervisor uuid := gen_random_uuid();
                            v_status uuid;
                            v_now timestamp := date_trunc('minute', now() at time zone 'utc');
                        BEGIN
                            SELECT m."Id" INTO v_model FROM "EntityAnalysisModel" m WHERE m."Guid" = '90c425fd-101a-420b-91d1-cb7a24a969cc' AND coalesce(m."Deleted", 0) = 0;
                            SELECT t."Id" INTO v_tenant FROM "TenantRegistry" t WHERE t."Id" = 1 AND coalesce(t."Deleted", 0) = 0;
                            SELECT r."Guid" INTO v_role FROM "RoleRegistry" r WHERE r."Id" = 1 AND coalesce(r."Deleted", 0) = 0;

                            IF v_model IS NULL OR v_tenant IS NULL OR v_role IS NULL THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "CaseWorkflow" WHERE "Name" = 'Smoke: Queue Volume' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            SELECT "Id", "Guid" INTO v_wf1_id, v_wf1 FROM "CaseWorkflow" WHERE "EntityAnalysisModelId" = v_model AND coalesce("Deleted", 0) = 0 AND "Name" <> 'Smoke: Queue Volume' ORDER BY "Id" LIMIT 1;

                            IF v_wf1 IS NULL THEN
                                RETURN;
                            END IF;

                            INSERT INTO "RoleRegistry" ("Name", "CreatedDate", "CreatedUser", "Active", "Locked", "Deleted", "Version", "TenantRegistryId", "Guid")
                            VALUES ('Smoke: Case Analyst', v_now, 'Administrator', 1, 0, 0, 1, v_tenant, v_analyst)
                            RETURNING "Id" INTO v_analyst_id;
                            INSERT INTO "RoleRegistryPermission" ("RoleRegistryId", "PermissionSpecificationId", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid")
                            SELECT v_analyst_id, p, 1, 0, v_now, 'Administrator', 0, 1, gen_random_uuid() FROM unnest(ARRAY[1,2]) AS p;

                            INSERT INTO "RoleRegistry" ("Name", "CreatedDate", "CreatedUser", "Active", "Locked", "Deleted", "Version", "TenantRegistryId", "Guid")
                            VALUES ('Smoke: Case Supervisor', v_now, 'Administrator', 1, 0, 0, 1, v_tenant, v_supervisor)
                            RETURNING "Id" INTO v_supervisor_id;
                            INSERT INTO "RoleRegistryPermission" ("RoleRegistryId", "PermissionSpecificationId", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid")
                            SELECT v_supervisor_id, p, 1, 0, v_now, 'Administrator', 0, 1, gen_random_uuid() FROM unnest(ARRAY[1,2,18,62,68,70,71,75,76,77,78,79]) AS p;

                            v_status := gen_random_uuid();
                            INSERT INTO "CaseWorkflowStatus" ("CaseWorkflowId", "Priority", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid", "OutcomeId", "EscalationReleaseLock")
                            VALUES (v_wf1_id, 6, 'Smoke: Admin Only', 1, 0, v_now, 'Administrator', 0, 1, v_status, 0, 0);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_role, v_now, 'Administrator', 0, 1);
                            v_status := gen_random_uuid();
                            INSERT INTO "CaseWorkflowStatus" ("CaseWorkflowId", "Priority", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid", "OutcomeId", "EscalationReleaseLock")
                            VALUES (v_wf1_id, 7, 'Smoke: Ungranted', 1, 0, v_now, 'Administrator', 0, 1, v_status, 0, 0);
                            v_status := gen_random_uuid();
                            INSERT INTO "CaseWorkflowStatus" ("CaseWorkflowId", "Priority", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid", "OutcomeId", "EscalationReleaseLock")
                            VALUES (v_wf1_id, 8, 'Smoke: Inactive', 0, 0, v_now, 'Administrator', 0, 1, v_status, 0, 0);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_role, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_analyst, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_supervisor, v_now, 'Administrator', 0, 1);
                            v_status := gen_random_uuid();
                            INSERT INTO "CaseWorkflowStatus" ("CaseWorkflowId", "Priority", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid", "OutcomeId", "EscalationReleaseLock")
                            VALUES (v_wf1_id, 9, 'Smoke: Deleted', 1, 0, v_now, 'Administrator', 1, 1, v_status, 0, 0);

                            INSERT INTO "CaseWorkflowRole" ("Guid", "CaseWorkflowGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_wf1, v_analyst, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), s."Guid", v_analyst, v_now, 'Administrator', 0, 1
                            FROM "CaseWorkflowStatus" s JOIN "CaseWorkflowStatusRole" g ON g."CaseWorkflowStatusGuid" = s."Guid" AND g."RoleRegistryGuid" = v_role AND coalesce(g."Deleted", 0) = 0
                            WHERE s."CaseWorkflowId" = v_wf1_id AND coalesce(s."Deleted", 0) = 0 AND s."Name" <> 'Smoke: Admin Only'
                              AND NOT EXISTS (SELECT 1 FROM "CaseWorkflowStatusRole" x WHERE x."CaseWorkflowStatusGuid" = s."Guid" AND x."RoleRegistryGuid" = v_analyst);
                            INSERT INTO "CaseWorkflowActionRole" ("Guid", "CaseWorkflowActionGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowActionGuid", v_analyst, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowActionRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;
                            INSERT INTO "CaseWorkflowFormRole" ("Guid", "CaseWorkflowFormGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowFormGuid", v_analyst, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowFormRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;
                            INSERT INTO "CaseWorkflowDisplayRole" ("Guid", "CaseWorkflowDisplayGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowDisplayGuid", v_analyst, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowDisplayRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;
                            INSERT INTO "CaseWorkflowMacroRole" ("Guid", "CaseWorkflowMacroGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowMacroGuid", v_analyst, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowMacroRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;
                            INSERT INTO "CaseWorkflowXPathRole" ("Guid", "CaseWorkflowXPathGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowXPathGuid", v_analyst, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowXPathRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;
                            INSERT INTO "CaseWorkflowRole" ("Guid", "CaseWorkflowGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_wf1, v_supervisor, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), s."Guid", v_supervisor, v_now, 'Administrator', 0, 1
                            FROM "CaseWorkflowStatus" s JOIN "CaseWorkflowStatusRole" g ON g."CaseWorkflowStatusGuid" = s."Guid" AND g."RoleRegistryGuid" = v_role AND coalesce(g."Deleted", 0) = 0
                            WHERE s."CaseWorkflowId" = v_wf1_id AND coalesce(s."Deleted", 0) = 0 AND s."Name" <> 'Smoke: Admin Only'
                              AND NOT EXISTS (SELECT 1 FROM "CaseWorkflowStatusRole" x WHERE x."CaseWorkflowStatusGuid" = s."Guid" AND x."RoleRegistryGuid" = v_supervisor);
                            INSERT INTO "CaseWorkflowActionRole" ("Guid", "CaseWorkflowActionGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowActionGuid", v_supervisor, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowActionRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;
                            INSERT INTO "CaseWorkflowFormRole" ("Guid", "CaseWorkflowFormGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowFormGuid", v_supervisor, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowFormRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;
                            INSERT INTO "CaseWorkflowDisplayRole" ("Guid", "CaseWorkflowDisplayGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowDisplayGuid", v_supervisor, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowDisplayRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;
                            INSERT INTO "CaseWorkflowMacroRole" ("Guid", "CaseWorkflowMacroGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowMacroGuid", v_supervisor, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowMacroRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;
                            INSERT INTO "CaseWorkflowXPathRole" ("Guid", "CaseWorkflowXPathGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            SELECT gen_random_uuid(), x."CaseWorkflowXPathGuid", v_supervisor, v_now, 'Administrator', 0, 1 FROM "CaseWorkflowXPathRole" x WHERE x."RoleRegistryGuid" = v_role AND coalesce(x."Deleted", 0) = 0;

                            INSERT INTO "CaseWorkflow" ("Name", "TenantRegistryId", "Active", "Locked", "EnableVisualisation", "CreatedDate", "CreatedUser", "Deleted", "EntityAnalysisModelId", "Version", "Guid", "FirstViewTargetMinutes", "OwnershipTargetMinutes", "ResolutionTargetMinutes", "DailyGoal", "TypologySeverityId")
                            VALUES ('Smoke: Role Gated', v_tenant, 1, 0, 0, v_now, 'Administrator', 0, v_model, 1, v_wf2, 240, 480, 2880, 10, 3)
                            RETURNING "Id" INTO v_wf2_id;
                            v_status := gen_random_uuid();
                            INSERT INTO "CaseWorkflowStatus" ("CaseWorkflowId", "Priority", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid", "OutcomeId", "EscalationReleaseLock")
                            VALUES (v_wf2_id, 1, 'Gated Review', 1, 0, v_now, 'Administrator', 0, 1, v_status, 0, 0);
                            INSERT INTO "CaseWorkflowRole" ("Guid", "CaseWorkflowGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_wf2, v_analyst, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_analyst, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowRole" ("Guid", "CaseWorkflowGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_wf2, v_supervisor, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_supervisor, v_now, 'Administrator', 0, 1);

                            INSERT INTO "CaseWorkflow" ("Name", "TenantRegistryId", "Active", "Locked", "EnableVisualisation", "CreatedDate", "CreatedUser", "Deleted", "EntityAnalysisModelId", "Version", "Guid", "FirstViewTargetMinutes", "OwnershipTargetMinutes", "ResolutionTargetMinutes", "DailyGoal", "TypologySeverityId")
                            VALUES ('Smoke: Queue Volume', v_tenant, 1, 0, 0, v_now, 'Administrator', 0, v_model, 1, v_wf3, 60, 120, 480, 25, 3)
                            RETURNING "Id" INTO v_wf3_id;
                            v_status := gen_random_uuid();
                            INSERT INTO "CaseWorkflowStatus" ("CaseWorkflowId", "Priority", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid", "OutcomeId", "MaxDwellMinutes", "EscalationReleaseLock")
                            VALUES (v_wf3_id, 2, 'Queue', 1, 0, v_now, 'Administrator', 0, 1, v_status, 0, 720, 0);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_role, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_analyst, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_supervisor, v_now, 'Administrator', 0, 1);
                            v_status := gen_random_uuid();
                            INSERT INTO "CaseWorkflowStatus" ("CaseWorkflowId", "Priority", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid", "OutcomeId", "MaxDwellMinutes", "EscalationReleaseLock")
                            VALUES (v_wf3_id, 1, 'Resolved', 1, 0, v_now, 'Administrator', 0, 1, v_status, 2, NULL, 0);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_role, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_analyst, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_supervisor, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowRole" ("Guid", "CaseWorkflowGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_wf3, v_role, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowRole" ("Guid", "CaseWorkflowGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_wf3, v_analyst, v_now, 'Administrator', 0, 1);
                            INSERT INTO "CaseWorkflowRole" ("Guid", "CaseWorkflowGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_wf3, v_supervisor, v_now, 'Administrator', 0, 1);

                            INSERT INTO "CaseWorkflowReleaseHook" ("Guid", "CaseWorkflowGuid", "Name", "Active", "Locked", "SourceField", "SourceValue", "ReleaseUrl", "RefuseUrl", "HttpEndpointTypeId", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_wf1, 'Smoke release hook (unreachable)', 1, 0, NULL, NULL, 'http://127.0.0.1:9/release', 'http://127.0.0.1:9/refuse', 1, v_now, 'Administrator', 0, 1),
                                   (gen_random_uuid(), v_wf1, 'Smoke ATM hook (no release url)', 1, 0, 'ChannelId', 'ATM', NULL, 'http://127.0.0.1:9/refuse', 1, v_now, 'Administrator', 0, 1);

                            v_status := gen_random_uuid();
                            INSERT INTO "CaseWorkflowFilter" ("CaseWorkflowId", "Name", "FilterSql", "FilterJson", "SelectJson", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid")
                            VALUES (v_wf1_id, 'Smoke: Broken filter', '"Case"."NoSuchColumn" = @1', '{"rules": [{"id": "NoSuchColumn", "type": "integer", "field": "\"Case\".\"NoSuchColumn\"", "input": "select", "value": 1, "operator": "equal"}], "valid": true, "condition": "AND"}', coalesce((SELECT f."SelectJson" FROM "CaseWorkflowFilter" f WHERE f."Name" = 'OpenCasesNotLockedByCreatedDate' LIMIT 1), '{}'), 1, 0, v_now, 'Administrator', 0, 1, v_status);
                            INSERT INTO "CaseWorkflowFilterRole" ("Guid", "CaseWorkflowFilterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_status, v_role, v_now, 'Administrator', 0, 1);
                        END
                        $seed$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
