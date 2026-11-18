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

    [Migration(2026111794000001)]
    public class SeedSmokeTestCaseGates : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_model integer;
                            v_model_guid uuid := '90c425fd-101a-420b-91d1-cb7a24a969cc';
                            v_wf1 uuid;
                            v_wf1_id integer;
                            v_wf2 uuid;
                            v_wf3 uuid;
                            v_flr uuid;
                            v_sup uuid;
                            v_now timestamp := date_trunc('minute', now() at time zone 'utc');
                        BEGIN
                            SELECT m."Id" INTO v_model FROM "EntityAnalysisModel" m WHERE m."Guid" = v_model_guid AND coalesce(m."Deleted", 0) = 0;
                            SELECT "Id", "Guid" INTO v_wf1_id, v_wf1 FROM "CaseWorkflow" WHERE "EntityAnalysisModelId" = v_model AND coalesce("Deleted", 0) = 0 AND "Name" NOT LIKE 'Smoke:%' ORDER BY "Id" LIMIT 1;
                            SELECT "Guid" INTO v_wf2 FROM "CaseWorkflow" WHERE "Name" = 'Smoke: Role Gated' AND coalesce("Deleted", 0) = 0;
                            SELECT "Guid" INTO v_wf3 FROM "CaseWorkflow" WHERE "Name" = 'Smoke: Queue Volume' AND coalesce("Deleted", 0) = 0;
                            SELECT "Guid" INTO v_flr FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_wf1_id AND "Name" = 'First Line Review' AND coalesce("Deleted", 0) = 0;
                            SELECT "Guid" INTO v_sup FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_wf1_id AND "Name" = 'Supervisor Review' AND coalesce("Deleted", 0) = 0;

                            IF v_model IS NULL OR v_wf1 IS NULL OR v_wf2 IS NULL OR v_wf3 IS NULL OR v_flr IS NULL OR v_sup IS NULL THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "Case" WHERE "CaseKeyValue" LIKE 'SMK-%' LIMIT 1) THEN
                                RETURN;
                            END IF;

                            CREATE TEMP TABLE smk_def (code text, wf text, status text, n int, closed int, reason int, note text, locked int, luser text, lock_h numeric, age_h numeric, close_h numeric, cuser text, nulldates boolean, diary int, dtype int, diary_h numeric, lane int, prio int, lik double precision, rval double precision, qa int, hold int, hreason text, hdate_h numeric, rating int, events text, trans text, kv text, archive boolean, pin boolean, assess boolean, hstate int, sim boolean, rule text, jsonmode text) ON COMMIT DROP;
                            INSERT INTO smk_def VALUES
                                ('OPEN-FRESH', 'W1', 'First Line Review', 5, 0, NULL, NULL, 0, NULL, NULL, 1, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('OPEN-WORKED', 'W1', 'First Line Review', 3, 0, NULL, NULL, 1, 'Administrator', 2, 20, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-R1', 'W1', 'First Line Review', 2, 3, 1, 'Customer confirmed the activity; consistent with the established pattern.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 1, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-R2', 'W1', 'First Line Review', 2, 3, 2, 'Threshold caught ordinary behaviour; recommend a rule review.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-R3', 'W1', 'First Line Review', 2, 3, 3, 'Duplicate of an existing case for the same account.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-R4', 'W1', 'First Line Review', 2, 3, 4, 'Evidence insufficient to establish suspicion.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-R5', 'W1', 'Reported', 2, 3, 5, 'Suspicion confirmed; report prepared for the nominated officer.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,9,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-R6', 'W1', 'Fraudulent', 2, 3, 6, 'Fraud confirmed and the payment stopped before settlement.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,9,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-R7', 'W1', 'First Line Review', 2, 3, 7, 'Customer contacted and gave a satisfactory explanation.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-R8', 'W1', 'First Line Review', 2, 3, 8, 'Reviewed and closed.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-NO-REASON', 'W1', 'First Line Review', 2, 3, NULL, NULL, 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-NOTE-1000', 'W1', 'First Line Review', 1, 3, 8, repeat('N', 1000), 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-NOTE-BLANK', 'W1', 'First Line Review', 1, 3, 8, '   ', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-NO-EVENTS', 'W1', 'First Line Review', 1, 3, 8, 'Closed with no history.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '', 'none', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-NULL-DATES', 'W1', 'First Line Review', 1, 3, 8, NULL, 0, NULL, NULL, 80, 30, 'Amara Okafor', true, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-NEGATIVE-DURATION', 'W1', 'First Line Review', 1, 3, 8, NULL, 0, NULL, NULL, 10, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CLOSED-STATUS-NULL', 'W1', 'First Line Review', 1, NULL, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('REOPENED', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Administrator', 1, 100, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5,9', 'reopen', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SUSPEND-OPEN', 'W1', 'First Line Review', 1, 1, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 2, 72, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,13', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SUSPEND-CLOSED', 'W1', 'First Line Review', 1, 2, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 2, 72, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,12', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SUSPEND-BYPASS', 'W1', 'First Line Review', 1, 4, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 2, 72, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,12', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SUSPEND-DIARY-DUE', 'W1', 'First Line Review', 1, 1, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 2, -2, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,13', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('STATUS-SUPERVISOR', 'W1', 'Supervisor Review', 3, 0, NULL, NULL, 1, 'Lena Fischer', 5, 40, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,9', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('STATUS-RESTRICTED', 'W1', 'Restricted', 2, 0, NULL, NULL, 0, NULL, NULL, 40, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,9', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('STATUS-FRAUDULENT-OPEN', 'W1', 'Fraudulent', 2, 0, NULL, NULL, 0, NULL, NULL, 40, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,9', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-MINE', 'W1', 'First Line Review', 3, 0, NULL, NULL, 1, 'Administrator', 3, 12, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-OTHER', 'W1', 'First Line Review', 3, 0, NULL, NULL, 1, 'Amara Okafor', 3, 12, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-STUCK-3H', 'W1', 'First Line Review', 3, 0, NULL, NULL, 1, 'Tomasz Kowalski', 3, 14, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-STUCK-30H', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Priya Raman', 30, 36, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-FRESH', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Jonas Berg', 0.17, 2, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-ORPHAN', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, NULL, 5, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-NOBODY', 'W1', 'First Line Review', 2, 0, NULL, NULL, 0, 'Mei Lin', NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-NULL', 'W1', 'First Line Review', 2, 0, NULL, NULL, NULL, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-GHOST', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Departed Analyst', 6, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-NULL-DATE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Carlos Mendes', NULL, 30, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-ON-CLOSED', 'W1', 'First Line Review', 2, 3, 8, 'Closed while held.', 1, 'Administrator', 40, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LOCK-RTL', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'عمر الفاروق', 4, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RV-ADMIN-ONLY', 'W1', 'Smoke: Admin Only', 2, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RV-UNGRANTED', 'W1', 'Smoke: Ungranted', 2, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RV-INACTIVE-STATUS', 'W1', 'Smoke: Inactive', 2, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RV-DELETED-STATUS', 'W1', 'Smoke: Deleted', 2, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RV-GATED-WORKFLOW', 'W2', 'Gated Review', 3, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-NONE-DEFINED', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-ALL-OPEN', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-ALL-DONE', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-PARTIAL', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-SLA-BREACHED', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Administrator', 1, 30, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,28', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-DUE-NOT-STAMPED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 30, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-DONE-LATE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 30, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,28', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-NULL-DUE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-OVERRIDE-LIVE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,25,26', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-OVERRIDE-OWN', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,25,26', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-OVERRIDE-STALE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,26', 'moved_other', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-OVERRIDE-CONSUMED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,26,27', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-OVERRIDE-WRONG-STATUS', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,26', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-ORPHAN-ITEM', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-WRONG-STATUS-ITEM', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-SUPERVISOR-OPEN', 'W1', 'Supervisor Review', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,9', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('CL-FRAUD-FINDING', 'W1', 'Fraudulent', 1, 0, NULL, NULL, 1, 'Administrator', 1, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,9', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LH-NORMAL', 'W1', 'First Line Review', 2, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 1, 'Held pending regulatory enquiry.', 48, 0, '4,32', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LH-FILING', 'W1', 'Reported', 1, 3, 5, 'Reported.', 0, NULL, NULL, 90, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 1, 'Regulatory report filed', 40, 0, '4,6,5,32', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LH-LIFTED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,32,33', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LH-NO-DETAIL', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 1, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LH-ON-CLOSED', 'W1', 'Fraudulent', 1, 3, 6, 'Fraud confirmed.', 0, NULL, NULL, 90, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 1, 'Retained for litigation.', 35, 0, '4,6,5,32', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LH-LIFT-EVENT-FLAG-ON', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 1, 'Held pending review.', 60, 0, '4,32,33', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T1-PAST', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 1, -30, 3, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T1-SOON', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 1, 20, 3, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T1-FAR', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 1, 720, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T2-PAST', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 2, -30, 3, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T2-SOON', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 2, 20, 3, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T2-FAR', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 2, 720, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T3-PAST', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 3, -30, 2, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T3-SOON', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 3, 20, 2, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T3-FAR', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 3, 720, 2, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T4-PAST', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 4, -30, 1, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T4-SOON', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 4, 20, 1, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-T4-FAR', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 4, 720, 1, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-NULL-DATE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-DATE-NO-FLAG', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 4, 24, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('DI-TYPE-INVALID', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 9, 20, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('PH-HELD', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 4, 6, 1, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,7', 'normal', 'std', true, true, true, 1, false, NULL, 'std'),
                                ('PH-HELD-OVERDUE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 4, -3, 1, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,7', 'normal', 'std', true, true, true, 1, false, NULL, 'std'),
                                ('PH-DECLINED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 4, -1, 1, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,7', 'normal', 'std', true, true, true, 2, false, NULL, 'std'),
                                ('PH-RELEASED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 4, -1, 1, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,7', 'normal', 'std', true, true, true, 3, false, NULL, 'std'),
                                ('PH-REFUSED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 4, -1, 1, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,7', 'normal', 'std', true, true, true, 4, false, NULL, 'std'),
                                ('PH-NO-DIARY', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, 1, false, NULL, 'std'),
                                ('PH-UNKNOWN-STATE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 4, 5, 1, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, 9, false, NULL, 'std'),
                                ('SL-ON-TRACK', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Administrator', 0.5, 1, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SL-AT-RISK', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Administrator', 40, 41, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SL-BREACHED', 'W1', 'First Line Review', 2, 0, NULL, NULL, 1, 'Administrator', 90, 100, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SL-FIRST-VIEW-BREACH', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SL-OWNERSHIP-BREACH', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 10, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SL-DWELL-BREACH', 'W1', 'First Line Review', 1, 0, NULL, NULL, 1, 'Administrator', 20, 30, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-LANE-1', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 1, 75, 0.7, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-LANE-2', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 2, 60, 0.5, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-LANE-3', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 3, 45, 0.29999999999999993, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-LANE-4', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.09999999999999998, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-LANE-NULL', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, NULL, NULL, NULL, NULL, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, false, false, NULL, false, NULL, 'std'),
                                ('RK-PRIORITY-NULL', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, NULL, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-PRIORITY-NEGATIVE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, -5, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-LIKELIHOOD-ZERO', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-LIKELIHOOD-ONE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 2, 30, 1, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-VALUE-HUGE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 1000000000000.0, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-VALUE-NULL', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, NULL, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-LANE-INVALID', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 9, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RK-ASSESSMENT-MISSING', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, false, NULL, false, NULL, 'std'),
                                ('RK-RISING', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 3, 70, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('QA-PICKED-OPEN', 'W1', 'First Line Review', 2, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 2, 0, NULL, NULL, 0, '4,20', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('QA-PICKED-CLOSED-OTHER', 'W1', 'First Line Review', 2, 3, 2, 'Rule too broad.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 2, 0, NULL, NULL, 0, '4,6,5,20', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('QA-PICKED-CLOSED-OWN', 'W1', 'First Line Review', 2, 3, 2, 'Rule too broad.', 0, NULL, NULL, 80, 30, 'Administrator', false, 0, 1, NULL, 4, 30, 0.3, 500, 2, 0, NULL, NULL, 0, '4,6,5,20', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('QA-REVIEWED', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 2, 0, NULL, NULL, 0, '4,6,5,20', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('QA-REVIEWED-AGENT', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 2, 0, NULL, NULL, 0, '4,6,5,20,29,30', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('QA-NOT-PICKED', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 1, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('QA-PICKED-SUSPENDED', 'W1', 'First Line Review', 1, 2, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 2, 72, 4, 30, 0.3, 500, 2, 0, NULL, NULL, 0, '4,12,20', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TR-PENDING', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,29', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TR-ACCEPTED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,29,30', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TR-OVERRIDDEN', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,29,31', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TR-SUPERSEDED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,29', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TR-INCONCLUSIVE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,29', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TR-NOT-CONFIRMED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,29', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TR-ON-CLOSED', 'W1', 'First Line Review', 1, 3, 2, 'Rule too broad.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5,29', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TR-LONG-EVIDENCE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,29', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-PENDING', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-APPROVED', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-REJECTED', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-RETRACTED', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-PENDING-LAPSED', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 500, 400, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-PENDING-EXPIRED', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 500, 400, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-OWN-REQUEST', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-APPROVED-REMOVED', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-APPROVED-EXPIRED', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 900, 800, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-FORCE-TARGET', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('SU-NO-TARGETS', 'W1', 'First Line Review', 1, 3, 1, 'Known activity.', 0, NULL, NULL, 80, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-NONE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-MANY', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-LONG', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-UNICODE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-NARRATIVE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-DOCUMENT', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-FINDING', 'W1', 'Fraudulent', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,9', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-HTML', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-AGENT-SYSTEM', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('NT-NO-ACTION', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TG-VOCABULARY', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TG-DELETED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TG-UNICODE-DUPLICATE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('TG-FIFTY', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LK-PAIR', 'W1', 'First Line Review', 2, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LK-HIDDEN', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LK-ORPHAN', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LK-REVERSED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('LK-SELF', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('AS-LOW-MEDIUM-HIGH', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,34', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('AS-WITHDRAWN', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,34,35', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('AS-HISTORY-21', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,34', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RP-DRAFT', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RP-VALIDATED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RP-PENDING-APPROVAL', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RP-APPROVED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RP-SUBMITTED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RP-ACKNOWLEDGED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RP-REJECTED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RP-WITHDRAWN', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('FM-ENTRIES', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('FM-NO-VALUES', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('FM-ORPHAN', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RT-REOPENED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,5,9', 'reopen', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RT-ESCALATED', 'W1', 'Supervisor Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,17', 'escalated', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RT-MOVED-BY-OTHER', 'W1', 'Supervisor Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,9', 'moved_other', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RT-NEW-EVENT', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,7', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RT-DIARY-DUE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 1, 1, -3, 3, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('RT-NOT-VIEWED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,5,9', 'reopen', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('ER-OPEN-CASE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'ER-OPEN', true, true, true, NULL, false, NULL, 'std'),
                                ('ER-LEGAL-HOLD', 'W1', 'First Line Review', 1, 3, 6, 'Closed.', 0, NULL, NULL, 90, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 1, 'Held pending enquiry.', 60, 0, '4,6,5,32', 'normal', 'ER-HELD', true, true, true, NULL, false, NULL, 'std'),
                                ('ER-RETENTION', 'W1', 'First Line Review', 1, 3, 2, 'Closed.', 0, NULL, NULL, 90, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'ER-RETAINED', true, true, true, NULL, false, NULL, 'std'),
                                ('ER-ERASABLE', 'W1', 'First Line Review', 1, 3, 2, 'Closed.', 0, NULL, NULL, 90, 30, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,6,5', 'normal', 'ER-OLD', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-KEY-RTL', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'rtl', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-KEY-EMOJI', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'emoji', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-KEY-LONG', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'long', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-KEY-EMPTY', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'empty', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-KEY-NULL', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'null', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-DUPLICATE-OPEN', 'W1', 'First Line Review', 2, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'dup', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-NO-ARCHIVE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', false, true, true, NULL, false, NULL, 'std'),
                                ('ED-JSON-EMPTY', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'empty'),
                                ('ED-JSON-BIG', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'big'),
                                ('ED-DELETED-RULE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, 'missing', 'std'),
                                ('ED-FUTURE-CREATED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, -240, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-MANY-EVENTS', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'normal', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-TRANSITIONS-NONE', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'none', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-TRANSITION-INVERTED', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4', 'inverted', 'std', true, true, true, NULL, false, NULL, 'std'),
                                ('ED-EVENT-TYPE-UNKNOWN', 'W1', 'First Line Review', 1, 0, NULL, NULL, 0, NULL, NULL, 6, NULL, 'Amara Okafor', false, 0, 1, NULL, 4, 30, 0.3, 500, 0, 0, NULL, NULL, 0, '4,99', 'normal', 'std', true, true, true, NULL, false, NULL, 'std');

                            CREATE TEMP TABLE smk_centroid (country text, lat double precision, lon double precision) ON COMMIT DROP;
                            INSERT INTO smk_centroid VALUES ('GB', 51.5074, -0.1278), ('DE', 52.52, 13.405), ('FR', 48.8566, 2.3522), ('NL', 52.3676, 4.9041), ('ES', 40.4168, -3.7038), ('IT', 41.9028, 12.4964), ('PL', 52.2297, 21.0122), ('EG', 30.0444, 31.2357), ('TR', 41.0082, 28.9784), ('AE', 25.2048, 55.2708);

                            CREATE TEMP TABLE smk_case ON COMMIT DROP AS
                            SELECT d.*, g AS i, gen_random_uuid() AS guid,
                                   CASE d.wf WHEN 'W1' THEN v_wf1 WHEN 'W2' THEN v_wf2 ELSE v_wf3 END AS wf_guid,
                                   CASE d.kv WHEN 'std' THEN 'SMK-' || d.code || '-' || lpad(g::text, 2, '0')
                                             WHEN 'rtl' THEN 'SMK-' || 'العميل' || '-' || lpad(g::text, 2, '0')
                                             WHEN 'emoji' THEN 'SMK-' || '💳🚨' || '-' || lpad(g::text, 2, '0')
                                             WHEN 'long' THEN 'SMK-LONG-' || repeat('X', 600)
                                             WHEN 'empty' THEN ''
                                             WHEN 'null' THEN NULL
                                             WHEN 'dup' THEN 'SMK-ED-DUPLICATE-OPEN'
                                             ELSE 'SMK-' || d.kv END AS key_value,
                                   v_now - d.age_h * interval '1 hour' AS created,
                                   CASE WHEN d.close_h IS NOT NULL THEN v_now - d.close_h * interval '1 hour' END AS closed_at,
                                   50 + (abs(hashtext('a' || d.code || g)) % 9000) AS amount,
                                   (ARRAY['GB','DE','FR','NL','ES','IT','PL','EG','TR','AE'])[1 + (abs(hashtext('c' || d.code || g)) % 10)] AS country,
                                   (ARRAY['StructuringBroad','RapidVelocity','MuleRingFanOut','ImpossibleTravel','ThresholdSanctionsDistance','StructuringTuned'])[1 + (abs(hashtext('r' || d.code || g)) % 6)] AS rule_name
                            FROM smk_def d CROSS JOIN LATERAL generate_series(1, d.n) AS g;

                            ALTER TABLE smk_case ADD COLUMN status_guid uuid, ADD COLUMN rule_guid uuid, ADD COLUMN rule_id integer, ADD COLUMN ajson jsonb, ADD COLUMN arch_at timestamp;

                            UPDATE smk_case c SET
                                status_guid = (SELECT s."Guid" FROM "CaseWorkflowStatus" s WHERE s."CaseWorkflowId" = (SELECT w."Id" FROM "CaseWorkflow" w WHERE w."Guid" = c.wf_guid) AND s."Name" = c.status ORDER BY s."Id" LIMIT 1),
                                rule_id = (SELECT r."Id" FROM "EntityAnalysisModelActivationRule" r WHERE r."EntityAnalysisModelId" = v_model AND r."Name" = c.rule_name AND coalesce(r."Deleted", 0) = 0 ORDER BY r."Id" LIMIT 1),
                                arch_at = CASE WHEN c.kv = 'ER-OLD' THEN v_now - interval '2600 days' ELSE least(c.created, v_now) END;

                            UPDATE smk_case c SET rule_guid = CASE WHEN c.rule = 'missing' THEN gen_random_uuid() ELSE (SELECT r."Guid" FROM "EntityAnalysisModelActivationRule" r WHERE r."Id" = c.rule_id) END;

                            UPDATE smk_case c SET ajson = jsonb_strip_nulls(jsonb_build_object(
                                'payload', jsonb_build_object('TxnId', 'SMK-TXN-' || c.code || '-' || c.i, 'TxnDateTime', to_char(c.arch_at, 'YYYY-MM-DD"T"HH24:MI:SS'),
                                    'AccountId', c.key_value, 'CurrencyAmount', c.amount, 'AmountEUR', c.amount, 'Currency', 'EUR', 'BillingCountry', c.country,
                                    'ChannelId', (ARRAY['Card','ATM','Transfer'])[1 + (abs(hashtext('ch' || c.code || c.i)) % 3)], 'ResponseCode', '0',
                                    'AccountLatitude', round((m.lat + ((abs(hashtext('la' || c.code || c.i)) % 100) - 50) / 100.0)::numeric, 4),
                                    'AccountLongitude', round((m.lon + ((abs(hashtext('lo' || c.code || c.i)) % 100) - 50) / 100.0)::numeric, 4)),
                                'tag', '[]'::jsonb,
                                'activation', jsonb_build_object(c.rule_name, jsonb_build_object('visible', true, 'forced', false)),
                                'shadowActivation', '{}'::jsonb, 'abstraction', '{}'::jsonb, 'ttlCounter', '{}'::jsonb,
                                'responseElevation', jsonb_build_object('value', 1 + (abs(hashtext('e' || c.code || c.i)) % 400) / 100.0, 'name', 'Level 2', 'createdDate', to_char(c.arch_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"')),
                                'prevailingEntityAnalysisModelActivationRuleName', c.rule_name, 'entityAnalysisModelActivationRuleCount', 1,
                                'entityAnalysisModelGuid', v_model_guid::text, 'entityAnalysisModelName', 'Detailed Account Financial Transactions',
                                'entityAnalysisModelInstanceEntryGuid', c.guid::text, 'createdDate', to_char(c.arch_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'),
                                'referenceDate', to_char(c.arch_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'), 'createCase', true,
                                'hold', CASE WHEN c.hstate IS NULL THEN NULL ELSE jsonb_build_object('stateId', c.hstate, 'rangeId', 1, 'rangeName', 'Standard hold', 'rangeMinimum', 0, 'rangeMaximum', 10000, 'elevation', 3.2,
                                    'stampedDate', to_char(c.arch_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'),
                                    'decision', CASE WHEN c.hstate IN (2, 3, 4) THEN jsonb_build_object('actor', 'Lena Fischer', 'date', to_char(c.arch_at + interval '1 hour', 'YYYY-MM-DD"T"HH24:MI:SS"Z"'), 'reason', 'Reviewed by the supervisor.', 'releaseHookId', 1, 'releaseHookName', 'Smoke release hook (unreachable)', 'httpStatus', CASE WHEN c.hstate = 3 THEN 200 END) END,
                                    'legs', jsonb_build_array(jsonb_build_object('kindId', 1, 'outcomeId', 2, 'date', to_char(c.arch_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'), 'destination', 'ops@example.com', 'detail', 'sent'))) END))
                            FROM smk_centroid m WHERE m.country = c.country;

                            UPDATE smk_case c SET ajson = CASE c.jsonmode WHEN 'empty' THEN '{}'::jsonb ELSE c.ajson END;

                            INSERT INTO "Archive" ("Json", "EntityAnalysisModelInstanceEntryGuid", "EntryKeyValue", "ResponseElevation", "EntityAnalysisModelActivationRuleId", "EntityAnalysisModelId", "ActivationRuleCount", "CreatedDate", "ReferenceDate", "Version")
                            SELECT c.ajson, c.guid, 'SMK-TXN-' || c.code || '-' || c.i, 1 + (abs(hashtext('e' || c.code || c.i)) % 400) / 100.0, c.rule_id, v_model, 1, c.arch_at, c.arch_at, 1
                            FROM smk_case c WHERE c.archive;

                            INSERT INTO "ArchiveKey" ("ProcessingTypeId", "Key", "KeyValueString", "KeyValueFloat", "EntityAnalysisModelInstanceEntryGuid", "Version")
                            SELECT 1, k.key, k.sv, k.fv, c.guid, 1
                            FROM smk_case c CROSS JOIN LATERAL (VALUES ('AccountId', c.key_value, NULL::double precision), ('AmountEUR', NULL, c.amount::double precision), ('BillingCountry', c.country, NULL)) AS k(key, sv, fv)
                            WHERE c.archive AND c.key_value IS NOT NULL AND c.key_value <> '' OR (c.archive AND k.key <> 'AccountId');

                            INSERT INTO "Case" ("EntityAnalysisModelInstanceEntryGuid", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "CreatedDate", "Locked", "LockedUser", "LockedDate", "ClosedStatusId", "ClosedDate", "ClosedUser",
                                "CaseKey", "CaseKeyValue", "Diary", "DiaryDate", "DiaryUser", "DiaryTypeId", "Rating", "Json", "LastClosedStatus", "EntityAnalysisModelActivationRuleGuid", "QaSampleStatusId", "QaSampleDate",
                                "RiskLaneId", "RiskPriority", "RiskLikelihoodProbability", "RiskNextDeadlineDate", "RiskValue", "RiskSequence", "RiskCheckedDate", "ClosedReasonId", "ClosedReasonNote", "LegalHold", "LegalHoldReason",
                                "LegalHoldUser", "LegalHoldDate", "SimilarityCheckedDate")
                            SELECT c.guid, c.wf_guid, c.status_guid, c.created, c.locked, c.luser, CASE WHEN c.lock_h IS NOT NULL THEN v_now - c.lock_h * interval '1 hour' END, c.closed,
                                   CASE WHEN c.nulldates THEN NULL ELSE c.closed_at END, CASE WHEN c.nulldates OR c.closed_at IS NULL THEN NULL ELSE c.cuser END,
                                   'AccountId', c.key_value, c.diary, CASE WHEN c.diary_h IS NOT NULL THEN v_now + c.diary_h * interval '1 hour' END, CASE WHEN c.diary = 1 THEN 'Administrator' END, c.dtype, c.rating,
                                   CASE c.jsonmode WHEN 'empty' THEN '{}'::jsonb WHEN 'big' THEN jsonb_build_object('payload', jsonb_build_object('Padding', repeat('P', 1000000))) ELSE c.ajson END,
                                   CASE WHEN c.closed = 3 THEN 3 ELSE 0 END, c.rule_guid, c.qa, CASE WHEN c.qa = 2 THEN c.created + interval '1 hour' END,
                                   c.lane, c.prio, c.lik, CASE WHEN c.closed IN (0, 1) THEN c.created + interval '2880 minutes' END, c.rval, CASE WHEN c.pin AND c.lane IS NOT NULL THEN 1 END,
                                   CASE WHEN c.pin THEN v_now + interval '10 years' END, c.reason, c.note, c.hold, c.hreason, CASE WHEN c.hold = 1 AND c.hreason IS NOT NULL THEN 'Lena Fischer' END,
                                   CASE WHEN c.hold = 1 AND c.hreason IS NOT NULL AND c.hdate_h IS NOT NULL THEN v_now - c.hdate_h * interval '1 hour' END, CASE WHEN c.sim THEN v_now END
                            FROM smk_case c;

                            CREATE TEMP TABLE smk_k ON COMMIT DROP AS
                            SELECT c.*, k."Id" AS case_id FROM smk_case c JOIN "Case" k ON k."EntityAnalysisModelInstanceEntryGuid" = c.guid AND k."CaseWorkflowGuid" = c.wf_guid;

                            INSERT INTO "CaseEvent" ("CaseEventTypeId", "Before", "After", "CreatedUser", "CreatedDate", "CaseKey", "CaseKeyValue", "CaseId")
                            SELECT e.t,
                                   CASE WHEN e.t IN (9, 17) THEN v_flr::text WHEN e.t = 14 THEN 'Unassigned' END,
                                   CASE WHEN e.t IN (9, 17) THEN CASE WHEN k.status_guid = v_flr THEN v_sup::text ELSE k.status_guid::text END WHEN e.t = 14 THEN k.luser WHEN e.t = 5 THEN to_char(k.closed_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"') WHEN e.t = 32 THEN k.hreason END,
                                   CASE WHEN e.t = 5 THEN coalesce(k.cuser, 'Amara Okafor') WHEN e.t IN (3, 6, 14) THEN coalesce(k.luser, 'Administrator') WHEN e.t = 18 THEN 'Lena Fischer' WHEN e.t = 29 THEN 'Ask Jooby'
                                        WHEN e.t IN (17, 20, 28) THEN 'Automation' WHEN e.t = 26 THEN CASE WHEN k.code = 'CL-OVERRIDE-OWN' THEN 'Administrator' ELSE 'Lena Fischer' END WHEN e.t = 32 THEN 'Lena Fischer' WHEN e.t = 33 THEN 'Daniel Brooks'
                                        WHEN e.t IN (7, 9) AND k.code LIKE 'RT-%' THEN 'Tomasz Kowalski' WHEN e.t IN (4, 16, 2, 25, 27, 30, 31, 34, 35) THEN 'Administrator' WHEN e.t = 99 THEN 'System' ELSE 'Amara Okafor' END,
                                   CASE WHEN e.t = 5 AND k.closed_at IS NOT NULL THEN k.closed_at
                                        WHEN k.code LIKE 'RT-%' AND e.t IN (5, 7, 9, 17) THEN v_now - interval '3 hours'
                                        WHEN e.t = 32 AND k.hdate_h IS NOT NULL THEN v_now - k.hdate_h * interval '1 hour'
                                        WHEN e.t = 33 AND k.hdate_h IS NOT NULL THEN v_now - k.hdate_h * interval '1 hour' + interval '12 hours'
                                        WHEN e.t = 33 THEN k.created + interval '30 minutes'
                                        WHEN e.t IN (25, 26, 27, 28) THEN v_now - interval '30 minutes' - (e.ord * interval '1 minute')
                                        ELSE k.created + (e.ord::numeric / (e.cnt + 1)) * greatest(coalesce(k.closed_at, v_now) - k.created, interval '10 minutes') END,
                                   'AccountId', k.key_value, k.case_id
                            FROM smk_k k CROSS JOIN LATERAL (SELECT u.t::integer AS t, u.ord::integer AS ord, array_length(string_to_array(k.events, ','), 1) AS cnt FROM unnest(string_to_array(k.events, ',')) WITH ORDINALITY AS u(t, ord) WHERE k.events <> '') e;

                            CREATE TEMP TABLE smk_status_other ON COMMIT DROP AS SELECT k.case_id, CASE WHEN k.status_guid = v_sup THEN v_flr ELSE v_sup END AS other_guid FROM smk_k k;

                            INSERT INTO "CaseStatusTransition" ("CaseId", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "PreviousCaseWorkflowStatusGuid", "EnterReasonId", "EnteredDate", "EnteredUser", "ExitReasonId", "ExitedDate", "ExitedUser", "DurationSeconds")
                            SELECT k.case_id, k.wf_guid, x.status, x.previous, x.enter_reason, x.entered, x.entered_by, x.exit_reason, x.exited, x.exited_by, CASE WHEN x.exited IS NULL THEN NULL ELSE extract(epoch FROM (x.exited - x.entered))::integer END
                            FROM smk_k k JOIN smk_status_other o ON o.case_id = k.case_id
                            CROSS JOIN LATERAL (VALUES
                                ('normal', k.status_guid, NULL::uuid, 1, k.created, 'Automation', CASE WHEN k.closed = 3 AND k.closed_at IS NOT NULL THEN 2 END::integer, CASE WHEN k.closed = 3 THEN k.closed_at END, CASE WHEN k.closed = 3 THEN k.cuser END),
                                ('inverted', k.status_guid, NULL, 1, k.created, 'Automation', 1, k.created - interval '1 hour', 'Automation'),
                                ('reopen', k.status_guid, NULL, 1, k.created, 'Automation', 2, v_now - interval '8 hours', 'Amara Okafor'),
                                ('reopen', k.status_guid, k.status_guid, 3, v_now - interval '3 hours', 'Tomasz Kowalski', NULL, NULL, NULL),
                                ('escalated', o.other_guid, NULL, 1, k.created, 'Automation', 1, v_now - interval '3 hours', 'Automation'),
                                ('escalated', k.status_guid, o.other_guid, 4, v_now - interval '3 hours', 'Automation', NULL, NULL, NULL),
                                ('moved_other', o.other_guid, NULL, 1, k.created, 'Automation', 1, v_now - interval '3 hours', 'Tomasz Kowalski'),
                                ('moved_other', k.status_guid, o.other_guid, 2, v_now - interval '3 hours', 'Tomasz Kowalski', NULL, NULL, NULL)
                            ) AS x(mode, status, previous, enter_reason, entered, entered_by, exit_reason, exited, exited_by)
                            WHERE x.mode = k.trans;

                            INSERT INTO "CaseRiskAssessment" ("CaseId", "Sequence", "CreatedDate", "TriggerId", "MethodId", "MethodVersion", "BaseRate", "LikelihoodProbability", "LikelihoodBandId", "EvidenceCount", "ContributionsJson", "ImpactId", "ImpactComponentsJson", "LaneId", "LaneReason", "NextDeadlineDate", "DeadlineBreached", "ExpectedHarm", "Priority")
                            SELECT k.case_id, 1, least(k.created + interval '5 minutes', v_now), 1, 1, 1, 0.1, coalesce(k.lik, 0), CASE WHEN coalesce(k.lik, 0) >= 0.75 THEN 4 WHEN k.lik >= 0.5 THEN 3 WHEN k.lik >= 0.25 THEN 2 ELSE 1 END, 3,
                                   '[{"label": "Amount against the account history", "family": "Amount", "weight": 0.4}]'::jsonb, 2, '{}'::jsonb, k.lane, 'Seeded for smoke testing', k.created + interval '2880 minutes', 0, 1.2, coalesce(k.prio, 0)
                            FROM smk_k k WHERE k.pin AND k.assess AND k.lane IS NOT NULL;
                        END
                        $seed$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
