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

    [Migration(2026111790000000)]
    public class SeedExecutiveDemonstrationConfiguration : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_model integer;
                            v_tenant integer;
                            v_workflow_id integer;
                            v_workflow uuid;
                            v_status_first uuid;
                            v_role uuid;
                            v_now timestamp := now() at time zone 'utc';
                            v_rule record;
                            v_ver integer;
                            v_makers text[] := ARRAY['Priya Raman', 'Tomasz Kowalski', 'Amara Okafor'];
                            v_checkers text[] := ARRAY['Lena Fischer', 'Daniel Brooks'];
                            v_created timestamp;
                            v_maker text;
                            v_checker text;
                        BEGIN
                            SELECT m."Id", m."TenantRegistryId" INTO v_model, v_tenant FROM "EntityAnalysisModel" m
                            WHERE m."Guid" = '90c425fd-101a-420b-91d1-cb7a24a969cc' AND coalesce(m."Deleted", 0) = 0;

                            IF v_model IS NULL THEN
                                RETURN;
                            END IF;

                            IF NOT EXISTS (SELECT 1 FROM "Case" WHERE "CaseKey" = 'AccountId' AND "CaseKeyValue" = 'ACC-441') THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "EntityAnalysisModelActivationRule" WHERE "EntityAnalysisModelId" = v_model AND "Name" = 'StructuringBroad') THEN
                                RETURN;
                            END IF;

                            SELECT "Id", "Guid" INTO v_workflow_id, v_workflow FROM "CaseWorkflow"
                            WHERE "EntityAnalysisModelId" = v_model AND coalesce("Deleted", 0) = 0 ORDER BY "Id" LIMIT 1;

                            IF v_workflow IS NULL THEN
                                RETURN;
                            END IF;

                            SELECT "Guid" INTO v_status_first FROM "CaseWorkflowStatus"
                            WHERE "CaseWorkflowId" = v_workflow_id AND "Name" = 'First Line Review' AND coalesce("Deleted", 0) = 0 LIMIT 1;

                            SELECT "Guid" INTO v_role FROM "RoleRegistry" WHERE "Id" = 1;

                            UPDATE "CaseWorkflow" SET
                                "FirstViewTargetMinutes" = coalesce("FirstViewTargetMinutes", 240),
                                "OwnershipTargetMinutes" = coalesce("OwnershipTargetMinutes", 480),
                                "ResolutionTargetMinutes" = coalesce("ResolutionTargetMinutes", 2880),
                                "DailyGoal" = coalesce("DailyGoal", 15),
                                "RiskQaSampleRate" = coalesce("RiskQaSampleRate", 0.10)
                            WHERE "Id" = v_workflow_id;

                            UPDATE "CaseWorkflowStatus" SET "MaxDwellMinutes" = CASE "Name" WHEN 'First Line Review' THEN 1440 WHEN 'Supervisor Review' THEN 2880 ELSE "MaxDwellMinutes" END
                            WHERE "CaseWorkflowId" = v_workflow_id AND "Name" IN ('First Line Review', 'Supervisor Review') AND "MaxDwellMinutes" IS NULL;

                            IF v_role IS NOT NULL THEN
                                INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                                SELECT gen_random_uuid(), s."Guid", v_role, v_now, 'Administrator', 0, 1
                                FROM "CaseWorkflowStatus" s
                                WHERE s."CaseWorkflowId" = v_workflow_id AND coalesce(s."Deleted", 0) = 0
                                  AND NOT EXISTS (SELECT 1 FROM "CaseWorkflowStatusRole" r WHERE r."CaseWorkflowStatusGuid" = s."Guid" AND r."RoleRegistryGuid" = v_role AND coalesce(r."Deleted", 0) = 0);
                            END IF;

                            UPDATE "EntityAnalysisModelRequestXpath" SET "ReportTable" = 1, "UpdatedDate" = v_now, "UpdatedUser" = 'Administrator'
                            WHERE "EntityAnalysisModelId" = v_model AND coalesce("Deleted", 0) = 0 AND coalesce("ReportTable", 0) = 0
                              AND "Name" IN ('AccountId', 'AmountEUR', 'BillingCountry', 'ChannelId');

                            CREATE TEMP TABLE exe_rule_spec ON COMMIT DROP AS
                            SELECT * FROM (VALUES
                                ('StructuringBroad', 0, 0, 1, 1, 3, 330, 0, 'If (Payload.AmountEUR > 3500 And Payload.AmountEUR < 5000) Then' || chr(10) || '    Return True' || chr(10) || 'End If' || chr(10) || 'Return False', 2, 10.0),
                                ('StructuringTuned', 1, 0, 1, 1, 2, 180, 0, 'If (Payload.AmountEUR > 3900 And Payload.AmountEUR < 5000 And Abstraction.ResponseCodeEqual0Volume > 3) Then' || chr(10) || '    Return True' || chr(10) || 'End If' || chr(10) || 'Return False', 2, 11.0),
                                ('RapidVelocity', 1, 0, 1, 1, 3, 340, 0, 'If (Abstraction.ResponseCodeEqual0Volume > 8) Then' || chr(10) || '    Return True' || chr(10) || 'End If' || chr(10) || 'Return False', 1, 12.0),
                                ('RapidVelocityTightenedShadow', 1, 1, 1, 0, 2, 50, 0, 'If (Abstraction.ResponseCodeEqual0Volume > 6) Then' || chr(10) || '    Return True' || chr(10) || 'End If' || chr(10) || 'Return False', 1, 13.0),
                                ('MuleRingFanOut', 1, 0, 1, 1, 2, 300, 0, 'If (Payload.AmountEUR > 9000) Then' || chr(10) || '    Return True' || chr(10) || 'End If' || chr(10) || 'Return False', 3, 14.0),
                                ('ImpossibleTravel', 1, 0, 1, 1, 3, 320, 0, 'If (Payload.BillingCountry <> "GB" And Payload.AmountEUR > 500) Then' || chr(10) || '    Return True' || chr(10) || 'End If' || chr(10) || 'Return False', 2, 15.0),
                                ('HighRiskCountryCrossing', 1, 0, 1, 0, 1, 290, 0, 'If (Payload.BillingCountry = "IR" Or Payload.BillingCountry = "KP" Or Payload.BillingCountry = "MM") Then' || chr(10) || '    Return True' || chr(10) || 'End If' || chr(10) || 'Return False', 1, 16.0)
                            ) AS t("Name", "Active", "Shadow", "Visible", "OpensCase", "Version", "CreatedDaysAgo", "Unused", "Script", "Elevation", "Priority");

                            FOR v_rule IN SELECT * FROM exe_rule_spec LOOP
                                v_created := v_now - make_interval(days => v_rule."CreatedDaysAgo");
                                v_maker := v_makers[1 + (abs(hashtext(v_rule."Name")) % 3)];

                                INSERT INTO "EntityAnalysisModelActivationRule" ("EntityAnalysisModelId", "Name", "CreatedDate", "ResponseElevation", "EnableCaseWorkflow", "Active", "Version",
                                    "CreatedUser", "Deleted", "Locked", "ResponsePayload", "EnableTtlCounter", "SendToActivationWatcher", "ActivationSample", "ReportTable", "EnableNotification",
                                    "CoderRuleScript", "RuleScriptTypeId", "EnableResponseElevation", "CaseKey", "ResponseElevationKey", "EnableBypass", "Visible", "EnableReprocessing",
                                    "Guid", "CaseWorkflowStatusGuid", "CaseWorkflowGuid", "Priority", "Shadow", "BuilderRuleScript", "ActivationCounter", "EvaluationCounter")
                                VALUES (v_model, v_rule."Name", v_created, v_rule."Elevation", v_rule."OpensCase", v_rule."Active", v_rule."Version",
                                    v_maker, 0, 0, 0, 0, 0, 1, 0, 0,
                                    v_rule."Script", '2', 1, 'AccountId', 'AccountId', 0, v_rule."Visible", 0,
                                    gen_random_uuid(), CASE WHEN v_rule."OpensCase" = 1 THEN v_status_first END, CASE WHEN v_rule."OpensCase" = 1 THEN v_workflow END, v_rule."Priority", v_rule."Shadow",
                                    'Return False', 0, 0);
                            END LOOP;

                            FOR v_rule IN
                                SELECT r."Id", r."Name", r."Version", r."CreatedDate", r."CreatedUser", r."Guid", r."EntityAnalysisModelId", r."CoderRuleScript", r."ResponseElevation", r."CaseKey", r."ResponseElevationKey", r."Priority", r."Visible",
                                       r."Shadow", r."Active", r."EnableCaseWorkflow", r."CaseWorkflowGuid", r."CaseWorkflowStatusGuid"
                                FROM "EntityAnalysisModelActivationRule" r
                                JOIN exe_rule_spec s ON s."Name" = r."Name"
                                WHERE r."EntityAnalysisModelId" = v_model
                            LOOP
                                FOR v_ver IN 1..v_rule."Version" LOOP
                                    v_maker := v_makers[1 + ((abs(hashtext(v_rule."Name")) + v_ver) % 3)];
                                    v_checker := v_checkers[1 + ((abs(hashtext(v_rule."Name")) + v_ver) % 2)];
                                    v_created := v_rule."CreatedDate" + make_interval(days => (v_ver - 1) * 55);

                                    IF v_ver < v_rule."Version" THEN
                                        INSERT INTO "EntityAnalysisModelActivationRuleVersion" ("EntityAnalysisModelActivationRuleId", "EntityAnalysisModelId", "Name", "CreatedDate", "ResponseElevation",
                                            "CaseWorkflowGuid", "EnableCaseWorkflow", "Active", "Version", "CreatedUser", "Deleted", "Locked", "ResponsePayload", "EnableTtlCounter", "SendToActivationWatcher",
                                            "CaseWorkflowStatusGuid", "ActivationSample", "ReportTable", "EnableNotification", "CoderRuleScript", "RuleScriptTypeId", "EnableResponseElevation", "CaseKey",
                                            "ResponseElevationKey", "EnableBypass", "Visible", "EnableReprocessing", "Guid", "Priority", "Shadow", "BuilderRuleScript")
                                        VALUES (v_rule."Id", v_rule."EntityAnalysisModelId", v_rule."Name", v_created, v_rule."ResponseElevation"::integer,
                                            v_rule."CaseWorkflowGuid", v_rule."EnableCaseWorkflow", v_rule."Active", v_ver, v_maker, 0, 0, 0, 0, 0,
                                            v_rule."CaseWorkflowStatusGuid", 1, 0, 0, v_rule."CoderRuleScript", '2', 1, v_rule."CaseKey",
                                            v_rule."ResponseElevationKey", 0, v_rule."Visible", 0, v_rule."Guid", v_rule."Priority", v_rule."Shadow", 'Return False');
                                    END IF;

                                    IF v_rule."Name" = 'RapidVelocityTightenedShadow' AND v_ver = 2 THEN
                                        CONTINUE;
                                    END IF;

                                    IF v_rule."Name" = 'ImpossibleTravel' AND v_ver = 3 THEN
                                        CONTINUE;
                                    END IF;

                                    INSERT INTO "EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId", "EntityVersion", "StateId", "Note", "CreatedDate", "CreatedUser", "Guid")
                                    VALUES (v_tenant, 16, v_rule."Id", v_ver,
                                        CASE WHEN v_rule."Name" IN ('StructuringTuned', 'MuleRingFanOut') AND v_ver = 1 THEN 2 ELSE 1 END,
                                        CASE WHEN v_rule."Name" IN ('StructuringTuned', 'MuleRingFanOut') AND v_ver = 1 THEN 'Threshold floor is too low for this population; widen the window and require a minimum volume before resubmitting.'
                                             WHEN v_ver = 1 THEN 'Reviewed against the backtest and approved.'
                                             ELSE 'Change reviewed and approved.' END,
                                        v_created + interval '1 day' + make_interval(hours => (abs(hashtext(v_rule."Name")) + v_ver) % 9),
                                        v_checker, gen_random_uuid());
                                END LOOP;
                            END LOOP;

                            UPDATE "EntityApproval" SET "CreatedUser" = v_checkers[1 + (("EntityId" + 1) % 2)]
                            WHERE "EntityApprovalKindId" = 16 AND "CreatedUser" = 'Administrator'
                              AND "EntityId" IN (SELECT r."Id" FROM "EntityAnalysisModelActivationRule" r WHERE r."EntityAnalysisModelId" = v_model AND r."CreatedUser" = 'Administrator');

                            INSERT INTO "EntityAnalysisModelActivationRuleOverride" ("CreatedDate", "CreatedUser", "Version", "Deleted", "OverrideKey", "OverrideKeyValue", "EntityAnalysisModelActivationRuleName",
                                "Guid", "EntityAnalysisModelGuid", "DeleteExpiryDate", "OverrideKind")
                            SELECT v_now - make_interval(days => o."DaysAgo"), o."Maker", 1, 0, 'AccountId', o."Value", o."Rule", gen_random_uuid(),
                                   '90c425fd-101a-420b-91d1-cb7a24a969cc', v_now + make_interval(days => o."Expires"), o."Kind"
                            FROM (VALUES ('ACC-S0042', 'RapidVelocity', 0, 5, 25, 'Lena Fischer'), ('ACC-S0099', 'ImpossibleTravel', 1, 3, 12, 'Daniel Brooks')) AS o("Value", "Rule", "Kind", "DaysAgo", "Expires", "Maker")
                            WHERE NOT EXISTS (SELECT 1 FROM "EntityAnalysisModelActivationRuleOverride" x WHERE x."OverrideKeyValue" = o."Value" AND x."EntityAnalysisModelActivationRuleName" = o."Rule");

                            INSERT INTO "EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId", "EntityVersion", "StateId", "Note", "CreatedDate", "CreatedUser", "Guid")
                            SELECT v_tenant, 23, x."Id", 1, 1, 'Override approved for the stated period.', x."CreatedDate" + interval '3 hours',
                                   CASE x."CreatedUser" WHEN 'Lena Fischer' THEN 'Daniel Brooks' ELSE 'Lena Fischer' END, gen_random_uuid()
                            FROM "EntityAnalysisModelActivationRuleOverride" x
                            WHERE x."OverrideKeyValue" IN ('ACC-S0042', 'ACC-S0099')
                              AND NOT EXISTS (SELECT 1 FROM "EntityApproval" a WHERE a."EntityApprovalKindId" = 23 AND a."EntityId" = x."Id");
                        END
                        $seed$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
