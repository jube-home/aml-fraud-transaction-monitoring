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

namespace Jube.Migrations.Branches.Graph
{
    using FluentMigrator;

    [Migration(20263108370000)]
    public class SeedGraphDemonstration : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE OR REPLACE FUNCTION pg_temp.graph_demo_txn(
                            p_model integer, p_txn text, p_at timestamp, p_account text, p_to text, p_amount numeric,
                            p_device text, p_mac text, p_email text, p_card text, p_channel text, p_first text, p_last text)
                        RETURNS uuid LANGUAGE plpgsql AS $f$
                        DECLARE
                            v_guid uuid := gen_random_uuid();
                            v_json jsonb;
                        BEGIN
                            v_json := jsonb_build_object(
                                'payload', jsonb_strip_nulls(jsonb_build_object(
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
                                    'ChannelId', p_channel,
                                    'FingerprintHash', p_device,
                                    'MAC', p_mac,
                                    'Email', p_email,
                                    'CreditCardHash', p_card,
                                    'BillingFirstName', p_first,
                                    'BillingLastName', p_last,
                                    'BillingCountry', 'EG',
                                    'OS', 'Android',
                                    'Brand', 'Samsung',
                                    'Model', 'SM-A536',
                                    'AccountLatitude', 30.0444,
                                    'AccountLongitude', 31.2357)),
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

                            INSERT INTO "ArchiveKey" ("ProcessingTypeId", "Key", "KeyValueString", "KeyValueFloat", "EntityAnalysisModelInstanceEntryGuid", "Version")
                            SELECT 1, k."Key", k."Text", k."Number", v_guid, 1
                            FROM (VALUES
                                ('AccountId', p_account, NULL::double precision),
                                ('ToAccountId', p_to, NULL),
                                ('FingerprintHash', p_device, NULL),
                                ('MAC', p_mac, NULL),
                                ('Email', p_email, NULL),
                                ('CreditCardHash', p_card, NULL),
                                ('AmountEUR', NULL, p_amount::double precision)) AS k("Key", "Text", "Number")
                            WHERE k."Text" IS NOT NULL OR k."Number" IS NOT NULL;

                            RETURN v_guid;
                        END
                        $f$;

                        DO $demo$
                        DECLARE
                            v_model integer;
                            v_workflow_id integer;
                            v_workflow uuid;
                            v_status_open uuid;
                            v_status_supervisor uuid;
                            v_status_fraud uuid;
                            v_registry_id integer;
                            v_registry uuid := '6f1a3c2e-9b7d-4e2a-8c3f-2d9e1b7a5c41';
                            v_admin_role uuid;
                            v_base timestamp := date_trunc('minute', now() at time zone 'utc') - interval '10 days';
                            v_anchor uuid;
                            v_anchor_778 uuid;
                            v_anchor_915 uuid;
                            v_last uuid;
                            v_i integer;
                            v_j integer;
                            v_account text;
                            v_dev text;
                            v_mac text;
                            v_wifi text := 'MAC-PUBLIC-WIFI-CAFE-01';
                        BEGIN
                            IF EXISTS (SELECT 1 FROM "Archive" LIMIT 1) THEN
                                RETURN;
                            END IF;

                            SELECT "Id" INTO v_model FROM "EntityAnalysisModel"
                            WHERE "Guid" = '90c425fd-101a-420b-91d1-cb7a24a969cc' AND coalesce("Deleted", 0) = 0;

                            IF v_model IS NULL THEN
                                RETURN;
                            END IF;

                            SELECT "Id", "Guid" INTO v_workflow_id, v_workflow FROM "CaseWorkflow"
                            WHERE "EntityAnalysisModelId" = v_model AND coalesce("Deleted", 0) = 0 ORDER BY "Id" LIMIT 1;

                            IF v_workflow IS NULL THEN
                                RETURN;
                            END IF;

                            SELECT "Guid" INTO v_status_open FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_workflow_id AND "Name" = 'First Line Review' AND coalesce("Deleted", 0) = 0 LIMIT 1;
                            SELECT "Guid" INTO v_status_supervisor FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_workflow_id AND "Name" = 'Supervisor Review' AND coalesce("Deleted", 0) = 0 LIMIT 1;
                            SELECT "Guid" INTO v_status_fraud FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_workflow_id AND "Name" = 'Fraudulent' AND coalesce("Deleted", 0) = 0 LIMIT 1;
                            SELECT "Guid" INTO v_admin_role FROM "RoleRegistry" WHERE "Id" = 1;

                            IF v_status_open IS NULL OR v_status_fraud IS NULL THEN
                                RETURN;
                            END IF;

                            UPDATE "CaseWorkflowStatus" SET "OutcomeId" = 1 WHERE "CaseWorkflowId" = v_workflow_id AND "Name" IN ('Fraudulent', 'Reported') AND coalesce("OutcomeId", 0) = 0;

                            UPDATE "EntityAnalysisModelRequestXpath" SET "ReportTable" = 1, "UpdatedDate" = now() at time zone 'utc', "UpdatedUser" = 'Administrator'
                            WHERE "EntityAnalysisModelId" = v_model AND coalesce("Deleted", 0) = 0
                              AND "Name" IN ('AccountId', 'ToAccountId', 'FingerprintHash', 'MAC', 'Email', 'CreditCardHash', 'AmountEUR');

                            INSERT INTO "EntityAnalysisModelSynchronisationSchedule" ("CreatedDate", "ScheduleDate", "TenantRegistryId", "CreatedUser", "EntityAnalysisModelId")
                            SELECT now() at time zone 'utc', now() at time zone 'utc', m."TenantRegistryId", 'Administrator', m."Id" FROM "EntityAnalysisModel" m WHERE m."Id" = v_model;

                            INSERT INTO "CaseWorkflowXPath" ("CaseWorkflowId", "Name", "XPath", "Active", "CreatedDate", "CreatedUser", "Version", "Guid",
                                "BoldLineFormatForeColor", "BoldLineFormatBackColor", "ConditionalFormatForeColor", "ConditionalFormatBackColor", "Drill", "RoleId")
                            SELECT v_workflow_id, n."Name", 'payload.' || n."Name", 1, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid(),
                                   '#000000', '#ffffff', '#000000', '#ffffff', n."Drill", n."RoleId"
                            FROM (VALUES ('ToAccountId', 1, 3), ('FingerprintHash', 1, 4), ('MAC', 1, 4), ('Email', 1, 4), ('CreditCardHash', 1, 4), ('AmountEUR', 0, 1)) AS n("Name", "Drill", "RoleId")
                            WHERE NOT EXISTS (SELECT 1 FROM "CaseWorkflowXPath" x WHERE x."CaseWorkflowId" = v_workflow_id AND x."XPath" = 'payload.' || n."Name" AND coalesce(x."Deleted", 0) = 0);

                            INSERT INTO "CaseWorkflowXPathRole" ("RoleRegistryGuid", "CaseWorkflowXPathGuid", "Guid", "CreatedDate", "CreatedUser", "Version")
                            SELECT v_admin_role, x."Guid", gen_random_uuid(), now() at time zone 'utc', 'Administrator', 1
                            FROM "CaseWorkflowXPath" x
                            WHERE x."CaseWorkflowId" = v_workflow_id AND x."Name" IN ('ToAccountId', 'FingerprintHash', 'MAC', 'Email', 'CreditCardHash', 'AmountEUR')
                              AND v_admin_role IS NOT NULL
                              AND NOT EXISTS (SELECT 1 FROM "CaseWorkflowXPathRole" r WHERE r."CaseWorkflowXPathGuid" = x."Guid" AND r."RoleRegistryGuid" = v_admin_role AND coalesce(r."Deleted", 0) = 0);

                            INSERT INTO "GraphRegistry" ("Guid", "TenantRegistryId", "Name", "Active", "Locked", "ContextKindId", "DefaultLayoutId", "DefaultHops", "DefaultWindowDays", "RenderCap", "HubPolicyId", "CreatedDate", "CreatedUser", "Version", "Deleted")
                            SELECT v_registry, m."TenantRegistryId", 'Demonstration payments network', 1, 0, 1, 1, 2, 30, 150, 1, now() at time zone 'utc', 'Administrator', 1, 0
                            FROM "EntityAnalysisModel" m WHERE m."Id" = v_model
                            RETURNING "Id" INTO v_registry_id;

                            INSERT INTO "GraphRegistryEdge" ("GraphRegistryId", "Guid", "Name", "KindId", "FromKey", "ToKey", "ToNodeKey", "AmountKey", "HalfLifeDays", "KindPrior", "HubThreshold", "MinimumAmount", "Hard", "Restricted", "Active", "Locked", "CreatedDate", "CreatedUser", "Version", "Deleted")
                            VALUES
                                (v_registry_id, gen_random_uuid(), 'Pays', 2, 'AccountId', 'ToAccountId', 'AccountId', 'AmountEUR', 90, 0.7, NULL, 0, 0, 0, 1, 0, now() at time zone 'utc', 'Administrator', 1, 0),
                                (v_registry_id, gen_random_uuid(), 'Shares device', 1, 'AccountId', 'FingerprintHash', NULL, NULL, 180, 0.4, NULL, 0, 0, 0, 1, 0, now() at time zone 'utc', 'Administrator', 1, 0),
                                (v_registry_id, gen_random_uuid(), 'Shares network', 1, 'AccountId', 'MAC', NULL, NULL, 180, 0.1, NULL, 0, 0, 0, 1, 0, now() at time zone 'utc', 'Administrator', 1, 0),
                                (v_registry_id, gen_random_uuid(), 'Shares email', 1, 'AccountId', 'Email', NULL, NULL, 365, 1.0, NULL, 0, 1, 0, 1, 0, now() at time zone 'utc', 'Administrator', 1, 0),
                                (v_registry_id, gen_random_uuid(), 'Shares card', 1, 'AccountId', 'CreditCardHash', NULL, NULL, 365, 1.0, NULL, 0, 1, 0, 1, 0, now() at time zone 'utc', 'Administrator', 1, 0);

                            IF v_admin_role IS NOT NULL THEN
                                INSERT INTO "GraphRegistryRole" ("GraphRegistryGuid", "Guid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Version", "Deleted")
                                VALUES (v_registry, gen_random_uuid(), v_admin_role, now() at time zone 'utc', 'Administrator', 1, 0);
                            END IF;

                            UPDATE "CaseWorkflow" SET "EnableGraph" = 1, "GraphRegistryGuid" = v_registry WHERE "Id" = v_workflow_id;

                            FOR v_i IN 1..40 LOOP
                                v_account := 'ACC-B' || lpad(v_i::text, 2, '0');
                                v_dev := 'DEV-B' || lpad(v_i::text, 2, '0');
                                v_mac := CASE WHEN v_i % 3 = 0 THEN v_wifi ELSE 'MAC-HOME-' || lpad(v_i::text, 2, '0') END;
                                FOR v_j IN 1..(3 + v_i % 4) LOOP
                                    PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-B' || lpad(v_i::text, 2, '0') || '-' || v_j,
                                        v_base - make_interval(days => (v_i * 7 + v_j * 11) % 80, hours => (v_i + v_j) % 24, mins => (v_i * 13 + v_j) % 60),
                                        v_account, 'MER-' || lpad(((v_i + v_j) % 5 + 1)::text, 2, '0'), 20 + ((v_i * 37 + v_j * 53) % 480),
                                        v_dev, v_mac, 'customer' || v_i || '@example.com', 'CARD-B' || lpad(v_i::text, 2, '0'), 'Card', 'Customer', 'B' || v_i);
                                END LOOP;
                            END LOOP;

                            FOR v_i IN 1..45 LOOP
                                PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-W' || lpad(v_i::text, 2, '0'),
                                    v_base - make_interval(days => (v_i * 3) % 60, hours => v_i % 24),
                                    'ACC-W' || lpad(v_i::text, 2, '0'), 'MER-' || lpad((v_i % 5 + 1)::text, 2, '0'), 15 + (v_i * 19) % 200,
                                    'DEV-W' || lpad(v_i::text, 2, '0'), v_wifi, 'wifi' || v_i || '@example.com', 'CARD-W' || lpad(v_i::text, 2, '0'), 'Card', 'Visitor', 'W' || v_i);
                            END LOOP;

                            FOR v_i IN 1..11 LOOP
                                PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-DEP-' || lpad(v_i::text, 2, '0'),
                                    v_base - make_interval(days => 9 - (v_i - 1) * 9 / 11, hours => 9 + v_i % 7, mins => (v_i * 17) % 60),
                                    'CASH-BRANCH-07', 'ACC-441', 3900 + (v_i * 97) % 1000,
                                    'DEV-BRANCH-07', 'MAC-BRANCH-07', NULL, NULL, 'Cash', 'Branch', 'Seven');
                            END LOOP;

                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-441-LOGIN-1', v_base - interval '12 days 2 hours', 'ACC-441', 'MER-02', 45,
                                'DEV-8831A', v_wifi, 'm.sterling@example.com', 'CARD-441A', 'Card', 'Marcus', 'Sterling');
                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-441-LOGIN-2', v_base - interval '6 days 5 hours', 'ACC-441', 'MER-04', 120,
                                'DEV-8831A', 'MAC-HOME-441', 'm.sterling@example.com', 'CARD-441A', 'Card', 'Marcus', 'Sterling');

                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-102-OUT', v_base + interval '10 hours 14 minutes 2 seconds', 'ACC-102', 'ACC-02', 9800,
                                'DEV-8831A', 'MAC-HOME-102', 'm.sterling@example.com', 'CARD-102A', 'P2P', 'Kamal', 'Hassan');
                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-102-LOGIN', v_base - interval '3 days', 'ACC-102', 'MER-01', 60,
                                'DEV-8831A', 'MAC-HOME-102', 'm.sterling@example.com', 'CARD-102A', 'Card', 'Kamal', 'Hassan');

                            v_anchor := pg_temp.graph_demo_txn(v_model, 'DEMO-441-P2P', v_base + interval '10 hours 14 minutes 55 seconds', 'ACC-441', 'ACC-02', 9500,
                                'DEV-8831A', 'MAC-HOME-441', 'm.sterling@example.com', 'CARD-441A', 'P2P', 'Marcus', 'Sterling');

                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-02-OUT-1', v_base + interval '10 hours 49 minutes', 'ACC-02', 'ACC-77', 9200,
                                'DEV-02', 'MAC-HOME-02', 'alpha.llc@example.com', NULL, 'Transfer', 'Alpha', 'LLC');
                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-02-OUT-2', v_base + interval '11 hours 2 minutes', 'ACC-02', 'ACC-77', 9700,
                                'DEV-02', 'MAC-HOME-02', 'alpha.llc@example.com', NULL, 'Transfer', 'Alpha', 'LLC');
                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-77-OUT', v_base + interval '13 hours 5 minutes', 'ACC-77', 'ACC-88', 18500,
                                'DEV-77', 'MAC-HOME-77', 'bridge77@example.com', NULL, 'Transfer', 'Bridge', 'Holdings');
                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-88-OUT', v_base + interval '1 day 9 hours', 'ACC-88', 'EXCH-CRYPTO-01', 18200,
                                'DEV-88', 'MAC-HOME-88', 'exit88@example.com', NULL, 'Transfer', 'Exit', 'Trading');

                            FOR v_i IN 1..6 LOOP
                                v_last := pg_temp.graph_demo_txn(v_model, 'DEMO-778-' || v_i, v_base - make_interval(days => 8 - v_i, hours => 18, mins => v_i * 7),
                                    'ACC-778', 'MER-' || lpad((v_i % 5 + 1)::text, 2, '0'), 150 + v_i * 80,
                                    'DEV-8831A', 'MAC-HOME-778', 'osei778@example.com', 'CARD-7F2A', 'Card', 'Kwame', 'Osei');
                            END LOOP;
                            v_anchor_778 := v_last;

                            FOR v_i IN 1..9 LOOP
                                v_last := pg_temp.graph_demo_txn(v_model, 'DEMO-915-' || v_i, v_base - make_interval(days => 28 - v_i * 2, hours => 20, mins => v_i * 5),
                                    'ACC-915', CASE WHEN v_i < 8 THEN 'MER-' || lpad((v_i % 5 + 1)::text, 2, '0') ELSE 'ACC-02' END,
                                    CASE WHEN v_i < 8 THEN 200 + v_i * 90 ELSE 4800 + v_i * 50 END,
                                    'DEV-8831A', 'MAC-HOME-915', 'duarte915@example.com', 'CARD-7F2A', CASE WHEN v_i < 8 THEN 'Card' ELSE 'P2P' END, 'Ana', 'Duarte');
                            END LOOP;
                            v_anchor_915 := v_last;

                            FOR v_i IN 1..5 LOOP
                                PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-SG-OUT-' || v_i, v_base - interval '6 days' + make_interval(mins => v_i * 9),
                                    'ACC-300', 'ACC-30' || v_i, 1900, 'DEV-300', 'MAC-HOME-300', 'hub300@example.com', NULL, 'P2P', 'Scatter', 'Source');
                                PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-SG-IN-' || v_i, v_base - interval '6 days' + make_interval(hours => 2, mins => v_i * 23),
                                    'ACC-30' || v_i, 'ACC-310', 1850, 'DEV-30' || v_i, 'MAC-HOME-30' || v_i, 'mid30' || v_i || '@example.com', NULL, 'P2P', 'Mid', v_i::text);
                            END LOOP;

                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-CYC-1', v_base - interval '4 days 9 hours', 'ACC-400', 'ACC-401', 5000, 'DEV-400', 'MAC-HOME-400', 'c400@example.com', NULL, 'P2P', 'Cycle', 'A');
                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-CYC-2', v_base - interval '4 days 6 hours', 'ACC-401', 'ACC-402', 4900, 'DEV-401', 'MAC-HOME-401', 'c401@example.com', NULL, 'P2P', 'Cycle', 'B');
                            PERFORM pg_temp.graph_demo_txn(v_model, 'DEMO-CYC-3', v_base - interval '4 days 2 hours', 'ACC-402', 'ACC-400', 4800, 'DEV-402', 'MAC-HOME-402', 'c402@example.com', NULL, 'P2P', 'Cycle', 'C');

                            INSERT INTO "Case" ("EntityAnalysisModelInstanceEntryGuid", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "CreatedDate", "Locked", "ClosedStatusId",
                                "CaseKey", "CaseKeyValue", "Diary", "DiaryDate", "Rating", "Json", "DiaryTypeId", "QaSampleStatusId", "LastClosedStatus")
                            SELECT a."EntityAnalysisModelInstanceEntryGuid", v_workflow, v_status_open, v_base + interval '10 hours 15 minutes', 0, 0,
                                   'AccountId', 'ACC-441', 0, now() at time zone 'utc', 0, a."Json", 1, 0, 0
                            FROM "Archive" a WHERE a."EntityAnalysisModelInstanceEntryGuid" = v_anchor;

                            IF v_status_supervisor IS NOT NULL THEN
                                INSERT INTO "Case" ("EntityAnalysisModelInstanceEntryGuid", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "CreatedDate", "Locked", "ClosedStatusId",
                                    "CaseKey", "CaseKeyValue", "Diary", "DiaryDate", "Rating", "Json", "DiaryTypeId", "QaSampleStatusId", "LastClosedStatus")
                                SELECT a."EntityAnalysisModelInstanceEntryGuid", v_workflow, v_status_supervisor, a."CreatedDate" + interval '3 minutes', 0, 0,
                                       'AccountId', 'ACC-778', 0, now() at time zone 'utc', 0, a."Json", 1, 0, 0
                                FROM "Archive" a WHERE a."EntityAnalysisModelInstanceEntryGuid" = v_anchor_778;
                            END IF;

                            INSERT INTO "Case" ("EntityAnalysisModelInstanceEntryGuid", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "CreatedDate", "Locked", "ClosedStatusId",
                                "ClosedDate", "ClosedUser", "CaseKey", "CaseKeyValue", "Diary", "DiaryDate", "Rating", "Json", "DiaryTypeId", "QaSampleStatusId", "LastClosedStatus")
                            SELECT a."EntityAnalysisModelInstanceEntryGuid", v_workflow, v_status_fraud, a."CreatedDate" + interval '2 minutes', 0, 3,
                                   v_base - interval '8 days', 'Administrator', 'AccountId', 'ACC-915', 0, now() at time zone 'utc', 0, a."Json", 1, 0, 3
                            FROM "Archive" a WHERE a."EntityAnalysisModelInstanceEntryGuid" = v_anchor_915;
                        END
                        $demo$;

                        DROP FUNCTION IF EXISTS pg_temp.graph_demo_txn(integer, text, timestamp, text, text, numeric, text, text, text, text, text, text, text);
                        """);
        }

        public override void Down()
        {
        }
    }
}
