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

    [Migration(2026111630000001)]
    public class SeedDemonstrationFraudScoreInputs : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE OR REPLACE FUNCTION pg_temp.seed_fraud_score_approval(p_tenant integer, p_kind integer, p_entity integer)
                        RETURNS void LANGUAGE plpgsql AS $f$
                        BEGIN
                            INSERT INTO "EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId", "EntityVersion",
                                "StateId", "CreatedDate", "CreatedUser", "Guid")
                            SELECT p_tenant, p_kind, p_entity, 1, 1, now() at time zone 'utc', 'Administrator', gen_random_uuid()
                            WHERE NOT EXISTS (SELECT 1 FROM "EntityApproval" a
                                              WHERE a."EntityApprovalKindId" = p_kind AND a."EntityId" = p_entity AND a."EntityVersion" = 1);
                        END
                        $f$;

                        CREATE OR REPLACE FUNCTION pg_temp.seed_fraud_score_rule(p_tenant integer, p_model integer, p_name text, p_rule_type integer,
                            p_builder text, p_json text, p_coder text, p_search integer, p_search_key text, p_interval text, p_value integer,
                            p_function integer, p_function_key text, p_offset integer, p_offset_type integer, p_offset_value integer,
                            p_response_payload integer)
                        RETURNS void LANGUAGE plpgsql AS $f$
                        DECLARE
                            v_id integer;
                        BEGIN
                            IF EXISTS (SELECT 1 FROM "EntityAnalysisModelAbstractionRule" r
                                       WHERE r."EntityAnalysisModelId" = p_model AND lower(r."Name") = lower(p_name) AND coalesce(r."Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "EntityAnalysisModelAbstractionRule" ("EntityAnalysisModelId", "Name", "RuleScriptTypeId", "BuilderRuleScript", "Json",
                                "CoderRuleScript", "Search", "SearchKey", "SearchInterval", "SearchValue", "SearchFunctionTypeId", "SearchFunctionKey",
                                "Offset", "OffsetTypeId", "OffsetValue", "IterationOrderId", "ResponsePayload", "ReportTable", "Active", "Locked",
                                "InheritedId", "Compiled", "CreatedDate", "CreatedUser", "Version", "Guid")
                            VALUES (p_model, p_name, p_rule_type, p_builder, p_json::jsonb, coalesce(p_coder, p_builder), p_search, p_search_key, p_interval,
                                p_value, p_function, p_function_key, p_offset, p_offset_type, p_offset_value, 1, p_response_payload, 0, 1, 0, 0, 1,
                                now() at time zone 'utc', 'Administrator', 1, gen_random_uuid())
                            RETURNING "Id" INTO v_id;

                            PERFORM pg_temp.seed_fraud_score_approval(p_tenant, 11, v_id);
                        END
                        $f$;

                        DO $seed$
                        DECLARE
                            v_model_guid constant uuid := '90c425fd-101a-420b-91d1-cb7a24a969cc';
                            v_model integer;
                            v_tenant integer;
                            v_list integer;
                            v_value text;
                            v_id integer;
                        BEGIN
                            SELECT m."Id", m."TenantRegistryId" INTO v_model, v_tenant FROM "EntityAnalysisModel" m
                            WHERE m."Id" = 1 AND m."Guid" = v_model_guid AND coalesce(m."Deleted", 0) = 0;

                            IF v_model IS NULL THEN
                                RETURN;
                            END IF;

                            UPDATE "EntityAnalysisModel" SET "CacheTtlIntervalValue" = 7
                            WHERE "Id" = v_model AND "CacheTtlInterval" = 'd' AND "CacheTtlIntervalValue" < 7;

                            UPDATE "EntityAnalysisModelRequestXpath" SET "SearchKeyTtlIntervalValue" = 7
                            WHERE "EntityAnalysisModelId" = v_model AND "Name" = 'AccountId' AND "SearchKey" = 1 AND coalesce("Deleted", 0) = 0
                              AND "SearchKeyTtlInterval" = 'd' AND "SearchKeyTtlIntervalValue" < 7;

                            SELECT l."Id" INTO v_list FROM "EntityAnalysisModelList" l
                            WHERE l."EntityAnalysisModelGuid" = v_model_guid AND lower(l."Name") = lower('HighRiskCountries') AND coalesce(l."Deleted", 0) = 0
                            ORDER BY l."Id" LIMIT 1;

                            IF v_list IS NULL THEN
                                INSERT INTO "EntityAnalysisModelList" ("Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Version", "Guid",
                                    "EntityAnalysisModelGuid")
                                VALUES ('HighRiskCountries', 1, 0, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid(), v_model_guid)
                                RETURNING "Id" INTO v_list;

                                PERFORM pg_temp.seed_fraud_score_approval(v_tenant, 2, v_list);

                                FOREACH v_value IN ARRAY ARRAY['IR', 'KP', 'MM'] LOOP
                                    INSERT INTO "EntityAnalysisModelListValue" ("EntityAnalysisModelListId", "ListValue", "CreatedDate", "CreatedUser",
                                        "Version", "Guid")
                                    VALUES (v_list, v_value, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid())
                                    RETURNING "Id" INTO v_id;

                                    PERFORM pg_temp.seed_fraud_score_approval(v_tenant, 3, v_id);
                                END LOOP;
                            END IF;

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'IsChip', 1,
                                $r$If (Payload.ServiceCode = "Chip") Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.ServiceCode", "type": "string", "field": "Payload.ServiceCode", "input": "text", "value": "Chip", "operator": "equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                0, NULL, 'h', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'IsManual', 1,
                                $r$If (Payload.ServiceCode = "Manual") Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.ServiceCode", "type": "string", "field": "Payload.ServiceCode", "input": "text", "value": "Manual", "operator": "equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                0, NULL, 'h', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'ATM', 1,
                                $r$If (Payload.ChannelId = "ATM") Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.ChannelId", "type": "string", "field": "Payload.ChannelId", "input": "text", "value": "ATM", "operator": "equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                0, NULL, 'h', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'CashTransaction', 1,
                                $r$If (Payload.ChannelId = "ATM" OR Payload.ChannelId = "Cash") Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.ChannelId", "type": "string", "field": "Payload.ChannelId", "input": "text", "value": "ATM", "operator": "equal"}, {"id": "Payload.ChannelId", "type": "string", "field": "Payload.ChannelId", "input": "text", "value": "Cash", "operator": "equal"}], "valid": true, "condition": "OR"}',
                                NULL,
                                0, NULL, 'h', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'InPerson', 1,
                                $r$If (Payload.ChannelId = "Card" OR Payload.ChannelId = "ATM" OR Payload.ChannelId = "Cash") Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.ChannelId", "type": "string", "field": "Payload.ChannelId", "input": "text", "value": "Card", "operator": "equal"}, {"id": "Payload.ChannelId", "type": "string", "field": "Payload.ChannelId", "input": "text", "value": "ATM", "operator": "equal"}, {"id": "Payload.ChannelId", "type": "string", "field": "Payload.ChannelId", "input": "text", "value": "Cash", "operator": "equal"}], "valid": true, "condition": "OR"}',
                                NULL,
                                0, NULL, 'h', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'Authenticated', 1,
                                $r$If (Payload.TwoFAResponseId = "1" OR Payload.Is3D = True) Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.TwoFAResponseId", "type": "string", "field": "Payload.TwoFAResponseId", "input": "text", "value": "1", "operator": "equal"}, {"id": "Payload.Is3D", "type": "string", "field": "Payload.Is3D", "input": "radio", "value": "True", "operator": "equal"}], "valid": true, "condition": "OR"}',
                                NULL,
                                0, NULL, 'h', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'HighRiskCountry', 1,
                                $r$If (List.HighRiskCountries.contains(Payload.BillingCountry)) Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "List.HighRiskCountries", "type": "string", "field": "List.HighRiskCountries", "input": "select", "value": "Payload.BillingCountry", "operator": "has"}], "valid": true, "condition": "AND"}',
                                NULL,
                                0, NULL, 'h', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'TransactionAmt', 1,
                                $r$If (Payload.AmountEUR >= 0) Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.AmountEUR", "type": "double", "field": "Payload.AmountEUR", "input": "number", "value": 0, "operator": "greater_or_equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                1, 'AccountId', 'd', 1, 3, 'AmountEUR', 1, 2, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'CountTransactionsDeclined1Day', 1,
                                $r$If (Payload.ResponseCode <> "0") Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.ResponseCode", "type": "string", "field": "Payload.ResponseCode", "input": "text", "value": "0", "operator": "not_equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                1, 'AccountId', 'd', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'CountTransactionsPINDecline1Day', 1,
                                $r$If (Payload.ResponseCode = "55") Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.ResponseCode", "type": "string", "field": "Payload.ResponseCode", "input": "text", "value": "55", "operator": "equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                1, 'AccountId', 'd', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'CountUnsafeTerminals1Day', 1,
                                $r$If (Payload.Jailbreak = True OR Payload.DebuggerAttached = True OR Payload.SimulatorAttached = True) Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.Jailbreak", "type": "string", "field": "Payload.Jailbreak", "input": "radio", "value": "True", "operator": "equal"}, {"id": "Payload.DebuggerAttached", "type": "string", "field": "Payload.DebuggerAttached", "input": "radio", "value": "True", "operator": "equal"}, {"id": "Payload.SimulatorAttached", "type": "string", "field": "Payload.SimulatorAttached", "input": "radio", "value": "True", "operator": "equal"}], "valid": true, "condition": "OR"}',
                                NULL,
                                1, 'AccountId', 'd', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'CountInternet1Day', 1,
                                $r$If (Payload.ChannelId = "Internet") Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.ChannelId", "type": "string", "field": "Payload.ChannelId", "input": "text", "value": "Internet", "operator": "equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                1, 'AccountId', 'd', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'CountATM1Day', 1,
                                $r$If (Payload.ChannelId = "ATM") Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.ChannelId", "type": "string", "field": "Payload.ChannelId", "input": "text", "value": "ATM", "operator": "equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                1, 'AccountId', 'd', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'CountOver30SEK1Day', 1,
                                $r$If (Payload.AmountEUR > 3) Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.AmountEUR", "type": "double", "field": "Payload.AmountEUR", "input": "number", "value": 3, "operator": "greater"}], "valid": true, "condition": "AND"}',
                                NULL,
                                1, 'AccountId', 'd', 1, 1, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'SumTransactions1Day', 2,
                                'Return False',
                                '{"rules": [], "condition": "AND"}',
                                'Cursor.Result = Cursor.Result + Payload.AmountEUR * 10',
                                1, 'AccountId', 'd', 1, 46, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'SumATMTransactions1Day', 2,
                                'Return False',
                                '{"rules": [], "condition": "AND"}',
                                $r$If Payload.ChannelId = "ATM" Then
                          Cursor.Result = Cursor.Result + Payload.AmountEUR * 10
                        End If$r$,
                                1, 'AccountId', 'd', 1, 46, NULL, 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'DifferentCountryTransactions1Week', 1,
                                $r$If (Payload.AmountEUR >= 0) Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.AmountEUR", "type": "double", "field": "Payload.AmountEUR", "input": "number", "value": 0, "operator": "greater_or_equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                1, 'AccountId', 'd', 7, 2, 'BillingCountry', 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'DifferentCities1Week', 1,
                                $r$If (Payload.AmountEUR >= 0) Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.AmountEUR", "type": "double", "field": "Payload.AmountEUR", "input": "number", "value": 0, "operator": "greater_or_equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                1, 'AccountId', 'd', 7, 2, 'BillingCity', 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'CountSameMerchantUsedBefore1Week', 1,
                                $r$If (Payload.AmountEUR >= 0) Then
                          Return True
                        End If$r$,
                                '{"not": false, "rules": [{"id": "Payload.AmountEUR", "type": "double", "field": "Payload.AmountEUR", "input": "number", "value": 0, "operator": "greater_or_equal"}], "valid": true, "condition": "AND"}',
                                NULL,
                                1, 'AccountId', 'd', 7, 12, 'ToAccountId', 0, 1, 0, 0);

                            PERFORM pg_temp.seed_fraud_score_rule(v_tenant, v_model, 'HasBeenAbroad', 2,
                                'Return False',
                                '{"rules": [], "condition": "AND"}',
                                $r$Static home As String = ""
                        If home = "" Then home = Payload.BillingCountry
                        If Payload.BillingCountry <> home Then Cursor.Result = 1$r$,
                                1, 'AccountId', 'd', 7, 46, NULL, 0, 1, 0, 0);
                        END
                        $seed$;

                        DROP FUNCTION IF EXISTS pg_temp.seed_fraud_score_rule(integer, integer, text, integer, text, text, text, integer, text, text, integer,
                            integer, text, integer, integer, integer, integer);
                        DROP FUNCTION IF EXISTS pg_temp.seed_fraud_score_approval(integer, integer, integer);
                        """);
        }

        public override void Down()
        {
        }
    }
}