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

    [Migration(2026111794000003)]
    public class SeedSmokeTestQueueVolume : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_model integer;
                            v_model_guid uuid := '90c425fd-101a-420b-91d1-cb7a24a969cc';
                            v_wf3 uuid;
                            v_queue uuid;
                            v_done uuid;
                            v_now timestamp := date_trunc('minute', now() at time zone 'utc');
                            v_analysts text[] := ARRAY['Amara Okafor', 'Tomasz Kowalski', 'Priya Raman', 'Jonas Berg', 'Mei Lin', 'Carlos Mendes', 'Administrator'];
                        BEGIN
                            SELECT m."Id" INTO v_model FROM "EntityAnalysisModel" m WHERE m."Guid" = v_model_guid AND coalesce(m."Deleted", 0) = 0;
                            SELECT w."Guid" INTO v_wf3 FROM "CaseWorkflow" w WHERE w."Name" = 'Smoke: Queue Volume' AND coalesce(w."Deleted", 0) = 0;
                            SELECT s."Guid" INTO v_queue FROM "CaseWorkflowStatus" s JOIN "CaseWorkflow" w ON w."Id" = s."CaseWorkflowId" WHERE w."Guid" = v_wf3 AND s."Name" = 'Queue' AND coalesce(s."Deleted", 0) = 0;
                            SELECT s."Guid" INTO v_done FROM "CaseWorkflowStatus" s JOIN "CaseWorkflow" w ON w."Id" = s."CaseWorkflowId" WHERE w."Guid" = v_wf3 AND s."Name" = 'Resolved' AND coalesce(s."Deleted", 0) = 0;

                            IF v_model IS NULL OR v_wf3 IS NULL OR v_queue IS NULL OR v_done IS NULL THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "Case" WHERE "CaseWorkflowGuid" = v_wf3 LIMIT 1) THEN
                                RETURN;
                            END IF;

                            CREATE TEMP TABLE vol_centroid (country text, lat double precision, lon double precision) ON COMMIT DROP;
                            INSERT INTO vol_centroid VALUES ('GB', 51.5074, -0.1278), ('DE', 52.52, 13.405), ('FR', 48.8566, 2.3522), ('NL', 52.3676, 4.9041), ('ES', 40.4168, -3.7038), ('IT', 41.9028, 12.4964), ('PL', 52.2297, 21.0122), ('EG', 30.0444, 31.2357), ('TR', 41.0082, 28.9784), ('AE', 25.2048, 55.2708);

                            CREATE TEMP TABLE vol ON COMMIT DROP AS
                            SELECT n, gen_random_uuid() AS guid, 'SMK-VOL-' || lpad(n::text, 4, '0') AS kv,
                                   CASE WHEN n <= 480 THEN 'open' WHEN n <= 600 THEN 'closed' ELSE 'stuck' END AS kind,
                                   abs(hashtext('v' || n)) AS h,
                                   (ARRAY['GB','DE','FR','NL','ES','IT','PL','EG','TR','AE'])[1 + (abs(hashtext('vc' || n)) % 10)] AS country,
                                   (ARRAY['StructuringBroad','RapidVelocity','MuleRingFanOut','ImpossibleTravel','ThresholdSanctionsDistance','StructuringTuned'])[1 + (abs(hashtext('vr' || n)) % 6)] AS rule_name,
                                   50 + (abs(hashtext('va' || n)) % 9000) AS amount
                            FROM generate_series(1, 810) n;

                            ALTER TABLE vol ADD COLUMN created timestamp, ADD COLUMN closed_at timestamp, ADD COLUMN luser text, ADD COLUMN lock_at timestamp, ADD COLUMN lane integer, ADD COLUMN prio integer, ADD COLUMN lik double precision,
                                ADD COLUMN reason integer, ADD COLUMN diary integer, ADD COLUMN qa integer, ADD COLUMN rule_id integer, ADD COLUMN rule_guid uuid, ADD COLUMN ajson jsonb;

                            UPDATE vol v SET
                                created = v_now - (CASE v.kind WHEN 'open' THEN 1 + (v.h % 600) WHEN 'closed' THEN 30 + (v.h % 500) ELSE 20 + (v.h % 60) END) * interval '1 hour',
                                luser = CASE WHEN v.kind = 'stuck' THEN v_analysts[1 + (v.h % 6)] WHEN v.kind = 'open' AND v.h % 100 < 30 THEN v_analysts[1 + (v.h % 7)] END,
                                lane = CASE WHEN v.h % 100 < 5 THEN 1 WHEN v.h % 100 < 10 THEN 2 WHEN v.h % 100 < 20 THEN 3 WHEN v.h % 100 < 98 THEN 4 END,
                                prio = 5 + (v.h % 90), lik = round(((v.h % 1000) / 1000.0)::numeric, 3)::double precision,
                                reason = CASE WHEN v.kind = 'closed' THEN 1 + (v.h % 8) END,
                                diary = CASE WHEN v.kind = 'open' AND v.h % 100 >= 90 THEN 1 ELSE 0 END,
                                qa = CASE WHEN v.h % 100 < 4 THEN 2 WHEN v.kind = 'closed' THEN 1 ELSE 0 END;

                            UPDATE vol v SET lock_at = CASE WHEN v.kind = 'stuck' THEN v_now - (3 + (v.h % 55)) * interval '1 hour' WHEN v.luser IS NOT NULL THEN v_now - (0.1 + (v.h % 28)) * interval '1 hour' END,
                                closed_at = CASE WHEN v.kind = 'closed' THEN v.created + (2 + (v.h % 200)) * interval '1 hour' END;
                            UPDATE vol v SET created = least(v.created, coalesce(v.lock_at, v.created) - interval '30 minutes'), closed_at = CASE WHEN v.kind = 'closed' THEN least(v.closed_at, v_now - interval '10 minutes') END;
                            UPDATE vol v SET rule_id = (SELECT r."Id" FROM "EntityAnalysisModelActivationRule" r WHERE r."EntityAnalysisModelId" = v_model AND r."Name" = v.rule_name AND coalesce(r."Deleted", 0) = 0 ORDER BY r."Id" LIMIT 1);
                            UPDATE vol v SET rule_guid = (SELECT r."Guid" FROM "EntityAnalysisModelActivationRule" r WHERE r."Id" = v.rule_id);

                            UPDATE vol v SET ajson = jsonb_build_object(
                                'payload', jsonb_build_object('TxnId', 'SMK-TXN-VOL-' || v.n, 'TxnDateTime', to_char(v.created, 'YYYY-MM-DD"T"HH24:MI:SS'), 'AccountId', v.kv, 'CurrencyAmount', v.amount, 'AmountEUR', v.amount, 'Currency', 'EUR', 'BillingCountry', v.country,
                                    'ChannelId', (ARRAY['Card','ATM','Transfer'])[1 + (v.h % 3)], 'ResponseCode', '0',
                                    'AccountLatitude', round((m.lat + ((v.h % 100) - 50) / 100.0)::numeric, 4), 'AccountLongitude', round((m.lon + (((v.h / 7) % 100) - 50) / 100.0)::numeric, 4)),
                                'tag', '[]'::jsonb, 'activation', jsonb_build_object(v.rule_name, jsonb_build_object('visible', true, 'forced', false)), 'shadowActivation', '{}'::jsonb, 'abstraction', '{}'::jsonb, 'ttlCounter', '{}'::jsonb,
                                'responseElevation', jsonb_build_object('value', 1 + (v.h % 400) / 100.0, 'name', 'Level 2', 'createdDate', to_char(v.created, 'YYYY-MM-DD"T"HH24:MI:SS"Z"')),
                                'prevailingEntityAnalysisModelActivationRuleName', v.rule_name, 'entityAnalysisModelActivationRuleCount', 1, 'entityAnalysisModelGuid', v_model_guid::text,
                                'entityAnalysisModelName', 'Detailed Account Financial Transactions', 'entityAnalysisModelInstanceEntryGuid', v.guid::text,
                                'createdDate', to_char(v.created, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'), 'referenceDate', to_char(v.created, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'), 'createCase', true)
                            FROM vol_centroid m WHERE m.country = v.country;

                            INSERT INTO "Archive" ("Json", "EntityAnalysisModelInstanceEntryGuid", "EntryKeyValue", "ResponseElevation", "EntityAnalysisModelActivationRuleId", "EntityAnalysisModelId", "ActivationRuleCount", "CreatedDate", "ReferenceDate", "Version")
                            SELECT v.ajson, v.guid, 'SMK-TXN-VOL-' || v.n, 1 + (v.h % 400) / 100.0, v.rule_id, v_model, 1, v.created, v.created, 1 FROM vol v;
                            INSERT INTO "ArchiveKey" ("ProcessingTypeId", "Key", "KeyValueString", "KeyValueFloat", "EntityAnalysisModelInstanceEntryGuid", "Version")
                            SELECT 1, k.key, k.sv, k.fv, v.guid, 1 FROM vol v CROSS JOIN LATERAL (VALUES ('AccountId', v.kv, NULL::double precision), ('AmountEUR', NULL, v.amount::double precision), ('BillingCountry', v.country, NULL)) AS k(key, sv, fv);

                            INSERT INTO "Case" ("EntityAnalysisModelInstanceEntryGuid", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "CreatedDate", "Locked", "LockedUser", "LockedDate", "ClosedStatusId", "ClosedDate", "ClosedUser", "CaseKey", "CaseKeyValue", "Diary", "DiaryDate", "DiaryUser", "DiaryTypeId", "Rating",
                                "Json", "LastClosedStatus", "EntityAnalysisModelActivationRuleGuid", "QaSampleStatusId", "QaSampleDate", "RiskLaneId", "RiskPriority", "RiskLikelihoodProbability", "RiskNextDeadlineDate", "RiskValue", "RiskSequence", "RiskCheckedDate", "ClosedReasonId", "ClosedReasonNote", "LegalHold")
                            SELECT v.guid, v_wf3, CASE WHEN v.kind = 'closed' THEN v_done ELSE v_queue END, v.created, CASE WHEN v.luser IS NOT NULL THEN 1 ELSE 0 END, v.luser, v.lock_at, CASE WHEN v.kind = 'closed' THEN 3 ELSE 0 END, v.closed_at,
                                   CASE WHEN v.kind = 'closed' THEN v_analysts[1 + (v.h % 6)] END, 'AccountId', v.kv, v.diary, CASE WHEN v.diary = 1 THEN v_now + ((v.h % 96) - 24) * interval '1 hour' END, CASE WHEN v.diary = 1 THEN 'Administrator' END, 1 + (v.h % 3), 0,
                                   v.ajson, CASE WHEN v.kind = 'closed' THEN 3 ELSE 0 END, v.rule_guid, v.qa, CASE WHEN v.qa = 2 THEN v.created + interval '1 hour' END, v.lane, v.prio, v.lik, CASE WHEN v.kind <> 'closed' THEN v.created + interval '480 minutes' END,
                                   v.amount::double precision, CASE WHEN v.lane IS NOT NULL THEN 1 END, v_now + interval '10 years', v.reason, CASE WHEN v.kind = 'closed' THEN 'Closed from the volume queue.' END, 0
                            FROM vol v;

                            CREATE TEMP TABLE vol_k ON COMMIT DROP AS SELECT v.*, c."Id" AS case_id FROM vol v JOIN "Case" c ON c."EntityAnalysisModelInstanceEntryGuid" = v.guid AND c."CaseWorkflowGuid" = v_wf3;

                            INSERT INTO "CaseEvent" ("CaseEventTypeId", "Before", "After", "CreatedUser", "CreatedDate", "CaseKey", "CaseKeyValue", "CaseId")
                            SELECT e.t, NULL, CASE WHEN e.t = 5 THEN to_char(v.closed_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"') END, CASE WHEN e.t = 6 THEN v.luser WHEN e.t = 5 THEN v_analysts[1 + (v.h % 6)] ELSE coalesce(v.luser, 'Administrator') END, e.at, 'AccountId', v.kv, v.case_id
                            FROM vol_k v CROSS JOIN LATERAL (VALUES
                                (4, v.created + interval '20 minutes', v.kind <> 'open' OR v.h % 100 < 85),
                                (6, v.lock_at, v.luser IS NOT NULL),
                                (5, v.closed_at, v.kind = 'closed')) AS e(t, at, applies) WHERE e.applies AND e.at IS NOT NULL;

                            INSERT INTO "CaseStatusTransition" ("CaseId", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "PreviousCaseWorkflowStatusGuid", "EnterReasonId", "EnteredDate", "EnteredUser", "ExitReasonId", "ExitedDate", "ExitedUser", "DurationSeconds")
                            SELECT v.case_id, v_wf3, CASE WHEN v.kind = 'closed' THEN v_done ELSE v_queue END, NULL, 1, v.created, 'Automation', CASE WHEN v.kind = 'closed' THEN 2 END, v.closed_at, CASE WHEN v.kind = 'closed' THEN v_analysts[1 + (v.h % 6)] END,
                                   CASE WHEN v.kind = 'closed' THEN extract(epoch FROM (v.closed_at - v.created))::integer END
                            FROM vol_k v;

                            INSERT INTO "CaseRiskAssessment" ("CaseId", "Sequence", "CreatedDate", "TriggerId", "MethodId", "MethodVersion", "BaseRate", "LikelihoodProbability", "LikelihoodBandId", "EvidenceCount", "ContributionsJson", "ImpactId", "ImpactComponentsJson", "LaneId", "LaneReason", "NextDeadlineDate", "DeadlineBreached", "ExpectedHarm", "Priority")
                            SELECT v.case_id, 1, least(v.created + interval '5 minutes', v_now), 1, 1, 1, 0.1, v.lik, CASE WHEN v.lik >= 0.75 THEN 4 WHEN v.lik >= 0.5 THEN 3 WHEN v.lik >= 0.25 THEN 2 ELSE 1 END, 3,
                                   '[{"label": "Amount against the account history", "family": "Amount", "weight": 0.4}]'::jsonb, 2, '{}'::jsonb, v.lane, 'Seeded for smoke testing', v.created + interval '480 minutes', 0, 1.2, v.prio
                            FROM vol_k v WHERE v.lane IS NOT NULL;

                            INSERT INTO "CaseNote" ("Note", "ActionId", "PriorityId", "CreatedDate", "CaseKey", "CreatedUser", "CaseKeyValue", "CaseId", "SourceId", "KindId", "IsFinding", "Version")
                            SELECT 'Closed from the volume queue after review.', NULL, 3, v.closed_at - interval '5 minutes', 'AccountId', v_analysts[1 + (v.h % 6)], v.kv, v.case_id, 1, 1, false, 1 FROM vol_k v WHERE v.kind = 'closed' AND v.h % 100 < 90;
                        END
                        $seed$;
                        """);

            Execute.Sql("""
                        ANALYZE "Archive";
                        ANALYZE "ArchiveKey";
                        ANALYZE "Case";
                        ANALYZE "CaseEvent";
                        ANALYZE "CaseStatusTransition";
                        ANALYZE "CaseRiskAssessment";
                        ANALYZE "CaseNote";
                        ANALYZE "CaseChecklistItem";
                        """);
        }

        public override void Down()
        {
        }
    }
}
