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

    [Migration(2026111790000002)]
    public class SeedExecutiveDemonstrationCases : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_model integer;
                            v_workflow uuid;
                            v_workflow_id integer;
                            v_first uuid;
                            v_super uuid;
                            v_fraud uuid;
                            v_reported uuid;
                            v_now timestamp := date_trunc('minute', now() at time zone 'utc');
                            v_analysts text[] := ARRAY['Amara Okafor', 'Tomasz Kowalski', 'Priya Raman', 'Jonas Berg', 'Mei Lin', 'Carlos Mendes'];
                            v_supervisors text[] := ARRAY['Lena Fischer', 'Daniel Brooks'];
                        BEGIN
                            SELECT m."Id" INTO v_model FROM "EntityAnalysisModel" m
                            WHERE m."Guid" = '90c425fd-101a-420b-91d1-cb7a24a969cc' AND coalesce(m."Deleted", 0) = 0;

                            IF v_model IS NULL THEN
                                RETURN;
                            END IF;

                            IF NOT EXISTS (SELECT 1 FROM "Archive" WHERE "EntryKeyValue" LIKE 'EXE-%' LIMIT 1) THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "Case" c JOIN "Archive" a ON a."EntityAnalysisModelInstanceEntryGuid" = c."EntityAnalysisModelInstanceEntryGuid" WHERE a."EntryKeyValue" LIKE 'EXE-%' LIMIT 1) THEN
                                RETURN;
                            END IF;

                            SELECT "Id", "Guid" INTO v_workflow_id, v_workflow FROM "CaseWorkflow"
                            WHERE "EntityAnalysisModelId" = v_model AND coalesce("Deleted", 0) = 0 ORDER BY "Id" LIMIT 1;

                            SELECT "Guid" INTO v_first FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_workflow_id AND "Name" = 'First Line Review' AND coalesce("Deleted", 0) = 0 LIMIT 1;
                            SELECT "Guid" INTO v_super FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_workflow_id AND "Name" = 'Supervisor Review' AND coalesce("Deleted", 0) = 0 LIMIT 1;
                            SELECT "Guid" INTO v_fraud FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_workflow_id AND "Name" = 'Fraudulent' AND coalesce("Deleted", 0) = 0 LIMIT 1;
                            SELECT "Guid" INTO v_reported FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_workflow_id AND "Name" = 'Reported' AND coalesce("Deleted", 0) = 0 LIMIT 1;

                            IF v_first IS NULL OR v_super IS NULL OR v_fraud IS NULL OR v_reported IS NULL THEN
                                RETURN;
                            END IF;

                            CREATE TEMP TABLE exe_c1 ON COMMIT DROP AS
                            SELECT row_number() OVER (ORDER BY a."CreatedDate", a."Id") AS cn,
                                   a."EntityAnalysisModelInstanceEntryGuid" AS guid,
                                   a."CreatedDate" + interval '2 minutes' AS opened,
                                   a."Json"->'payload'->>'AccountId' AS account,
                                   (a."Json"->'payload'->>'AmountEUR')::numeric AS amount,
                                   a."Json"->>'prevailingEntityAnalysisModelActivationRuleName' AS rule_name,
                                   r."Guid" AS rule_guid,
                                   (v_now::date - a."CreatedDate"::date) AS days_ago,
                                   13 - least(12, ((v_now::date - a."CreatedDate"::date) / 30) + 1) AS month_ix,
                                   a."ResponseElevation" AS elevation,
                                   a."Json" AS json
                            FROM "Archive" a
                            JOIN "EntityAnalysisModelActivationRule" r ON r."Id" = a."EntityAnalysisModelActivationRuleId"
                            WHERE a."EntryKeyValue" LIKE 'EXE-%' AND a."ActivationRuleCount" > 0 AND r."EnableCaseWorkflow" = 1;

                            CREATE TEMP TABLE exe_c2 ON COMMIT DROP AS
                            SELECT c.*,
                                   v_analysts[1 + (abs(hashtext('an' || c.cn)) % 6)] AS analyst,
                                   v_supervisors[1 + (abs(hashtext('sv' || c.cn)) % 2)] AS supervisor,
                                   (abs(hashtext('fp' || c.cn)) % 1000) / 1000.0 AS fp_roll,
                                   (abs(hashtext('ot' || c.cn)) % 1000) AS other_roll,
                                   (abs(hashtext('rs' || c.cn)) % 1000) / 1000.0 AS reason_roll,
                                   (abs(hashtext('es' || c.cn)) % 100) AS esc_roll,
                                   (abs(hashtext('rh' || c.cn)) % 1000) / 1000.0 AS res_roll,
                                   (abs(hashtext('lg' || c.cn)) % 100) AS long_roll,
                                   (abs(hashtext('fv' || c.cn)) % 200) AS fv_roll,
                                   (abs(hashtext('ow' || c.cn)) % 120) AS ow_roll,
                                   (abs(hashtext('sp' || c.cn)) % 100) AS split_roll,
                                   (abs(hashtext('qa' || c.cn)) % 100) AS qa_roll,
                                   (abs(hashtext('ev' || c.cn)) % 100) AS misc_roll
                            FROM exe_c1 c;

                            CREATE TEMP TABLE exe_c3 ON COMMIT DROP AS
                            SELECT c.*,
                                   CASE c.rule_name WHEN 'StructuringBroad' THEN 0.92 WHEN 'StructuringTuned' THEN 0.70 WHEN 'RapidVelocity' THEN 0.85 WHEN 'MuleRingFanOut' THEN 0.40
                                                    WHEN 'ImpossibleTravel' THEN 0.75 WHEN 'ThresholdSanctionsDistance' THEN 0.93 ELSE 0.88 END AS p_fp,
                                   (4 + (abs(hashtext('rb' || c.cn)) % 40)
                                    + CASE WHEN c.long_roll < 6 THEN 40 + (abs(hashtext('rl' || c.cn)) % 2200) ELSE 0 END)
                                   * CASE WHEN c.month_ix = 9 THEN 3.5 WHEN c.month_ix IN (8, 10) THEN 1.6 WHEN c.month_ix <= 6 THEN 1.25 ELSE 1.0 END AS resolution_hours,
                                   (c.esc_roll < 35) AS escalated
                            FROM exe_c2 c;

                            CREATE TEMP TABLE exe_c4 ON COMMIT DROP AS
                            SELECT c.*,
                                   c.opened + interval '1 hour' * c.resolution_hours AS planned_close,
                                   (c.opened + interval '1 hour' * c.resolution_hours) <= v_now AS is_closed,
                                   CASE WHEN c.other_roll < 70 THEN 'other'
                                        WHEN c.fp_roll < c.p_fp THEN 'fp'
                                        ELSE 'tp' END AS outcome_kind
                            FROM exe_c3 c;

                            CREATE TEMP TABLE exe_c5 ON COMMIT DROP AS
                            SELECT c.*,
                                   CASE c.outcome_kind
                                       WHEN 'other' THEN CASE WHEN c.other_roll < 25 THEN 3 WHEN c.other_roll < 45 THEN 4 WHEN c.other_roll < 60 THEN 7 ELSE 8 END
                                       WHEN 'fp' THEN CASE WHEN c.reason_roll < CASE WHEN c.rule_name = 'StructuringBroad' THEN 0.7 ELSE 0.3 END THEN 2 ELSE 1 END
                                       ELSE CASE WHEN c.reason_roll < 0.6 THEN 5 ELSE 6 END
                                   END AS reason_id
                            FROM exe_c4 c;

                            CREATE TEMP TABLE exe_c6 ON COMMIT DROP AS
                            SELECT c.*,
                                   CASE WHEN c.reason_id = 5 THEN v_reported WHEN c.reason_id = 6 THEN v_fraud
                                        WHEN c.escalated THEN v_super ELSE v_first END AS final_status,
                                   CASE WHEN c.is_closed THEN c.planned_close END AS closed_at,
                                   CASE WHEN c.is_closed THEN CASE WHEN c.escalated OR c.reason_id IN (5, 6) THEN c.supervisor ELSE c.analyst END END AS closer
                            FROM exe_c5 c;

                            INSERT INTO "Case" ("EntityAnalysisModelInstanceEntryGuid", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "CreatedDate", "Locked", "LockedUser", "LockedDate", "ClosedStatusId",
                                "ClosedDate", "ClosedUser", "CaseKey", "CaseKeyValue", "Diary", "DiaryDate", "DiaryUser", "DiaryTypeId", "Rating", "Json", "LastClosedStatus", "EntityAnalysisModelActivationRuleGuid",
                                "QaSampleStatusId", "QaSampleDate", "RiskLaneId", "RiskPriority", "RiskNextDeadlineDate", "ClosedReasonId", "ClosedReasonNote", "LegalHold")
                            SELECT c.guid, v_workflow,
                                   CASE WHEN c.is_closed THEN c.final_status WHEN c.escalated AND (v_now - c.opened) > interval '20 hours' THEN v_super ELSE v_first END,
                                   c.opened,
                                   CASE WHEN NOT c.is_closed AND c.misc_roll < 70 THEN 1 ELSE 0 END,
                                   CASE WHEN NOT c.is_closed AND c.misc_roll < 70 THEN c.analyst END,
                                   CASE WHEN NOT c.is_closed AND c.misc_roll < 70 THEN c.opened + interval '1 minute' * (c.fv_roll + c.ow_roll) END,
                                   CASE WHEN c.is_closed THEN 3 ELSE 0 END,
                                   c.closed_at, c.closer, 'AccountId', c.account,
                                   CASE WHEN NOT c.is_closed AND c.misc_roll >= 70 AND c.misc_roll < 80 THEN 1 ELSE 0 END,
                                   CASE WHEN NOT c.is_closed AND c.misc_roll >= 70 AND c.misc_roll < 80 THEN v_now + interval '1 day' * (1 + c.misc_roll % 9) END,
                                   CASE WHEN NOT c.is_closed AND c.misc_roll >= 70 AND c.misc_roll < 80 THEN c.analyst END,
                                   CASE WHEN NOT c.is_closed AND c.misc_roll >= 70 AND c.misc_roll < 80 THEN CASE WHEN c.misc_roll IN (70, 71, 72) THEN 4 ELSE 1 + (c.misc_roll % 3) END ELSE 1 END,
                                   c.misc_roll % 6, c.json, CASE WHEN c.is_closed THEN 3 ELSE 0 END, c.rule_guid,
                                   CASE WHEN c.is_closed AND c.qa_roll < 12 THEN 2 WHEN c.is_closed THEN 1 ELSE 0 END,
                                   CASE WHEN c.is_closed AND c.qa_roll < 12 THEN c.closed_at END,
                                   CASE WHEN c.elevation >= 3 THEN 1 WHEN c.rule_name = 'ThresholdSanctionsDistance' THEN 2 WHEN c.resolution_hours > 48 THEN 3 ELSE 4 END,
                                   10 + (c.misc_roll % 80),
                                   CASE WHEN NOT c.is_closed THEN c.opened + interval '2880 minutes' END,
                                   CASE WHEN c.is_closed THEN c.reason_id END,
                                   CASE WHEN c.is_closed AND c.reason_id = 8 THEN 'Closed after review; no further action recorded under a standard reason.' END,
                                   0
                            FROM exe_c6 c;

                            CREATE TEMP TABLE exe_c7 ON COMMIT DROP AS
                            SELECT c.*, k."Id" AS case_id
                            FROM exe_c6 c
                            JOIN "Case" k ON k."EntityAnalysisModelInstanceEntryGuid" = c.guid AND k."CaseWorkflowGuid" = v_workflow;

                            CREATE TEMP TABLE exe_c8 ON COMMIT DROP AS
                            SELECT c.*,
                                   c.opened + interval '1 minute' * (c.fv_roll * CASE WHEN c.month_ix = 9 THEN 3 ELSE 1 END + 5) AS t_first_view,
                                   c.opened + interval '1 minute' * ((c.fv_roll * CASE WHEN c.month_ix = 9 THEN 3 ELSE 1 END) + c.ow_roll + 10) AS t_own,
                                   c.opened + (COALESCE(c.closed_at, c.planned_close) - c.opened) * (0.35 + (c.split_roll % 25) / 100.0) AS t_escalate
                            FROM exe_c7 c;

                            INSERT INTO "CaseEvent" ("CaseEventTypeId", "Before", "After", "CreatedUser", "CreatedDate", "CaseKey", "CaseKeyValue", "CaseId")
                            SELECT e.type_id, e.before, e.after, e.by_user, e.at, 'AccountId', c.account, c.case_id
                            FROM exe_c8 c
                            CROSS JOIN LATERAL (
                                VALUES
                                    (4, NULL::text, NULL::text, c.analyst, c.t_first_view, true),
                                    (CASE WHEN c.misc_roll % 5 = 0 THEN 14 ELSE 6 END, CASE WHEN c.misc_roll % 5 = 0 THEN 'Unassigned' END, c.analyst, c.analyst, c.t_own, c.t_own <= v_now),
                                    (CASE WHEN c.t_escalate - c.opened > interval '1440 minutes' THEN 17 ELSE 9 END, v_first::text, v_super::text, CASE WHEN c.t_escalate - c.opened > interval '1440 minutes' THEN 'Automation' ELSE c.analyst END, c.t_escalate, c.escalated AND c.t_escalate <= v_now),
                                    (9, CASE WHEN c.escalated THEN v_super::text ELSE v_first::text END, c.final_status::text, c.closer, c.closed_at - interval '2 minutes', c.is_closed AND c.final_status <> CASE WHEN c.escalated THEN v_super ELSE v_first END),
                                    (5, NULL, to_char(c.closed_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'), c.closer, c.closed_at, c.is_closed),
                                    (20, NULL, NULL, 'Automation', c.closed_at + interval '1 minute', c.is_closed AND c.qa_roll < 12),
                                    (16, NULL, NULL, c.supervisor, c.t_first_view + interval '30 minutes', c.misc_roll % 10 < 3 AND c.t_first_view + interval '30 minutes' <= v_now),
                                    (18, c.analyst, c.supervisor, c.supervisor, c.t_own + interval '90 minutes', c.misc_roll = 5 AND c.t_own + interval '90 minutes' <= v_now),
                                    (26, NULL, NULL, c.analyst, c.closed_at - interval '30 minutes', c.is_closed AND c.qa_roll % 25 = 1),
                                    (27, NULL, NULL, c.supervisor, c.closed_at - interval '20 minutes', c.is_closed AND c.qa_roll % 25 = 1 AND c.cn % 2 = 0),
                                    (28, NULL, NULL, 'Automation', c.t_own + interval '6 hours', c.resolution_hours > 72 AND c.misc_roll % 7 = 0 AND c.t_own + interval '6 hours' <= v_now),
                                    (29, NULL, NULL, 'Ask Jooby', c.opened + interval '5 minutes', c.days_ago < 120),
                                    (CASE WHEN c.cn % 100 < 85 THEN 30 ELSE 31 END, NULL, NULL, c.analyst, c.t_first_view + interval '4 minutes', c.days_ago < 120 AND c.t_first_view + interval '4 minutes' <= v_now)
                            ) AS e(type_id, before, after, by_user, at, applies)
                            WHERE e.applies AND e.at IS NOT NULL;

                            INSERT INTO "CaseStatusTransition" ("CaseId", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "PreviousCaseWorkflowStatusGuid", "EnterReasonId", "EnteredDate", "EnteredUser",
                                "ExitReasonId", "ExitedDate", "ExitedUser", "DurationSeconds")
                            SELECT c.case_id, v_workflow, s.status, s.previous, s.enter_reason, s.entered, s.entered_by,
                                   CASE WHEN s.exited IS NULL THEN NULL WHEN s.is_last THEN 2 ELSE 1 END, s.exited, CASE WHEN s.exited IS NULL THEN NULL ELSE s.exited_by END,
                                   CASE WHEN s.exited IS NULL THEN NULL ELSE extract(epoch FROM (s.exited - s.entered))::integer END
                            FROM exe_c8 c
                            CROSS JOIN LATERAL (
                                SELECT * FROM (VALUES
                                    (1, v_first, NULL::uuid, 1, c.opened, 'Automation',
                                        CASE WHEN c.escalated THEN CASE WHEN c.t_escalate <= v_now THEN c.t_escalate END
                                             WHEN c.final_status <> v_first AND c.is_closed THEN c.closed_at - interval '2 minutes'
                                             WHEN c.is_closed THEN c.closed_at END,
                                        c.analyst,
                                        NOT c.escalated AND c.final_status = v_first),
                                    (2, v_super, v_first, 4, c.t_escalate, 'Automation',
                                        CASE WHEN c.final_status <> v_super AND c.is_closed THEN c.closed_at - interval '2 minutes' WHEN c.is_closed THEN c.closed_at END,
                                        c.supervisor,
                                        c.final_status = v_super),
                                    (3, c.final_status, CASE WHEN c.escalated THEN v_super ELSE v_first END, 2, c.closed_at - interval '2 minutes', c.closer, c.closed_at, c.closer, true)
                                ) AS x(seq, status, previous, enter_reason, entered, entered_by, exited, exited_by, is_last)
                                WHERE (x.seq = 1)
                                   OR (x.seq = 2 AND c.escalated AND c.t_escalate <= v_now)
                                   OR (x.seq = 3 AND c.is_closed AND c.final_status NOT IN (v_first, v_super))
                            ) s;

                            UPDATE "CaseStatusTransition" t SET "ExitReasonId" = 2
                            FROM exe_c8 c
                            WHERE t."CaseId" = c.case_id AND c.is_closed AND t."ExitedDate" = c.closed_at AND coalesce(t."ExitReasonId", 0) <> 2;

                            INSERT INTO "CaseNote" ("Note", "ActionId", "PriorityId", "CreatedDate", "CaseKey", "CreatedUser", "CaseKeyValue", "CaseId")
                            SELECT CASE c.reason_id
                                       WHEN 1 THEN 'Customer confirmed the activity; consistent with the established pattern of the account.'
                                       WHEN 2 THEN 'Threshold caught ordinary behaviour; recommend the rule is reviewed for this segment.'
                                       WHEN 3 THEN 'Duplicate of an existing case for the same account.'
                                       WHEN 4 THEN 'Evidence insufficient to establish suspicion after enquiries.'
                                       WHEN 5 THEN 'Suspicion confirmed; report prepared for the nominated officer.'
                                       WHEN 6 THEN 'Fraud confirmed and the payment was stopped before settlement.'
                                       WHEN 7 THEN 'Customer contacted and gave a satisfactory explanation.'
                                       ELSE 'Reviewed and closed.' END,
                                   NULL, 3, c.closed_at - interval '5 minutes', 'AccountId', c.closer, c.account, c.case_id
                            FROM exe_c8 c
                            WHERE c.is_closed AND c.misc_roll < 97;

                            INSERT INTO "ArchiveTag" ("EntityAnalysisModelInstanceEntryGuid", "Name", "Version", "CreatedDate", "CreatedUser", "Deleted")
                            SELECT c.guid, CASE WHEN c.reason_id IN (5, 6) THEN 'ConfirmedFraud' ELSE 'FalsePositive' END, 1, c.closed_at, c.closer, 0
                            FROM exe_c8 c
                            WHERE c.is_closed AND (c.reason_id IN (5, 6) OR c.qa_roll < 35);

                            UPDATE "Case" k SET "LegalHold" = 1, "LegalHoldReason" = 'Held pending regulatory enquiry.', "LegalHoldUser" = 'Lena Fischer', "LegalHoldDate" = k."CreatedDate" + interval '3 days'
                            WHERE k."Id" IN (SELECT c.case_id FROM exe_c8 c WHERE c.reason_id IN (5, 6) AND c.is_closed ORDER BY c.cn DESC LIMIT 6);

                            INSERT INTO "CaseEvent" ("CaseEventTypeId", "Before", "After", "CreatedUser", "CreatedDate", "CaseKey", "CaseKeyValue", "CaseId")
                            SELECT 32, NULL, 'Held pending regulatory enquiry.', 'Lena Fischer', k."LegalHoldDate", k."CaseKey", k."CaseKeyValue", k."Id"
                            FROM "Case" k WHERE k."LegalHold" = 1 AND k."Id" IN (SELECT case_id FROM exe_c8);

                            WITH lifted AS (
                                SELECT k."Id" FROM "Case" k WHERE k."LegalHold" = 1 AND k."Id" IN (SELECT case_id FROM exe_c8) ORDER BY k."Id" LIMIT 2
                            ), done AS (
                                UPDATE "Case" k SET "LegalHold" = 0 FROM lifted l WHERE k."Id" = l."Id" RETURNING k."Id", k."CaseKey", k."CaseKeyValue", k."LegalHoldDate"
                            )
                            INSERT INTO "CaseEvent" ("CaseEventTypeId", "Before", "After", "CreatedUser", "CreatedDate", "CaseKey", "CaseKeyValue", "CaseId")
                            SELECT 33, NULL, NULL, 'Daniel Brooks', d."LegalHoldDate" + interval '40 days', d."CaseKey", d."CaseKeyValue", d."Id" FROM done d;

                            INSERT INTO "CaseSuppression" ("CaseId", "CaseKey", "CaseKeyValue", "OverrideKey", "OverrideKeyValue", "PathId", "Reason", "ExpiryDate", "StateId", "SourceId", "Version",
                                "CountInCase", "FirstSeenDate", "LastSeenDate", "WiderCountStateId", "WiderTransactions", "WiderKeys", "WiderCapped", "CreatedDate", "CreatedUser", "UpdatedDate", "UpdatedUser",
                                "DecidedDate", "DecidedUser")
                            SELECT c.case_id, 'AccountId', c.account, 'AccountId', c.account, 1, 'Recurring benign activity confirmed with the customer.',
                                   c.opened + interval '1 day' * (20 + s.k * 2),
                                   CASE WHEN s.k % 10 < 6 THEN 2 WHEN s.k % 10 < 8 THEN 3 WHEN s.k % 10 = 8 THEN 1 ELSE 4 END,
                                   1, 1, 1 + (s.k % 5), c.opened, c.opened + interval '1 day', 1, 20 + s.k * 3, 1, 0,
                                   c.opened + interval '4 hours', c.analyst, c.opened + interval '6 hours', c.analyst,
                                   CASE WHEN s.k % 10 IN (8) THEN NULL ELSE c.opened + interval '1 day' + interval '1 hour' * (s.k % 20) END,
                                   CASE WHEN s.k % 10 IN (8) THEN NULL ELSE c.supervisor END
                            FROM (SELECT x.*, row_number() OVER (ORDER BY x.cn DESC) AS k FROM exe_c8 x WHERE x.is_closed AND x.reason_id IN (1, 2)) c0
                            JOIN exe_c8 c ON c.cn = c0.cn
                            CROSS JOIN LATERAL (SELECT c0.k::integer AS k) s
                            WHERE c0.k <= 30;

                            INSERT INTO "CaseSuppressionApproval" ("CaseSuppressionId", "SuppressionVersion", "DecisionId", "Note", "CreatedDate", "CreatedUser")
                            SELECT s."Id", 1, CASE s."StateId" WHEN 2 THEN 1 WHEN 3 THEN 2 ELSE 3 END, CASE s."StateId" WHEN 2 THEN 'Approved for the stated period.' WHEN 3 THEN 'Evidence of repetition is thin; not approved.' ELSE 'Withdrawn by the requester.' END,
                                   coalesce(s."DecidedDate", s."UpdatedDate"), coalesce(s."DecidedUser", s."UpdatedUser")
                            FROM "CaseSuppression" s
                            WHERE s."CaseId" IN (SELECT case_id FROM exe_c8) AND s."StateId" IN (2, 3, 4);
                        END
                        $seed$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
