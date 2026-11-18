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

    [Migration(2026111794000002)]
    public class SeedSmokeTestCaseDetail : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_model_guid uuid := '90c425fd-101a-420b-91d1-cb7a24a969cc';
                            v_tenant integer;
                            v_flr uuid;
                            v_sup uuid;
                            v_now timestamp := date_trunc('minute', now() at time zone 'utc');
                        BEGIN
                            SELECT "Id" INTO v_tenant FROM "TenantRegistry" WHERE "Id" = 1 AND coalesce("Deleted", 0) = 0;
                            SELECT s."Guid" INTO v_flr FROM "CaseWorkflowStatus" s JOIN "CaseWorkflow" w ON w."Id" = s."CaseWorkflowId" WHERE s."Name" = 'First Line Review' AND w."Name" NOT LIKE 'Smoke:%' AND coalesce(s."Deleted", 0) = 0 ORDER BY s."Id" LIMIT 1;
                            SELECT s."Guid" INTO v_sup FROM "CaseWorkflowStatus" s JOIN "CaseWorkflow" w ON w."Id" = s."CaseWorkflowId" WHERE s."Name" = 'Supervisor Review' AND w."Name" NOT LIKE 'Smoke:%' AND coalesce(s."Deleted", 0) = 0 ORDER BY s."Id" LIMIT 1;

                            IF v_tenant IS NULL OR v_flr IS NULL OR v_sup IS NULL OR NOT EXISTS (SELECT 1 FROM "Case" WHERE "CaseKeyValue" LIKE 'SMK-%') THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "CaseChecklistItem" i JOIN "Case" c ON c."Id" = i."CaseId" WHERE c."CaseKeyValue" LIKE 'SMK-CL-%' LIMIT 1) THEN
                                RETURN;
                            END IF;

                            CREATE TEMP TABLE smk_x ON COMMIT DROP AS
                            SELECT c."Id" AS case_id, c."CaseKeyValue" AS kv, substring(c."CaseKeyValue" from '^SMK-(.*?)(-[0-9][0-9])?$') AS code, c."CreatedDate" AS created, c."ClosedDate" AS closed_at,
                                   c."CaseWorkflowStatusGuid" AS status_guid, c."LockedUser" AS luser, c."CaseKey" AS case_key, c."CaseWorkflowGuid" AS wf_guid,
                                   row_number() OVER (PARTITION BY substring(c."CaseKeyValue" from '^SMK-(.*?)(-[0-9][0-9])?$') ORDER BY c."Id") AS seq,
                                   (SELECT r."Name" FROM "EntityAnalysisModelActivationRule" r WHERE r."Guid" = c."EntityAnalysisModelActivationRuleGuid") AS rule_name
                            FROM "Case" c WHERE c."CaseKeyValue" LIKE 'SMK-%';

                            CREATE TEMP TABLE smk_item ON COMMIT DROP AS
                            SELECT x.case_id, x.code, x.created, i."Guid" AS item_guid, cl."Guid" AS list_guid, cl."Name" AS list_name, cl."CaseWorkflowStatusGuid" AS status_guid, i."Name" AS item_name, i."Priority" AS prio, i."SlaMinutes" AS sla,
                                   row_number() OVER (PARTITION BY x.case_id ORDER BY i."Priority", i."Id") AS rn
                            FROM smk_x x JOIN "CaseWorkflowChecklist" cl ON cl."CaseWorkflowStatusGuid" = x.status_guid AND cl."Active" = 1 AND coalesce(cl."Deleted", 0) = 0
                            JOIN "CaseWorkflowChecklistItem" i ON i."CaseWorkflowChecklistGuid" = cl."Guid" AND i."Active" = 1 AND coalesce(i."Deleted", 0) = 0
                            WHERE x.code LIKE 'CL-%' AND x.code <> 'CL-NONE-DEFINED';

                            CREATE TEMP TABLE smk_item2 ON COMMIT DROP AS
                            SELECT s.*, CASE
                                       WHEN s.code IN ('CL-ALL-DONE', 'CL-ORPHAN-ITEM', 'CL-WRONG-STATUS-ITEM') THEN 3
                                       WHEN s.code = 'CL-PARTIAL' THEN CASE WHEN s.rn <= 2 THEN 3 ELSE 1 END
                                       WHEN s.code = 'CL-DONE-LATE' THEN CASE WHEN s.rn = 1 THEN 3 ELSE 1 END
                                       ELSE 1 END AS state_id,
                                   s.created + interval '5 minutes' AS shown
                            FROM smk_item s;

                            INSERT INTO "CaseChecklistItem" ("CaseId", "CaseWorkflowChecklistItemGuid", "CaseWorkflowChecklistGuid", "CaseWorkflowStatusGuid", "ChecklistName", "ItemName", "Priority", "StateId", "FirstShownDate", "ShownDate", "DueDate", "CompletedDate", "CompletedUser", "LastEvaluatedDate", "CreatedDate", "SlaBreachedDate")
                            SELECT s.case_id, s.item_guid, s.list_guid, s.status_guid, s.list_name, s.item_name, s.prio, s.state_id, s.shown, s.shown,
                                   CASE WHEN s.code = 'CL-NULL-DUE' OR s.sla IS NULL THEN NULL ELSE s.shown + s.sla * interval '1 minute' END,
                                   CASE WHEN s.state_id = 3 THEN CASE WHEN s.code = 'CL-DONE-LATE' AND s.sla IS NOT NULL THEN s.shown + (s.sla + 120) * interval '1 minute' ELSE s.shown + interval '20 minutes' END END,
                                   CASE WHEN s.state_id = 3 THEN 'Administrator' END, v_now, s.shown,
                                   CASE WHEN s.code IN ('CL-SLA-BREACHED', 'CL-DONE-LATE') AND s.sla IS NOT NULL AND s.shown + s.sla * interval '1 minute' < v_now THEN s.shown + s.sla * interval '1 minute' END
                            FROM smk_item2 s;

                            INSERT INTO "CaseChecklistItem" ("CaseId", "CaseWorkflowChecklistItemGuid", "CaseWorkflowChecklistGuid", "CaseWorkflowStatusGuid", "ChecklistName", "ItemName", "Priority", "StateId", "FirstShownDate", "ShownDate", "DueDate", "LastEvaluatedDate", "CreatedDate")
                            SELECT x.case_id, gen_random_uuid(), gen_random_uuid(), CASE WHEN x.code = 'CL-WRONG-STATUS-ITEM' THEN v_sup ELSE x.status_guid END, 'Initial Triage', 'An item with no definition', 99, 1,
                                   x.created + interval '5 minutes', x.created + interval '5 minutes', x.created + interval '2 hours', v_now, x.created + interval '5 minutes'
                            FROM smk_x x WHERE x.code IN ('CL-ORPHAN-ITEM', 'CL-WRONG-STATUS-ITEM');

                            INSERT INTO "CaseChecklistEvent" ("CaseId", "CaseChecklistItemId", "CaseWorkflowChecklistItemGuid", "ActionId", "Actor", "CreatedDate", "Detail")
                            SELECT i."CaseId", i."Id", i."CaseWorkflowChecklistItemGuid", e.action, CASE WHEN e.action = 3 THEN 'Administrator' ELSE 'Automation' END, e.at, i."ItemName"
                            FROM "CaseChecklistItem" i JOIN smk_x x ON x.case_id = i."CaseId"
                            CROSS JOIN LATERAL (VALUES (1, i."ShownDate", true), (3, i."CompletedDate", i."StateId" = 3), (9, i."SlaBreachedDate", i."SlaBreachedDate" IS NOT NULL)) AS e(action, at, applies)
                            WHERE x.code LIKE 'CL-%' AND e.applies AND e.at IS NOT NULL;

                            INSERT INTO "CaseChecklistOverride" ("CaseId", "CaseWorkflowStatusGuid", "Reason", "OpenItemsJson", "GrantedUser", "GrantedDate", "ConsumedDate", "ConsumedUser")
                            SELECT x.case_id, CASE WHEN x.code = 'CL-OVERRIDE-WRONG-STATUS' THEN v_sup ELSE x.status_guid END, 'Customer contact is not possible; supervisor accepts the risk.',
                                   '["Check related cases on the case key", "Contact the customer", "Record an investigation note", "Review the triggering transactions"]',
                                   CASE WHEN x.code = 'CL-OVERRIDE-OWN' THEN 'Administrator' ELSE 'Lena Fischer' END,
                                   CASE WHEN x.code = 'CL-OVERRIDE-STALE' THEN v_now - interval '8 hours' ELSE v_now - interval '2 hours' END,
                                   CASE WHEN x.code = 'CL-OVERRIDE-CONSUMED' THEN v_now - interval '1 hour' END, CASE WHEN x.code = 'CL-OVERRIDE-CONSUMED' THEN 'Administrator' END
                            FROM smk_x x WHERE x.code IN ('CL-OVERRIDE-LIVE', 'CL-OVERRIDE-OWN', 'CL-OVERRIDE-STALE', 'CL-OVERRIDE-CONSUMED', 'CL-OVERRIDE-WRONG-STATUS');

                            INSERT INTO "CaseServiceLevelBreach" ("CaseId", "DimensionId", "BreachedDate")
                            SELECT x.case_id, b.dim, x.created + b.minutes * interval '1 minute'
                            FROM smk_x x JOIN (VALUES ('SL-BREACHED', 3, 2880), ('SL-FIRST-VIEW-BREACH', 1, 240), ('SL-OWNERSHIP-BREACH', 2, 480), ('SL-DWELL-BREACH', 4, 1440)) AS b(code, dim, minutes) ON b.code = x.code;

                            INSERT INTO "CaseRiskAssessment" ("CaseId", "Sequence", "CreatedDate", "TriggerId", "MethodId", "MethodVersion", "BaseRate", "LikelihoodProbability", "LikelihoodBandId", "EvidenceCount", "ContributionsJson", "ImpactId", "ImpactComponentsJson", "LaneId", "LaneReason", "NextDeadlineDate", "DeadlineBreached", "ExpectedHarm", "Priority")
                            SELECT x.case_id, r.seq, v_now - r.hours * interval '1 hour', 3, 1, 1, 0.1, r.lik, 3, 4, '[{"label": "Linked key changed", "family": "Network", "weight": 0.5}]'::jsonb, 3, '{}'::jsonb, 3, 'Priority rose', x.created + interval '2880 minutes', 0, 2.0, r.prio
                            FROM smk_x x CROSS JOIN (VALUES (2, 4, 0.55, 35), (3, 2, 0.8, 70)) AS r(seq, hours, lik, prio) WHERE x.code = 'RK-RISING';
                            UPDATE "Case" c SET "RiskSequence" = 3, "RiskPriority" = 70, "RiskLikelihoodProbability" = 0.8 FROM smk_x x WHERE x.case_id = c."Id" AND x.code = 'RK-RISING';

                            INSERT INTO "CaseView" ("CaseId", "User", "LastViewedDate", "LastViewedCaseEventId", "LastViewedRiskSequence")
                            SELECT x.case_id, 'Administrator', v_now - interval '6 hours', 0, 1 FROM smk_x x
                            WHERE x.code IN ('RT-REOPENED', 'RT-ESCALATED', 'RT-MOVED-BY-OTHER', 'RT-NEW-EVENT', 'RT-DIARY-DUE', 'RK-RISING');

                            INSERT INTO "CaseAgentRun" ("Guid", "TenantRegistryId", "CaseId", "RecommendedOutcomeId", "Rationale", "Evidence", "FactsHash", "ModelName", "AgentDefinitionHash", "StateId", "OverrideReason", "DecidedUser", "DecidedDate", "CreatedDate")
                            SELECT gen_random_uuid(), v_tenant, x.case_id, t.outcome,
                                   CASE WHEN x.code = 'TR-LONG-EVIDENCE' THEN repeat('The activity pattern is consistent with structuring across several accounts and merchants. ', 20) ELSE 'Recommendation seeded for smoke testing: ' || t.label END,
                                   CASE WHEN x.code = 'TR-LONG-EVIDENCE' THEN (SELECT jsonb_agg('Evidence item ' || n || ' ' || repeat('e', 280))::text FROM generate_series(1, 10) n) ELSE '["Amount is above the account median", "Counterparty seen once before"]' END,
                                   'smoke-' || x.case_id, 'smoke-seed', 'smoke-definition', t.state, CASE WHEN t.state = 3 THEN 'The customer provided documentary evidence.' END,
                                   CASE WHEN t.state IN (2, 3) THEN 'Administrator' END, CASE WHEN t.state IN (2, 3) THEN v_now - interval '1 hour' END, v_now - interval '4 hours'
                            FROM smk_x x JOIN (VALUES ('TR-PENDING', 1, 1, 'confirmed pending'), ('TR-ACCEPTED', 2, 2, 'not confirmed accepted'), ('TR-OVERRIDDEN', 1, 3, 'confirmed overridden'), ('TR-SUPERSEDED', 2, 4, 'superseded'),
                                ('TR-INCONCLUSIVE', 0, 1, 'inconclusive'), ('TR-NOT-CONFIRMED', 2, 1, 'not confirmed pending'), ('TR-ON-CLOSED', 2, 1, 'pending on a closed case'), ('TR-LONG-EVIDENCE', 1, 1, 'long evidence'),
                                ('QA-REVIEWED-AGENT', 2, 2, 'accepted for review')) AS t(code, outcome, state, label) ON t.code = x.code;

                            INSERT INTO "CaseQaReview" ("Guid", "TenantRegistryId", "CaseId", "ReviewerUser", "AnalystVerdictId", "AgentVerdictId", "CaseAgentRunGuid", "Comment", "CreatedDate")
                            SELECT gen_random_uuid(), v_tenant, x.case_id, r.reviewer, r.verdict, CASE WHEN x.code = 'QA-REVIEWED-AGENT' THEN 1 END,
                                   CASE WHEN x.code = 'QA-REVIEWED-AGENT' THEN (SELECT ar."Guid" FROM "CaseAgentRun" ar WHERE ar."CaseId" = x.case_id ORDER BY ar."Id" LIMIT 1) END, r.comment, v_now - r.hours * interval '1 hour'
                            FROM smk_x x CROSS JOIN (VALUES ('Tomasz Kowalski', 1, 'Decision was sound.', 20), ('Priya Raman', 2, 'Closure note is thin; evidence not referenced.', 10)) AS r(reviewer, verdict, comment, hours)
                            WHERE x.code = 'QA-REVIEWED' OR (x.code = 'QA-REVIEWED-AGENT' AND r.reviewer = 'Tomasz Kowalski');

                            CREATE TEMP TABLE smk_sup ON COMMIT DROP AS
                            SELECT x.case_id, x.kv, x.case_key, x.rule_name, x.created, s.*
                            FROM smk_x x JOIN (VALUES
                                ('SU-PENDING', 1, 20, 720, 'Amara Okafor', false, 0),
                                ('SU-APPROVED', 2, 40, 720, 'Amara Okafor', true, 0),
                                ('SU-REJECTED', 3, 40, 720, 'Amara Okafor', true, 0),
                                ('SU-RETRACTED', 4, 40, 720, 'Amara Okafor', false, 0),
                                ('SU-PENDING-LAPSED', 1, 360, 240, 'Amara Okafor', false, 0),
                                ('SU-PENDING-EXPIRED', 1, 120, -24, 'Amara Okafor', false, 0),
                                ('SU-OWN-REQUEST', 1, 20, 720, 'Administrator', false, 0),
                                ('SU-APPROVED-REMOVED', 2, 80, 720, 'Amara Okafor', true, 0),
                                ('SU-APPROVED-EXPIRED', 2, 900, -48, 'Amara Okafor', true, 0),
                                ('SU-FORCE-TARGET', 1, 20, 720, 'Amara Okafor', false, 1),
                                ('SU-NO-TARGETS', 2, 40, 720, 'Amara Okafor', true, -1)
                            ) AS s(code, state_id, created_h, expiry_h, maker, decided, kind) ON s.code = x.code;

                            INSERT INTO "CaseSuppression" ("CaseId", "CaseKey", "CaseKeyValue", "OverrideKey", "OverrideKeyValue", "PathId", "Reason", "ExpiryDate", "StateId", "SourceId", "Version", "CountInCase", "FirstSeenDate", "LastSeenDate",
                                "WiderCountStateId", "WiderTransactions", "WiderKeys", "WiderCapped", "CreatedDate", "CreatedUser", "UpdatedDate", "UpdatedUser", "DecidedDate", "DecidedUser")
                            SELECT p.case_id, p.case_key, p.kv, p.case_key, p.kv, 1, 'Recurring benign activity confirmed with the customer.', v_now + p.expiry_h * interval '1 hour', p.state_id, 1, 1, 3, p.created, p.created + interval '1 day', 1, 12, 1, 0,
                                   v_now - p.created_h * interval '1 hour', p.maker, CASE WHEN p.code = 'SU-PENDING-LAPSED' THEN NULL ELSE v_now - (p.created_h - 1) * interval '1 hour' END, CASE WHEN p.code = 'SU-PENDING-LAPSED' THEN NULL ELSE p.maker END,
                                   CASE WHEN p.decided THEN v_now - (p.created_h - 2) * interval '1 hour' END, CASE WHEN p.decided THEN 'Lena Fischer' END
                            FROM smk_sup p;

                            INSERT INTO "CaseSuppressionTarget" ("CaseSuppressionId", "EntityAnalysisModelGuid", "EntityAnalysisModelName", "RuleName", "OverrideKindId", "AppliedOverrideId", "AppliedDate")
                            SELECT s."Id", v_model_guid, 'Detailed Account Financial Transactions', p.rule_name, CASE WHEN p.kind = 1 THEN 1 ELSE 0 END, CASE WHEN p.code = 'SU-APPROVED-REMOVED' THEN 999999999 END, CASE WHEN p.code = 'SU-APPROVED-REMOVED' THEN v_now - interval '2 days' END
                            FROM smk_sup p JOIN "CaseSuppression" s ON s."CaseId" = p.case_id AND s."CreatedDate" = v_now - p.created_h * interval '1 hour' WHERE p.kind >= 0;

                            INSERT INTO "CaseSuppressionApproval" ("CaseSuppressionId", "SuppressionVersion", "DecisionId", "Note", "CreatedDate", "CreatedUser")
                            SELECT s."Id", 1, CASE s."StateId" WHEN 2 THEN 1 WHEN 3 THEN 2 ELSE 3 END, CASE s."StateId" WHEN 2 THEN 'Approved for the stated period.' WHEN 3 THEN 'Evidence of repetition is thin; not approved.' ELSE 'Withdrawn by the requester.' END,
                                   coalesce(s."DecidedDate", s."UpdatedDate"), coalesce(s."DecidedUser", s."UpdatedUser")
                            FROM "CaseSuppression" s JOIN smk_sup p ON p.case_id = s."CaseId" WHERE s."StateId" IN (2, 3, 4);

                            INSERT INTO "CaseNote" ("Note", "ActionId", "PriorityId", "CreatedDate", "CaseKey", "CreatedUser", "CaseKeyValue", "CaseId", "SourceId", "KindId", "IsFinding", "Version")
                            SELECT n.note, n.action, 3, v_now - n.hours * interval '1 hour', x.case_key, n.by_user, x.kv, x.case_id, n.source, n.kind, n.finding, n.version
                            FROM smk_x x JOIN LATERAL (
                                SELECT 'Note ' || g || ': reviewed the transactions and spoke to the customer.' AS note, 1 AS action, 'Amara Okafor' AS by_user, g * 2 AS hours, 1 AS source, 1 AS kind, false AS finding, 1 AS version FROM generate_series(1, 30) g WHERE x.code = 'NT-MANY'
                                UNION ALL SELECT repeat('L', 100000), 1, 'Amara Okafor', 3, 1, 1, false, 1 WHERE x.code = 'NT-LONG'
                                UNION ALL SELECT t, 1, 'Amara Okafor', h, 1, 1, false, 1 FROM (VALUES ('ملاحظة: العميل أكد النشاط وهو متسق مع النمط المعتاد للحساب.', 5), ('הערה: הלקוח אישר את הפעילות.', 4), ('Payment flagged 🚨💳 then cleared ✅', 3), ('zero' || chr(8203) || 'width' || chr(8203) || 'joiners and e' || chr(769) || ' combining', 2)) AS u(t, h) WHERE x.code = 'NT-UNICODE'
                                UNION ALL SELECT 'Narrative version ' || g, 1, 'Amara Okafor', 10 - g, 1, 2, false, g FROM generate_series(1, 3) g WHERE x.code = 'NT-NARRATIVE'
                                UNION ALL SELECT 'Document: customer statement received and filed.', 1, 'Amara Okafor', 4, 1, 4, false, 1 WHERE x.code = 'NT-DOCUMENT'
                                UNION ALL SELECT 'Finding: the account received structured deposits from unrelated parties.', 1, 'Amara Okafor', 4, 1, 1, true, 1 WHERE x.code = 'NT-FINDING'
                                UNION ALL SELECT '<b>Bold</b> text <script>alert(1)</script><img src=x onerror=alert(1)> end', 1, 'Amara Okafor', 4, 1, 1, false, 1 WHERE x.code = 'NT-HTML'
                                UNION ALL SELECT 'Written by Ask Jooby after reviewing the journal.', 1, 'Ask Jooby', 4, 3, 1, false, 1 WHERE x.code = 'NT-AGENT-SYSTEM'
                                UNION ALL SELECT 'Raised by the system when the case opened.', 1, 'System', 5, 2, 1, false, 1 WHERE x.code = 'NT-AGENT-SYSTEM'
                                UNION ALL SELECT 'A note with no action recorded.', NULL, 'Amara Okafor', 4, 1, 1, false, 1 WHERE x.code = 'NT-NO-ACTION'
                            ) n ON true;

                            UPDATE "CaseNote" n2 SET "SupersedesNoteId" = prev."Id" FROM "CaseNote" prev, smk_x x
                            WHERE x.code = 'NT-NARRATIVE' AND n2."CaseId" = x.case_id AND prev."CaseId" = x.case_id AND prev."KindId" = 2 AND n2."KindId" = 2 AND n2."Version" = prev."Version" + 1;

                            INSERT INTO "CaseNote" ("Note", "ActionId", "PriorityId", "CreatedDate", "CaseKey", "CreatedUser", "CaseKeyValue", "CaseId", "SourceId", "KindId", "IsFinding", "Version")
                            VALUES ('A note with no case id, reachable only through the case key.', 1, 3, v_now - interval '5 hours', 'AccountId', 'Amara Okafor', 'SMK-ED-ORPHAN-NOTE', NULL, 1, 1, false, 1);

                            INSERT INTO "CaseTag" ("CaseId", "Name", "Version", "CreatedUser", "CreatedDate", "Deleted")
                            SELECT x.case_id, t.name, 1, 'Amara Okafor', v_now - interval '3 hours', t.deleted
                            FROM smk_x x JOIN LATERAL (
                                SELECT n AS name, 0 AS deleted FROM unnest(ARRAY['Fraud', 'ConfirmedFraud', 'FalsePositive', 'CustomerVerified', 'MoneyMule']) n WHERE x.code = 'TG-VOCABULARY'
                                UNION ALL SELECT 'Fraud', 0 WHERE x.code = 'TG-DELETED'
                                UNION ALL SELECT 'MoneyMule', 1 WHERE x.code = 'TG-DELETED'
                                UNION ALL SELECT n, 0 FROM unnest(ARRAY['نشاط مشبوه 🚩', 'Fraud', 'Fraud']) n WHERE x.code = 'TG-UNICODE-DUPLICATE'
                                UNION ALL SELECT 'Smoke Tag ' || lpad(g::text, 2, '0'), 0 FROM generate_series(1, 50) g WHERE x.code = 'TG-FIFTY'
                            ) t ON true;

                            INSERT INTO "CaseLink" ("CaseIdLow", "CaseIdHigh", "Reason", "CreatedUser", "CreatedDate")
                            SELECT least(p.a, p.b), greatest(p.a, p.b), 'Same device observed on both accounts.', 'Amara Okafor', v_now - interval '4 hours'
                            FROM (SELECT (SELECT case_id FROM smk_x WHERE code = 'LK-PAIR' AND seq = 1) AS a, (SELECT case_id FROM smk_x WHERE code = 'LK-PAIR' AND seq = 2) AS b) p;
                            INSERT INTO "CaseLink" ("CaseIdLow", "CaseIdHigh", "Reason", "CreatedUser", "CreatedDate")
                            SELECT least(h.case_id, g.case_id), greatest(h.case_id, g.case_id), 'Linked to a case in a workflow the viewer cannot see.', 'Amara Okafor', v_now - interval '4 hours'
                            FROM smk_x h, smk_x g WHERE h.code = 'LK-HIDDEN' AND g.code = 'RV-GATED-WORKFLOW' AND g.seq = 1;
                            INSERT INTO "CaseLink" ("CaseIdLow", "CaseIdHigh", "Reason", "CreatedUser", "CreatedDate")
                            SELECT x.case_id, 2147483000, 'Linked to a case that no longer exists.', 'Amara Okafor', v_now - interval '4 hours' FROM smk_x x WHERE x.code = 'LK-ORPHAN';
                            INSERT INTO "CaseLink" ("CaseIdLow", "CaseIdHigh", "Reason", "CreatedUser", "CreatedDate")
                            SELECT greatest(x.case_id, f.case_id), least(x.case_id, f.case_id), 'Stored with the higher id first.', 'Amara Okafor', v_now - interval '4 hours'
                            FROM smk_x x, smk_x f WHERE x.code = 'LK-REVERSED' AND f.code = 'OPEN-FRESH' AND f.seq = 1;
                            INSERT INTO "CaseLink" ("CaseIdLow", "CaseIdHigh", "Reason", "CreatedUser", "CreatedDate")
                            SELECT x.case_id, x.case_id, 'A case linked to itself.', 'Amara Okafor', v_now - interval '4 hours' FROM smk_x x WHERE x.code = 'LK-SELF';

                            INSERT INTO "CaseAnalystAssessment" ("Guid", "TenantRegistryId", "CaseId", "LikelihoodId", "Reason", "ComputedSequence", "ComputedLikelihoodProbability", "CreatedUser", "CreatedDate", "WithdrawnUser", "WithdrawnDate")
                            SELECT gen_random_uuid(), v_tenant, x.case_id, g.lik, 'Assessment ' || g.n || ': pattern reviewed against the key history.', 1, 0.3, 'Administrator', v_now - (30 - g.n) * interval '1 hour', CASE WHEN g.withdrawn THEN 'Administrator' END, CASE WHEN g.withdrawn THEN v_now - (29 - g.n) * interval '1 hour' END
                            FROM smk_x x JOIN LATERAL (
                                SELECT 1 AS n, 3 AS lik, false AS withdrawn WHERE x.code = 'AS-LOW-MEDIUM-HIGH'
                                UNION ALL SELECT 1, 2, true WHERE x.code = 'AS-WITHDRAWN'
                                UNION ALL SELECT n, 1 + (n % 3), n < 21 FROM generate_series(1, 21) n WHERE x.code = 'AS-HISTORY-21'
                            ) g ON true;

                            INSERT INTO "CaseReportProfile" ("TenantRegistryId", "Name", "Version", "Description", "TemplateXml", "SchemaDocumentsJson", "CodeListsJson", "CreatedUser", "CreatedDate")
                            SELECT v_tenant, 'Smoke SAR', 1, 'Minimal profile so the seeded reports resolve a profile name.', '<report/>', '[]', '{}', 'Administrator', v_now
                            WHERE NOT EXISTS (SELECT 1 FROM "CaseReportProfile" WHERE "Name" = 'Smoke SAR');
                            INSERT INTO "CaseReport" ("Guid", "TenantRegistryId", "CaseId", "CaseKey", "CaseKeyValue", "ProfileName", "ProfileVersion", "ReportCode", "StateId", "CurrentVersion", "FiuReference", "CreatedUser", "CreatedDate", "UpdatedUser", "UpdatedDate", "SubmittedUser", "SubmittedDate")
                            SELECT gen_random_uuid(), v_tenant, x.case_id, x.case_key, x.kv, 'Smoke SAR', 1, 'SAR', r.state, 1, CASE WHEN r.state = 6 THEN 'FIU-SMOKE-0001' END, 'Amara Okafor', v_now - interval '10 hours', 'Amara Okafor', v_now - interval '8 hours',
                                   CASE WHEN r.state >= 5 AND r.state <> 7 AND r.state <> 8 THEN 'Lena Fischer' END, CASE WHEN r.state >= 5 AND r.state <> 7 AND r.state <> 8 THEN v_now - interval '6 hours' END
                            FROM smk_x x JOIN (VALUES ('RP-DRAFT', 1), ('RP-VALIDATED', 2), ('RP-PENDING-APPROVAL', 3), ('RP-APPROVED', 4), ('RP-SUBMITTED', 5), ('RP-ACKNOWLEDGED', 6), ('RP-REJECTED', 7), ('RP-WITHDRAWN', 8)) AS r(code, state) ON r.code = x.code;
                            INSERT INTO "CaseReportVersion" ("CaseReportId", "Version", "DataJson", "CreatedUser", "CreatedDate")
                            SELECT r."Id", 1, '{}', 'Amara Okafor', r."CreatedDate" FROM "CaseReport" r JOIN smk_x x ON x.case_id = r."CaseId" WHERE x.code LIKE 'RP-%';

                            INSERT INTO "CaseWorkflowFormEntry" ("Payload", "CreatedDate", "CreatedUser", "CaseKey", "CaseId", "CaseWorkflowFormId", "CaseKeyValue")
                            SELECT '{"Reason": "Customer contacted", "Outcome": "Satisfied"}', v_now - e.hours * interval '1 hour', 'Amara Okafor', x.case_key, CASE WHEN x.code = 'FM-ORPHAN' THEN NULL ELSE x.case_id END, 1, x.kv
                            FROM smk_x x JOIN LATERAL (SELECT g * 3 AS hours FROM generate_series(1, 3) g WHERE x.code = 'FM-ENTRIES' UNION ALL SELECT 4 WHERE x.code IN ('FM-NO-VALUES', 'FM-ORPHAN')) e ON true;
                            INSERT INTO "CaseWorkflowFormEntryValue" ("Name", "Value", "CaseWorkflowFormEntryId")
                            SELECT v.name, v.value, e."Id" FROM "CaseWorkflowFormEntry" e JOIN smk_x x ON x.kv = e."CaseKeyValue" AND x.code IN ('FM-ENTRIES', 'FM-ORPHAN')
                            CROSS JOIN (VALUES ('Reason', 'Customer contacted'), ('Outcome', 'Satisfied'), ('Notes', 'Smoke seeded value')) AS v(name, value);

                            INSERT INTO "ErasureRequest" ("Guid", "TenantRegistryId", "KeyName", "KeyValue", "Reason", "StateId", "RefusalReasonId", "ArchiveRowsFound", "ArchiveRowsRemoved", "GraphNodesRemoved", "GraphEdgesRemoved", "CasePayloadsNulled", "RequestedUser", "RequestedDate", "DecidedUser", "DecidedDate", "DecisionNote", "ExecutedDate")
                            SELECT gen_random_uuid(), v_tenant, 'AccountId', r.kv, 'Data subject request received ' || r.label, r.state, r.refusal, r.found, CASE WHEN r.state = 3 THEN r.found END, CASE WHEN r.state = 3 THEN 1 END, CASE WHEN r.state = 3 THEN 2 END, CASE WHEN r.state = 3 THEN 1 END,
                                   'Amara Okafor', v_now - r.days * interval '1 day', CASE WHEN r.state IN (2, 3, 4) THEN 'Daniel Brooks' END, CASE WHEN r.state IN (2, 3, 4) THEN v_now - (r.days - 1) * interval '1 day' END, CASE WHEN r.state = 4 THEN 'Retention obligation applies.' END, CASE WHEN r.state = 3 THEN v_now - (r.days - 2) * interval '1 day' END
                            FROM (VALUES ('SMK-ER-OLD', 'pending', 1, NULL::integer, 1, 6), ('SMK-ER-OLD-A', 'approved', 2, NULL, 1, 5), ('SMK-ER-OLD-B', 'executed', 3, NULL, 1, 20), ('SMK-ER-OLD-C', 'rejected', 4, NULL, 1, 9), ('SMK-ER-OLD-D', 'retracted', 5, NULL, 0, 8),
                                ('SMK-ER-HELD', 'refused for legal hold', 6, 1, 1, 4), ('SMK-ER-OPEN', 'refused for an open case', 6, 2, 1, 4), ('SMK-ER-RETAINED', 'refused inside retention', 6, 3, 1, 4), ('SMK-ER-UNKNOWN', 'refused nothing found', 6, 4, 0, 4)) AS r(kv, label, state, refusal, found, days);
                            INSERT INTO "PreservationSnapshot" ("Guid", "TenantRegistryId", "SnapshotSourceId", "Name", "Json", "ExportVersion", "ExportGuid", "EntityAnalysisModelCount", "Bytes", "InError", "ErrorStack", "CreatedUser", "CreatedDate", "Deleted")
                            SELECT gen_random_uuid(), v_tenant, 1, 'Smoke: failed snapshot', '{}', 1, gen_random_uuid(), 0, 0, 1, 'System.InvalidOperationException: seeded failure for smoke testing of the error display.', 'Administrator', v_now - interval '2 days', 0
                            WHERE NOT EXISTS (SELECT 1 FROM "PreservationSnapshot" WHERE "Name" = 'Smoke: failed snapshot');

                            INSERT INTO "CaseEvent" ("CaseEventTypeId", "Before", "After", "CreatedUser", "CreatedDate", "CaseKey", "CaseKeyValue", "CaseId")
                            SELECT (ARRAY[4, 7, 10, 11, 16, 21, 23, 24])[1 + (g % 8)], NULL, NULL, 'Amara Okafor', x.created + g * interval '5 minutes', x.case_key, x.kv, x.case_id
                            FROM smk_x x CROSS JOIN generate_series(1, 996) g WHERE x.code = 'ED-MANY-EVENTS';
                        END
                        $seed$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
