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

namespace Jube.Migrations.Branches.CaseChecklists
{
    using FluentMigrator;

    [Migration(20263108420000)]
    public class SeedDemonstrationCaseChecklists : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE OR REPLACE FUNCTION pg_temp.seed_checklist_item(
                            p_status text, p_list text, p_list_priority integer, p_item text, p_item_priority integer,
                            p_show text, p_validate text, p_message text, p_sla integer)
                        RETURNS void LANGUAGE plpgsql AS $f$
                        DECLARE
                            v_status uuid;
                            v_list uuid;
                            v_now timestamp := now() at time zone 'utc';
                        BEGIN
                            SELECT s."Guid" INTO v_status
                            FROM "CaseWorkflowStatus" s
                            JOIN "CaseWorkflow" w ON w."Id" = s."CaseWorkflowId"
                            WHERE w."Name" = 'Detailed Account Financial Transactions Cases'
                              AND s."Name" = p_status
                              AND coalesce(s."Deleted", 0) = 0
                              AND coalesce(w."Deleted", 0) = 0
                            ORDER BY s."Id" LIMIT 1;

                            IF v_status IS NULL THEN
                                RETURN;
                            END IF;

                            SELECT "Guid" INTO v_list FROM "CaseWorkflowChecklist"
                            WHERE "CaseWorkflowStatusGuid" = v_status AND "Name" = p_list AND coalesce("Deleted", 0) = 0
                            ORDER BY "Id" LIMIT 1;

                            IF v_list IS NULL THEN
                                v_list := gen_random_uuid();
                                INSERT INTO "CaseWorkflowChecklist" ("Guid", "CaseWorkflowStatusGuid", "Name", "Priority",
                                    "Active", "CreatedDate", "CreatedUser", "Deleted", "Version")
                                VALUES (v_list, v_status, p_list, p_list_priority, 1, v_now, 'Administrator', 0, 1);
                            END IF;

                            IF NOT EXISTS (SELECT 1 FROM "CaseWorkflowChecklistItem"
                                           WHERE "CaseWorkflowChecklistGuid" = v_list AND "Name" = p_item
                                             AND coalesce("Deleted", 0) = 0) THEN
                                INSERT INTO "CaseWorkflowChecklistItem" ("Guid", "CaseWorkflowChecklistGuid", "Name",
                                    "Priority", "ShowSql", "ValidateSql", "ValidateMessage", "SlaMinutes", "Active",
                                    "CreatedDate", "CreatedUser", "Deleted", "Version")
                                VALUES (gen_random_uuid(), v_list, p_item, p_item_priority, p_show, p_validate,
                                    p_message, p_sla, 1, v_now, 'Administrator', 0, 1);
                            END IF;
                        END
                        $f$;

                        SELECT pg_temp.seed_checklist_item('First Line Review', 'Initial Triage', 1,
                            'Review the triggering transactions', 1, NULL, NULL, NULL, NULL);

                        SELECT pg_temp.seed_checklist_item('First Line Review', 'Initial Triage', 1,
                            'Record an investigation note', 2, NULL,
                            $q$select exists (select 1 from "CaseNote" n where n."CaseId" = @Id)$q$,
                            'Add at least one note to the case before ticking this item.', 60);

                        SELECT pg_temp.seed_checklist_item('First Line Review', 'Initial Triage', 1,
                            'Contact the customer', 3,
                            $q$select coalesce((select (c."Json" -> 'payload' ->> 'CurrencyAmount')::numeric >= 1000 from "Case" c where c."Id" = @Id), false)$q$,
                            $q$select exists (select 1 from "CaseWorkflowFormEntry" e where e."CaseId" = @Id)$q$,
                            'Submit a case form recording the customer contact before ticking this item.', 240);

                        SELECT pg_temp.seed_checklist_item('First Line Review', 'Initial Triage', 1,
                            'Check related cases on the case key', 4,
                            $q$select (select count(*) from "Case" c where c."CaseKeyValue" = @CaseKeyValue) > 1$q$,
                            NULL, NULL, 120);

                        SELECT pg_temp.seed_checklist_item('Supervisor Review', 'Supervisor Sign-off', 1,
                            'Second-person review', 1, NULL,
                            $q$select exists (select 1 from "CaseNote" n join "Case" c on c."Id" = n."CaseId" where c."Id" = @Id and n."CreatedUser" = c."LockedUser") and exists (select 1 from "CaseNote" n join "Case" c on c."Id" = n."CaseId" where c."Id" = @Id and n."CreatedUser" <> coalesce(c."LockedUser", chr(32)))$q$,
                            'The reviewer must add their own note, and the first-line analyst must already have noted the case.', 120);

                        SELECT pg_temp.seed_checklist_item('Fraudulent', 'Fraud Confirmation', 1,
                            'Record the finding', 1, NULL,
                            $q$select exists (select 1 from "CaseNote" n where n."CaseId" = @Id and n."IsFinding" = true)$q$,
                            'Add a note marked as a finding before ticking this item.', NULL);
                        """);
        }

        public override void Down()
        {
        }
    }
}
