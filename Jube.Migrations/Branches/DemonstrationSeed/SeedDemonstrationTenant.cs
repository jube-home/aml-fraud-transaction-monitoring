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

namespace Jube.Migrations.Branches.DemonstrationSeed
{
    using FluentMigrator;

    [Migration(2026111620000000)]
    public class SeedDemonstrationTenant : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE OR REPLACE FUNCTION pg_temp.seed_approval(p_tenant integer, p_kind integer, p_entity integer)
                        RETURNS void LANGUAGE plpgsql AS $f$
                        BEGIN
                            INSERT INTO "EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId", "EntityVersion",
                                "StateId", "CreatedDate", "CreatedUser", "Guid")
                            SELECT p_tenant, p_kind, p_entity, 1, 1, now() at time zone 'utc', 'Administrator', gen_random_uuid()
                            WHERE NOT EXISTS (SELECT 1 FROM "EntityApproval" a
                                              WHERE a."EntityApprovalKindId" = p_kind AND a."EntityId" = p_entity AND a."EntityVersion" = 1);
                        END
                        $f$;

                        CREATE OR REPLACE FUNCTION pg_temp.seed_list(p_tenant integer, p_model uuid, p_name text)
                        RETURNS integer LANGUAGE plpgsql AS $f$
                        DECLARE
                            v_id integer;
                        BEGIN
                            SELECT "Id" INTO v_id FROM "EntityAnalysisModelList"
                            WHERE "EntityAnalysisModelGuid" = p_model AND "Name" = p_name AND coalesce("Deleted", 0) = 0
                            ORDER BY "Id" LIMIT 1;

                            IF v_id IS NULL THEN
                                INSERT INTO "EntityAnalysisModelList" ("Name", "Active", "CreatedDate", "CreatedUser", "Version", "Guid",
                                    "EntityAnalysisModelGuid")
                                VALUES (p_name, 1, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid(), p_model)
                                RETURNING "Id" INTO v_id;

                                PERFORM pg_temp.seed_approval(p_tenant, 2, v_id);
                            END IF;

                            RETURN v_id;
                        END
                        $f$;

                        CREATE OR REPLACE FUNCTION pg_temp.seed_list_value(p_tenant integer, p_list integer, p_value text,
                            p_expiry timestamp, p_approved boolean)
                        RETURNS void LANGUAGE plpgsql AS $f$
                        DECLARE
                            v_id integer;
                        BEGIN
                            IF EXISTS (SELECT 1 FROM "EntityAnalysisModelListValue"
                                       WHERE "EntityAnalysisModelListId" = p_list AND "ListValue" = p_value AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "EntityAnalysisModelListValue" ("EntityAnalysisModelListId", "ListValue", "CreatedDate", "CreatedUser",
                                "Version", "Guid", "DeleteExpiryDate")
                            VALUES (p_list, p_value, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid(), p_expiry)
                            RETURNING "Id" INTO v_id;

                            IF p_approved THEN
                                PERFORM pg_temp.seed_approval(p_tenant, 3, v_id);
                            END IF;
                        END
                        $f$;

                        CREATE OR REPLACE FUNCTION pg_temp.seed_dictionary_value(p_tenant integer, p_dictionary integer, p_key text,
                            p_value double precision, p_approved boolean)
                        RETURNS void LANGUAGE plpgsql AS $f$
                        DECLARE
                            v_id integer;
                        BEGIN
                            IF EXISTS (SELECT 1 FROM "EntityAnalysisModelDictionaryKvp"
                                       WHERE "EntityAnalysisModelDictionaryId" = p_dictionary AND "KvpKey" = p_key AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "EntityAnalysisModelDictionaryKvp" ("EntityAnalysisModelDictionaryId", "KvpKey", "KvpValue", "Active",
                                "CreatedDate", "CreatedUser", "Version", "Guid")
                            VALUES (p_dictionary, p_key, p_value, 1, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid())
                            RETURNING "Id" INTO v_id;

                            IF p_approved THEN
                                PERFORM pg_temp.seed_approval(p_tenant, 5, v_id);
                            END IF;
                        END
                        $f$;

                        CREATE OR REPLACE FUNCTION pg_temp.seed_tag(p_tenant integer, p_model integer, p_name text)
                        RETURNS void LANGUAGE plpgsql AS $f$
                        DECLARE
                            v_id integer;
                        BEGIN
                            IF EXISTS (SELECT 1 FROM "EntityAnalysisModelTag"
                                       WHERE "EntityAnalysisModelId" = p_model AND "Name" = p_name AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "EntityAnalysisModelTag" ("EntityAnalysisModelId", "Name", "Active", "CreatedDate", "CreatedUser",
                                "Version", "Guid")
                            VALUES (p_model, p_name, 1, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid())
                            RETURNING "Id" INTO v_id;

                            PERFORM pg_temp.seed_approval(p_tenant, 17, v_id);
                        END
                        $f$;

                        CREATE OR REPLACE FUNCTION pg_temp.seed_case_note(p_case integer, p_note text, p_action integer, p_priority integer,
                            p_finding boolean, p_at timestamp)
                        RETURNS void LANGUAGE plpgsql AS $f$
                        BEGIN
                            INSERT INTO "CaseNote" ("Note", "ActionId", "PriorityId", "CreatedDate", "CaseKey", "CreatedUser", "CaseKeyValue",
                                "CaseId", "SourceId", "KindId", "IsFinding", "Version")
                            SELECT p_note, p_action, p_priority, p_at, c."CaseKey", 'Administrator', c."CaseKeyValue", c."Id", 1, 1, p_finding, 1
                            FROM "Case" c
                            WHERE c."Id" = p_case
                              AND NOT EXISTS (SELECT 1 FROM "CaseNote" n WHERE n."CaseId" = p_case AND n."Note" = p_note);
                        END
                        $f$;

                        CREATE OR REPLACE FUNCTION pg_temp.seed_travel_txn(p_model integer, p_txn text, p_at timestamp, p_account text,
                            p_to text, p_amount numeric, p_device text, p_city text, p_latitude double precision,
                            p_longitude double precision)
                        RETURNS void LANGUAGE plpgsql AS $f$
                        DECLARE
                            v_guid uuid := gen_random_uuid();
                            v_json jsonb;
                        BEGIN
                            IF EXISTS (SELECT 1 FROM "Archive" WHERE "EntityAnalysisModelId" = p_model AND "EntryKeyValue" = p_txn) THEN
                                RETURN;
                            END IF;

                            v_json := jsonb_build_object(
                                'payload', jsonb_build_object(
                                    'TxnId', p_txn,
                                    'TxnDateTime', to_char(p_at, 'YYYY-MM-DD"T"HH24:MI:SS'),
                                    'AccountId', p_account,
                                    'ToAccountId', p_to,
                                    'CurrencyAmount', p_amount,
                                    'AmountEUR', p_amount,
                                    'SettlementAmount', p_amount,
                                    'Currency', 'EUR',
                                    'AccountCurrency', 'EUR',
                                    'ResponseCode', '0',
                                    'ChannelId', 'Card',
                                    'FingerprintHash', p_device,
                                    'MAC', 'MAC-TRAVEL-' || p_city,
                                    'Email', 'm.sterling@example.com',
                                    'CreditCardHash', 'CARD-441A',
                                    'BillingFirstName', 'Marcus',
                                    'BillingLastName', 'Sterling',
                                    'BillingCity', p_city,
                                    'OS', 'Android',
                                    'Brand', 'Samsung',
                                    'Model', 'SM-A536',
                                    'AccountLatitude', p_latitude,
                                    'AccountLongitude', p_longitude),
                                'tag', '[]'::jsonb,
                                'activation', '{}'::jsonb,
                                'abstraction', '{}'::jsonb,
                                'ttlCounter', '{}'::jsonb,
                                'responseElevation', 0,
                                'createdDate', to_char(p_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'),
                                'referenceDate', to_char(p_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'),
                                'entityAnalysisModelInstanceEntryGuid', v_guid::text,
                                'entityAnalysisModelInstanceEntryName', p_txn,
                                'createCase', false);

                            INSERT INTO "Archive" ("Json", "EntityAnalysisModelInstanceEntryGuid", "EntryKeyValue", "ResponseElevation",
                                "EntityAnalysisModelId", "ActivationRuleCount", "CreatedDate", "ReferenceDate", "Version")
                            VALUES (v_json, v_guid, p_txn, 0, p_model, 0, p_at, p_at, 1);

                            INSERT INTO "ArchiveKey" ("ProcessingTypeId", "Key", "KeyValueString", "KeyValueFloat",
                                "EntityAnalysisModelInstanceEntryGuid", "Version")
                            SELECT 1, k."Key", k."Text", k."Number", v_guid, 1
                            FROM (VALUES
                                ('AccountId', p_account, NULL::double precision),
                                ('ToAccountId', p_to, NULL),
                                ('FingerprintHash', p_device, NULL),
                                ('MAC', 'MAC-TRAVEL-' || p_city, NULL),
                                ('Email', 'm.sterling@example.com', NULL),
                                ('CreditCardHash', 'CARD-441A', NULL),
                                ('AmountEUR', NULL, p_amount::double precision)) AS k("Key", "Text", "Number");
                        END
                        $f$;

                        DO $seed$
                        DECLARE
                            v_model_guid uuid := '90c425fd-101a-420b-91d1-cb7a24a969cc';
                            v_model integer;
                            v_tenant integer;
                            v_workflow uuid;
                            v_list integer;
                            v_dictionary integer;
                            v_case_441 integer;
                            v_case_778 integer;
                            v_case_915 integer;
                            v_call integer;
                            v_escalation integer;
                            v_anchor uuid;
                            v_workflow_name text;
                            v_status_first uuid;
                            v_status_supervisor uuid;
                            v_status_fraud uuid;
                            v_case_earlier integer;
                            v_now timestamp := now() at time zone 'utc';
                            v_base timestamp := date_trunc('minute', now() at time zone 'utc') - interval '3 days';
                        BEGIN
                            SELECT "Id", "TenantRegistryId" INTO v_model, v_tenant FROM "EntityAnalysisModel"
                            WHERE "Id" = 1 AND "Guid" = v_model_guid AND coalesce("Deleted", 0) = 0;

                            IF v_model IS NULL OR v_tenant IS NULL THEN
                                RETURN;
                            END IF;

                            v_list := pg_temp.seed_list(v_tenant, v_model_guid, 'IPDenyList');

                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, '192.0.2.10', NULL, true);
                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, '192.0.2.44', NULL, true);
                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, '198.51.100.23', NULL, true);
                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, '198.51.100.90', v_now + interval '90 days', true);
                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, '203.0.113.77', NULL, false);

                            v_list := pg_temp.seed_list(v_tenant, v_model_guid, 'WatchedAccounts');

                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, 'ACC-441', NULL, true);
                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, 'ACC-02', NULL, true);
                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, 'ACC-77', NULL, true);
                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, 'ACC-88', v_now + interval '30 days', true);
                            PERFORM pg_temp.seed_list_value(v_tenant, v_list, 'ACC-300', NULL, false);

                            SELECT "Id" INTO v_dictionary FROM "EntityAnalysisModelDictionary"
                            WHERE "EntityAnalysisModelGuid" = v_model_guid AND "Name" = 'VolumeThresholdByAccountId'
                              AND coalesce("Deleted", 0) = 0
                            ORDER BY "Id" LIMIT 1;

                            IF v_dictionary IS NOT NULL THEN
                                PERFORM pg_temp.seed_dictionary_value(v_tenant, v_dictionary, 'ACC-441', 5000, true);
                                PERFORM pg_temp.seed_dictionary_value(v_tenant, v_dictionary, 'ACC-02', 20000, true);
                                PERFORM pg_temp.seed_dictionary_value(v_tenant, v_dictionary, 'ACC-778', 2500, true);
                                PERFORM pg_temp.seed_dictionary_value(v_tenant, v_dictionary, 'ACC-915', 2500, true);
                                PERFORM pg_temp.seed_dictionary_value(v_tenant, v_dictionary, 'CASH-BRANCH-07', 50000, false);
                            END IF;

                            PERFORM pg_temp.seed_tag(v_tenant, v_model, 'ConfirmedFraud');
                            PERFORM pg_temp.seed_tag(v_tenant, v_model, 'FalsePositive');
                            PERFORM pg_temp.seed_tag(v_tenant, v_model, 'CustomerVerified');
                            PERFORM pg_temp.seed_tag(v_tenant, v_model, 'MoneyMule');

                            PERFORM pg_temp.seed_travel_txn(v_model, 'DEMO-TRV-01', v_base, 'ACC-441', 'MER-01', 42, 'DEV-8831A', 'Cairo', 30.0444, 31.2357);
                            PERFORM pg_temp.seed_travel_txn(v_model, 'DEMO-TRV-02', v_base + interval '3 hours', 'ACC-441', 'MER-03', 18, 'DEV-8831A', 'Giza', 30.0131, 31.2089);
                            PERFORM pg_temp.seed_travel_txn(v_model, 'DEMO-TRV-03', v_base + interval '9 hours', 'ACC-441', 'MER-02', 65, 'DEV-8831A', 'Alexandria', 31.2001, 29.9187);
                            PERFORM pg_temp.seed_travel_txn(v_model, 'DEMO-TRV-04', v_base + interval '1 day 2 hours', 'ACC-441', 'MER-04', 120, 'DEV-8831A', 'Cairo', 30.0444, 31.2357);
                            PERFORM pg_temp.seed_travel_txn(v_model, 'DEMO-TRV-05', v_base + interval '1 day 2 hours 40 minutes', 'ACC-441', 'MER-05', 980, 'DEV-LON-01', 'London', 51.5074, -0.1278);
                            PERFORM pg_temp.seed_travel_txn(v_model, 'DEMO-TRV-06', v_base + interval '1 day 3 hours 5 minutes', 'ACC-441', 'MER-05', 1450, 'DEV-LON-01', 'London', 51.5155, -0.0922);
                            PERFORM pg_temp.seed_travel_txn(v_model, 'DEMO-TRV-07', v_base + interval '1 day 9 hours', 'ACC-441', 'MER-01', 30, 'DEV-8831A', 'Cairo', 30.0626, 31.2497);
                            PERFORM pg_temp.seed_travel_txn(v_model, 'DEMO-TRV-08', v_base + interval '2 days 1 hour', 'ACC-441', 'MER-02', 2200, 'DEV-DXB-01', 'Dubai', 25.2048, 55.2708);
                            PERFORM pg_temp.seed_travel_txn(v_model, 'DEMO-TRV-09', v_base + interval '2 days 7 hours', 'ACC-441', 'MER-03', 55, 'DEV-8831A', 'Cairo', 30.0444, 31.2357);

                            SELECT "Guid", "Name" INTO v_workflow, v_workflow_name FROM "CaseWorkflow"
                            WHERE "EntityAnalysisModelId" = v_model AND coalesce("Deleted", 0) = 0 ORDER BY "Id" LIMIT 1;

                            IF v_workflow IS NULL THEN
                                RETURN;
                            END IF;

                            SELECT "Id" INTO v_case_441 FROM "Case" WHERE "CaseWorkflowGuid" = v_workflow AND "CaseKey" = 'AccountId' AND "CaseKeyValue" = 'ACC-441' ORDER BY "Id" LIMIT 1;
                            SELECT "Id" INTO v_case_778 FROM "Case" WHERE "CaseWorkflowGuid" = v_workflow AND "CaseKey" = 'AccountId' AND "CaseKeyValue" = 'ACC-778' ORDER BY "Id" LIMIT 1;
                            SELECT "Id" INTO v_case_915 FROM "Case" WHERE "CaseWorkflowGuid" = v_workflow AND "CaseKey" = 'AccountId' AND "CaseKeyValue" = 'ACC-915' ORDER BY "Id" LIMIT 1;

                            SELECT a."Id" INTO v_call FROM "CaseWorkflowAction" a JOIN "CaseWorkflow" w ON w."Id" = a."CaseWorkflowId"
                            WHERE w."Guid" = v_workflow AND a."Name" = 'Call Customer' AND coalesce(a."Deleted", 0) = 0 ORDER BY a."Id" LIMIT 1;
                            SELECT a."Id" INTO v_escalation FROM "CaseWorkflowAction" a JOIN "CaseWorkflow" w ON w."Id" = a."CaseWorkflowId"
                            WHERE w."Guid" = v_workflow AND a."Name" = 'Escalation' AND coalesce(a."Deleted", 0) = 0 ORDER BY a."Id" LIMIT 1;

                            IF v_case_441 IS NOT NULL THEN
                                PERFORM pg_temp.seed_case_note(v_case_441, 'Peer to peer transfer of 9,500 to ACC-02 within a minute of ACC-102 sending 9,800 to the same account from the same device (DEV-8831A).', NULL, 2, false, v_now - interval '2 days 6 hours');
                                PERFORM pg_temp.seed_case_note(v_case_441, 'Called the customer on the registered number. No answer, voicemail left asking for a call back.', v_call, 3, false, v_now - interval '2 days 4 hours');
                                PERFORM pg_temp.seed_case_note(v_case_441, 'Eleven cash deposits from branch seven in nine days, each just under 5,000, precede the transfer. The pattern is consistent with structuring.', NULL, 2, true, v_now - interval '1 day 20 hours');
                                PERFORM pg_temp.seed_case_note(v_case_441, 'Card present in London forty minutes after a purchase in Cairo. Escalated for supervisor review.', v_escalation, 1, false, v_now - interval '1 day 2 hours');

                                INSERT INTO "CaseTag" ("CaseId", "Name", "Version", "CreatedUser", "CreatedDate", "Deleted")
                                SELECT v_case_441, t."Name", 1, 'Administrator', v_now, 0
                                FROM (VALUES ('MoneyMule'), ('Fraud')) AS t("Name")
                                WHERE NOT EXISTS (SELECT 1 FROM "CaseTag" c WHERE c."CaseId" = v_case_441 AND c."Name" = t."Name" AND coalesce(c."Deleted", 0) = 0);

                                SELECT "EntityAnalysisModelInstanceEntryGuid" INTO v_anchor FROM "Archive"
                                WHERE "EntityAnalysisModelId" = v_model AND "EntryKeyValue" = 'DEMO-TRV-05' ORDER BY "Id" LIMIT 1;

                                INSERT INTO "CaseKeyJournalPin" ("CaseId", "EntityAnalysisModelInstanceEntryGuid", "Pinned", "CreatedUser", "CreatedDate",
                                    "UpdatedUser", "UpdatedDate")
                                SELECT v_case_441, g."Guid", 1, 'Administrator', v_now, 'Administrator', v_now
                                FROM (SELECT v_anchor AS "Guid"
                                      UNION ALL
                                      SELECT c."EntityAnalysisModelInstanceEntryGuid" FROM "Case" c WHERE c."Id" = v_case_441) AS g
                                WHERE g."Guid" IS NOT NULL
                                  AND NOT EXISTS (SELECT 1 FROM "CaseKeyJournalPin" p WHERE p."CaseId" = v_case_441 AND p."EntityAnalysisModelInstanceEntryGuid" = g."Guid");
                            END IF;

                            IF v_case_778 IS NOT NULL THEN
                                PERFORM pg_temp.seed_case_note(v_case_778, 'Shares device DEV-8831A and card CARD-7F2A with ACC-915, which was closed as fraudulent.', NULL, 2, false, v_now - interval '1 day 12 hours');
                                PERFORM pg_temp.seed_case_note(v_case_778, 'Customer confirmed the recent card purchases by telephone.', v_call, 4, false, v_now - interval '1 day 3 hours');
                            END IF;

                            IF v_case_915 IS NOT NULL THEN
                                PERFORM pg_temp.seed_case_note(v_case_915, 'Customer denies the two transfers to ACC-02. Account restricted and the loss reported.', NULL, 1, true, v_now - interval '8 days');

                                INSERT INTO "CaseTag" ("CaseId", "Name", "Version", "CreatedUser", "CreatedDate", "Deleted")
                                SELECT v_case_915, 'ConfirmedFraud', 1, 'Administrator', v_now, 0
                                WHERE NOT EXISTS (SELECT 1 FROM "CaseTag" c WHERE c."CaseId" = v_case_915 AND c."Name" = 'ConfirmedFraud' AND coalesce(c."Deleted", 0) = 0);
                            END IF;

                            INSERT INTO "CaseLink" ("CaseIdLow", "CaseIdHigh", "Reason", "CreatedUser", "CreatedDate")
                            SELECT least(l."A", l."B"), greatest(l."A", l."B"), l."Reason", 'Administrator', v_now
                            FROM (VALUES
                                (v_case_441, v_case_778, 'Both accounts were used from device DEV-8831A.'),
                                (v_case_778, v_case_915, 'Both accounts share card CARD-7F2A.')) AS l("A", "B", "Reason")
                            WHERE l."A" IS NOT NULL AND l."B" IS NOT NULL
                              AND NOT EXISTS (SELECT 1 FROM "CaseLink" c WHERE c."CaseIdLow" = least(l."A", l."B") AND c."CaseIdHigh" = greatest(l."A", l."B"));

                            SELECT s."Guid" INTO v_status_first FROM "CaseWorkflowStatus" s JOIN "CaseWorkflow" w ON w."Id" = s."CaseWorkflowId"
                            WHERE w."Guid" = v_workflow AND s."Name" = 'First Line Review' AND coalesce(s."Deleted", 0) = 0 ORDER BY s."Id" LIMIT 1;
                            SELECT s."Guid" INTO v_status_supervisor FROM "CaseWorkflowStatus" s JOIN "CaseWorkflow" w ON w."Id" = s."CaseWorkflowId"
                            WHERE w."Guid" = v_workflow AND s."Name" = 'Supervisor Review' AND coalesce(s."Deleted", 0) = 0 ORDER BY s."Id" LIMIT 1;
                            SELECT s."Guid" INTO v_status_fraud FROM "CaseWorkflowStatus" s JOIN "CaseWorkflow" w ON w."Id" = s."CaseWorkflowId"
                            WHERE w."Guid" = v_workflow AND s."Name" = 'Fraudulent' AND coalesce(s."Deleted", 0) = 0 ORDER BY s."Id" LIMIT 1;

                            IF v_status_first IS NULL OR v_status_supervisor IS NULL OR v_status_fraud IS NULL THEN
                                RETURN;
                            END IF;

                            IF v_case_441 IS NOT NULL THEN
                                UPDATE "Case" SET "Locked" = 1, "LockedUser" = 'Administrator', "LockedDate" = v_now
                                WHERE "Id" = v_case_441 AND coalesce("Locked", 0) = 0 AND coalesce("ClosedStatusId", 0) = 0;

                                INSERT INTO "CaseStatusTransition" ("CaseId", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "EnterReasonId",
                                    "EnteredDate", "EnteredUser")
                                SELECT c."Id", v_workflow, v_status_first, 1, c."CreatedDate", 'Administrator'
                                FROM "Case" c
                                WHERE c."Id" = v_case_441 AND NOT EXISTS (SELECT 1 FROM "CaseStatusTransition" s WHERE s."CaseId" = c."Id");

                                INSERT INTO "CaseAnalystAssessment" ("Guid", "TenantRegistryId", "CaseId", "LikelihoodId", "Reason", "CreatedUser", "CreatedDate")
                                SELECT gen_random_uuid(), v_tenant, v_case_441, 3,
                                       'Structured cash deposits followed by a transfer to an account that received from a second customer on the same device.',
                                       'Administrator', v_now - interval '1 day'
                                WHERE NOT EXISTS (SELECT 1 FROM "CaseAnalystAssessment" a WHERE a."CaseId" = v_case_441);

                                INSERT INTO "CaseResearch" ("CaseId", "CaseKey", "CaseKeyValue", "Topic", "Findings", "SourceId", "CreatedDate", "CreatedUser")
                                SELECT c."Id", c."CaseKey", c."CaseKeyValue", 'Recipient account ACC-02',
                                       'ACC-02 is registered to Alpha LLC and forwards what it receives to ACC-77 within the hour, in two transfers of 9,200 and 9,700.',
                                       1, v_now - interval '1 day 18 hours', 'Administrator'
                                FROM "Case" c
                                WHERE c."Id" = v_case_441 AND NOT EXISTS (SELECT 1 FROM "CaseResearch" r WHERE r."CaseId" = c."Id");

                                IF NOT EXISTS (SELECT 1 FROM "Case" c WHERE c."CaseWorkflowGuid" = v_workflow AND c."CaseKey" = 'AccountId'
                                               AND c."CaseKeyValue" = 'ACC-441' AND c."Id" <> v_case_441) THEN
                                    INSERT INTO "Case" ("EntityAnalysisModelInstanceEntryGuid", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "CreatedDate",
                                        "Locked", "ClosedStatusId", "ClosedDate", "ClosedUser", "CaseKey", "CaseKeyValue", "Diary", "DiaryDate", "Rating",
                                        "Json", "DiaryTypeId", "QaSampleStatusId", "LastClosedStatus")
                                    SELECT a."EntityAnalysisModelInstanceEntryGuid", v_workflow, v_status_first, v_now - interval '21 days', 0, 3,
                                           v_now - interval '20 days', 'Administrator', 'AccountId', 'ACC-441', 0, v_now, 0, a."Json", 1, 0, 3
                                    FROM "Archive" a
                                    WHERE a."EntityAnalysisModelId" = v_model AND a."EntryKeyValue" = 'DEMO-441-LOGIN-1'
                                    ORDER BY a."Id" LIMIT 1
                                    RETURNING "Id" INTO v_case_earlier;

                                    IF v_case_earlier IS NOT NULL THEN
                                        PERFORM pg_temp.seed_case_note(v_case_earlier, 'Login from a public wifi address shared with many customers. Customer confirmed the purchase, closed with no further action.', v_call, 4, false, v_now - interval '20 days');

                                        INSERT INTO "CaseStatusTransition" ("CaseId", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "EnterReasonId",
                                            "EnteredDate", "EnteredUser", "ExitReasonId", "ExitedDate", "ExitedUser", "DurationSeconds")
                                        VALUES (v_case_earlier, v_workflow, v_status_first, 1, v_now - interval '21 days', 'Administrator', 2,
                                                v_now - interval '20 days', 'Administrator', 86400);
                                    END IF;
                                END IF;
                            END IF;

                            IF v_case_778 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "CaseStatusTransition" s WHERE s."CaseId" = v_case_778) THEN
                                INSERT INTO "CaseStatusTransition" ("CaseId", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "PreviousCaseWorkflowStatusGuid",
                                    "EnterReasonId", "EnteredDate", "EnteredUser", "ExitReasonId", "ExitedDate", "ExitedUser", "DurationSeconds")
                                SELECT c."Id", v_workflow, t."Status", t."Previous", t."Enter", c."CreatedDate" + t."EnterAfter", 'Administrator',
                                       t."Exit", c."CreatedDate" + t."ExitAfter", CASE WHEN t."Exit" IS NULL THEN NULL ELSE 'Administrator' END, t."Seconds"
                                FROM "Case" c
                                CROSS JOIN (VALUES
                                    (v_status_first, NULL::uuid, 1, interval '0', 1, interval '2 hours', 7200),
                                    (v_status_supervisor, v_status_first, 4, interval '2 hours', NULL::integer, NULL::interval, NULL::integer))
                                    AS t("Status", "Previous", "Enter", "EnterAfter", "Exit", "ExitAfter", "Seconds")
                                WHERE c."Id" = v_case_778;

                                INSERT INTO "CaseAnalystAssessment" ("Guid", "TenantRegistryId", "CaseId", "LikelihoodId", "Reason", "CreatedUser", "CreatedDate")
                                SELECT gen_random_uuid(), v_tenant, v_case_778, 2,
                                       'Shares a device and a card with an account closed as fraudulent, yet the customer has confirmed the purchases.',
                                       'Administrator', v_now - interval '1 day'
                                WHERE NOT EXISTS (SELECT 1 FROM "CaseAnalystAssessment" a WHERE a."CaseId" = v_case_778);
                            END IF;

                            IF v_case_915 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM "CaseStatusTransition" s WHERE s."CaseId" = v_case_915) THEN
                                INSERT INTO "CaseStatusTransition" ("CaseId", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "PreviousCaseWorkflowStatusGuid",
                                    "EnterReasonId", "EnteredDate", "EnteredUser", "ExitReasonId", "ExitedDate", "ExitedUser", "DurationSeconds")
                                SELECT c."Id", v_workflow, t."Status", t."Previous", t."Enter", c."CreatedDate" + t."EnterAfter", 'Administrator',
                                       t."Exit", c."CreatedDate" + t."ExitAfter", 'Administrator', t."Seconds"
                                FROM "Case" c
                                CROSS JOIN (VALUES
                                    (v_status_first, NULL::uuid, 1, interval '0', 1, interval '3 hours', 10800),
                                    (v_status_supervisor, v_status_first, 4, interval '3 hours', 1, interval '1 day', 75600),
                                    (v_status_fraud, v_status_supervisor, 2, interval '1 day', 2, interval '1 day 1 hour', 3600))
                                    AS t("Status", "Previous", "Enter", "EnterAfter", "Exit", "ExitAfter", "Seconds")
                                WHERE c."Id" = v_case_915;

                                INSERT INTO "CaseQaReview" ("Guid", "TenantRegistryId", "CaseId", "ReviewerUser", "AnalystVerdictId", "Comment", "CreatedDate")
                                SELECT gen_random_uuid(), v_tenant, v_case_915, 'Administrator', 1,
                                       'The finding is recorded and the loss was reported within the day.', v_now - interval '7 days'
                                WHERE NOT EXISTS (SELECT 1 FROM "CaseQaReview" q WHERE q."CaseId" = v_case_915);
                            END IF;

                            INSERT INTO "UserNotification" ("Guid", "TenantRegistryId", "RecipientUser", "KindId", "PriorityId", "ArgumentsJson",
                                "DedupeKey", "CreatedDate")
                            SELECT gen_random_uuid(), v_tenant, 'Administrator', 3, 1,
                                   jsonb_build_object('workflow', v_workflow_name, 'open', 2, 'unassigned', 1, 'overdue', 0, 'closed', 2),
                                   'demonstration-case-digest', v_now
                            WHERE NOT EXISTS (SELECT 1 FROM "UserNotification" n WHERE n."DedupeKey" = 'demonstration-case-digest');
                        END
                        $seed$;

                        DROP FUNCTION IF EXISTS pg_temp.seed_approval(integer, integer, integer);
                        DROP FUNCTION IF EXISTS pg_temp.seed_list(integer, uuid, text);
                        DROP FUNCTION IF EXISTS pg_temp.seed_list_value(integer, integer, text, timestamp, boolean);
                        DROP FUNCTION IF EXISTS pg_temp.seed_dictionary_value(integer, integer, text, double precision, boolean);
                        DROP FUNCTION IF EXISTS pg_temp.seed_tag(integer, integer, text);
                        DROP FUNCTION IF EXISTS pg_temp.seed_case_note(integer, text, integer, integer, boolean, timestamp);
                        DROP FUNCTION IF EXISTS pg_temp.seed_travel_txn(integer, text, timestamp, text, text, numeric, text, text, double precision, double precision);
                        """);
        }

        public override void Down()
        {
        }
    }
}
