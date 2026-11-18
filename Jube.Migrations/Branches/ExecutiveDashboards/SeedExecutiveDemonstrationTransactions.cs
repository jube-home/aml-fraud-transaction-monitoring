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

    [Migration(2026111790000001)]
    public class SeedExecutiveDemonstrationTransactions : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_model integer;
                            v_tenant integer;
                            v_now timestamp := date_trunc('minute', now() at time zone 'utc');
                            v_sanction text;
                            v_model_guid uuid := '90c425fd-101a-420b-91d1-cb7a24a969cc';
                        BEGIN
                            SELECT m."Id", m."TenantRegistryId" INTO v_model, v_tenant FROM "EntityAnalysisModel" m
                            WHERE m."Guid" = v_model_guid AND coalesce(m."Deleted", 0) = 0;

                            IF v_model IS NULL THEN
                                RETURN;
                            END IF;

                            IF NOT EXISTS (SELECT 1 FROM "EntityAnalysisModelActivationRule" WHERE "EntityAnalysisModelId" = v_model AND "Name" = 'StructuringBroad') THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "Archive" WHERE "EntryKeyValue" LIKE 'EXE-%' LIMIT 1) THEN
                                RETURN;
                            END IF;

                            SELECT "Name" INTO v_sanction FROM "EntityAnalysisModelSanction" WHERE "EntityAnalysisModelId" = v_model AND coalesce("Deleted", 0) = 0 ORDER BY "Id" LIMIT 1;

                            CREATE TEMP TABLE exe_txn ON COMMIT DROP AS
                            SELECT row_number() OVER (ORDER BY d.days_ago DESC, d.k) AS n, d.days_ago, d.k
                            FROM (
                                SELECT g AS days_ago, s AS k
                                FROM generate_series(0, 364) g
                                CROSS JOIN LATERAL generate_series(1, 35
                                    + CASE WHEN extract(dow FROM (v_now::date - g)) IN (0, 6) THEN 0 ELSE 30 END
                                    + (abs(hashtext('d' || g)) % 25)) s
                            ) d;

                            CREATE INDEX ON exe_txn (n);

                            CREATE TEMP TABLE exe_txn2 ON COMMIT DROP AS
                            SELECT t.n, t.days_ago,
                                   13 - least(12, (t.days_ago / 30) + 1) AS month_ix,
                                   v_now - make_interval(days => t.days_ago) - make_interval(secs => (abs(hashtext('t' || t.n)) % 86000)) AS at,
                                   (abs(hashtext('x' || t.n)) % 1000) AS alert_roll,
                                   (abs(hashtext('r' || t.n)) % 100) AS rule_roll,
                                   (abs(hashtext('c' || t.n)) % 100) AS country_roll,
                                   (abs(hashtext('a' || t.n)) % 600) AS acct_roll,
                                   (abs(hashtext('h' || t.n)) % 90) AS hot_roll,
                                   (abs(hashtext('m' || t.n)) % 10000) AS amt_roll,
                                   (abs(hashtext('s' || t.n)) % 1090) AS struct_roll,
                                   (abs(hashtext('z' || t.n)) % 6) AS cofire_roll,
                                   (abs(hashtext('q' || t.n)) % 1000) AS sanction_roll,
                                   (abs(hashtext('w' || t.n)) % 1000) AS shadow_roll,
                                   (abs(hashtext('e' || t.n)) % 1000) AS elevation_roll
                            FROM exe_txn t;

                            CREATE TEMP TABLE exe_txn3 ON COMMIT DROP AS
                            SELECT x.*,
                                   (x.alert_roll < CASE WHEN x.month_ix <= 6 THEN 95 WHEN x.month_ix = 9 THEN 80 ELSE 62 END) AS is_alert,
                                   (x.sanction_roll < 14) AS is_sanction
                            FROM exe_txn2 x;

                            CREATE TEMP TABLE exe_txn4 ON COMMIT DROP AS
                            SELECT y.*,
                                   CASE
                                       WHEN y.is_sanction THEN 'ThresholdSanctionsDistance'
                                       WHEN NOT y.is_alert THEN NULL
                                       WHEN y.month_ix <= 6 THEN
                                           CASE WHEN y.rule_roll < 38 THEN 'StructuringBroad' WHEN y.rule_roll < 58 THEN 'RapidVelocity' WHEN y.rule_roll < 66 THEN 'MuleRingFanOut'
                                                WHEN y.rule_roll < 78 THEN 'ImpossibleTravel' WHEN y.rule_roll < 88 THEN 'HighRiskCountryCrossing' ELSE 'VolumeThresholdByAccountId' END
                                       ELSE
                                           CASE WHEN y.rule_roll < 16 THEN 'StructuringTuned' WHEN y.rule_roll < 44 THEN 'RapidVelocity' WHEN y.rule_roll < 56 THEN 'MuleRingFanOut'
                                                WHEN y.rule_roll < 72 THEN 'ImpossibleTravel' WHEN y.rule_roll < 88 THEN 'HighRiskCountryCrossing' ELSE 'VolumeThresholdByAccountId' END
                                   END AS rule1
                            FROM exe_txn3 y;

                            CREATE TEMP TABLE exe_txn5 ON COMMIT DROP AS
                            SELECT z.*,
                                   CASE WHEN z.rule1 IS NOT NULL AND z.rule1 <> 'ThresholdSanctionsDistance' AND z.cofire_roll = 0 THEN
                                       (ARRAY['RapidVelocity', 'ImpossibleTravel', 'VolumeThresholdByAccountId', 'MuleRingFanOut'])[1 + (z.rule_roll % 4)]
                                   END AS rule2_raw
                            FROM exe_txn4 z;

                            CREATE TEMP TABLE exe_txn6 ON COMMIT DROP AS
                            SELECT w.n, w.days_ago, w.month_ix, w.at, w.is_alert, w.is_sanction, w.rule1,
                                   CASE WHEN w.rule2_raw = w.rule1 THEN NULL ELSE w.rule2_raw END AS rule2,
                                   CASE WHEN w.rule1 IS NOT NULL THEN 'ACC-S' || lpad((1 + w.hot_roll)::text, 4, '0') ELSE 'ACC-S' || lpad((1 + w.acct_roll)::text, 4, '0') END AS account,
                                   CASE
                                       WHEN w.rule1 = 'HighRiskCountryCrossing' THEN (ARRAY['IR', 'KP', 'MM'])[1 + (w.country_roll % 3)]
                                       WHEN w.rule1 = 'ImpossibleTravel' THEN (ARRAY['NG', 'AE', 'TR', 'EG', 'DE'])[1 + (w.country_roll % 5)]
                                       WHEN w.country_roll < 40 THEN 'GB' WHEN w.country_roll < 55 THEN 'DE' WHEN w.country_roll < 67 THEN 'FR' WHEN w.country_roll < 76 THEN 'NL'
                                       WHEN w.country_roll < 83 THEN 'ES' WHEN w.country_roll < 89 THEN 'IT' WHEN w.country_roll < 93 THEN 'PL' WHEN w.country_roll < 96 THEN 'TR'
                                       WHEN w.country_roll < 98 THEN 'AE' ELSE 'EG'
                                   END AS country,
                                   (ARRAY['Card', 'Web', 'Transfer', 'ATM', 'P2P'])[1 + (w.amt_roll % 5)] AS channel,
                                   CASE
                                       WHEN w.rule1 IN ('StructuringBroad', 'StructuringTuned') THEN 3900 + w.struct_roll
                                       WHEN w.rule1 = 'MuleRingFanOut' THEN 9000 + (w.struct_roll % 900)
                                       WHEN w.rule1 = 'RapidVelocity' THEN 80 + (w.amt_roll % 400)
                                       WHEN w.rule1 IS NULL AND w.struct_roll % 25 = 0 THEN 3500 + (w.struct_roll % 399)
                                       ELSE round((20 + power(w.amt_roll / 10000.0, 3) * 3000)::numeric, 2)
                                   END AS amount,
                                   w.shadow_roll, w.elevation_roll, w.sanction_roll,
                                   gen_random_uuid() AS guid
                            FROM exe_txn5 w;

                            CREATE TEMP TABLE exe_txn7 ON COMMIT DROP AS
                            SELECT u.*,
                                   (SELECT max(e) FROM unnest(ARRAY[
                                       CASE u.rule1 WHEN 'StructuringBroad' THEN 2 WHEN 'StructuringTuned' THEN 2 WHEN 'RapidVelocity' THEN 1 WHEN 'MuleRingFanOut' THEN 3 WHEN 'ImpossibleTravel' THEN 2
                                                    WHEN 'HighRiskCountryCrossing' THEN 1 WHEN 'ThresholdSanctionsDistance' THEN 3 WHEN 'VolumeThresholdByAccountId' THEN 2 ELSE 0 END,
                                       CASE u.rule2 WHEN 'StructuringBroad' THEN 2 WHEN 'RapidVelocity' THEN 1 WHEN 'MuleRingFanOut' THEN 3 WHEN 'ImpossibleTravel' THEN 2
                                                    WHEN 'VolumeThresholdByAccountId' THEN 2 ELSE 0 END]) AS e) AS elevation_tier
                            FROM exe_txn6 u;

                            INSERT INTO "Archive" ("Json", "EntityAnalysisModelInstanceEntryGuid", "EntryKeyValue", "ResponseElevation", "EntityAnalysisModelActivationRuleId",
                                "EntityAnalysisModelId", "ActivationRuleCount", "CreatedDate", "ReferenceDate", "Version")
                            SELECT jsonb_strip_nulls(jsonb_build_object(
                                       'payload', jsonb_build_object(
                                           'TxnId', 'EXE-' || t.n,
                                           'TxnDateTime', to_char(t.at, 'YYYY-MM-DD"T"HH24:MI:SS'),
                                           'AccountId', t.account,
                                           'CurrencyAmount', t.amount,
                                           'AmountEUR', t.amount,
                                           'Currency', 'EUR',
                                           'BillingCountry', t.country,
                                           'ChannelId', t.channel,
                                           'ResponseCode', '0'),
                                       'tag', '[]'::jsonb,
                                       'activation', CASE WHEN t.rule1 IS NULL THEN '{}'::jsonb ELSE
                                           jsonb_build_object(t.rule1, jsonb_build_object('visible', true, 'forced', false))
                                           || CASE WHEN t.rule2 IS NULL THEN '{}'::jsonb ELSE jsonb_build_object(t.rule2, jsonb_build_object('visible', true, 'forced', false)) END END,
                                       'shadowActivation', CASE
                                           WHEN t.month_ix IN (5, 6) AND t.rule1 = 'StructuringBroad' AND t.shadow_roll < 420 THEN jsonb_build_object('StructuringTuned', jsonb_build_object('visible', true, 'forced', false))
                                           WHEN t.days_ago < 56 AND t.rule1 = 'RapidVelocity' AND t.shadow_roll < 450 THEN jsonb_build_object('RapidVelocityTightenedShadow', jsonb_build_object('visible', true, 'forced', false))
                                           WHEN t.days_ago < 56 AND t.rule1 IS NULL AND t.shadow_roll < 20 THEN jsonb_build_object('RapidVelocityTightenedShadow', jsonb_build_object('visible', true, 'forced', false))
                                           ELSE '{}'::jsonb END,
                                       'abstraction', '{}'::jsonb,
                                       'ttlCounter', '{}'::jsonb,
                                       'sanction', CASE WHEN t.is_sanction AND v_sanction IS NOT NULL THEN jsonb_build_object(v_sanction, 1 + (t.sanction_roll % 2)) END,
                                       'responseElevation', jsonb_build_object('value', t.elevation_val, 'name', 'Level ' || greatest(t.elevation_tier, 1), 'createdDate', to_char(t.at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"')),
                                       'prevailingEntityAnalysisModelActivationRuleName', t.rule1,
                                       'entityAnalysisModelActivationRuleCount', CASE WHEN t.rule1 IS NULL THEN 0 WHEN t.rule2 IS NULL THEN 1 ELSE 2 END,
                                       'entityAnalysisModelGuid', v_model_guid::text,
                                       'entityAnalysisModelName', 'Detailed Account Financial Transactions',
                                       'entityAnalysisModelInstanceEntryGuid', t.guid::text,
                                       'createdDate', to_char(t.at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'),
                                       'referenceDate', to_char(t.at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'),
                                       'createCase', (t.rule1 IS NOT NULL AND t.rule1 <> 'HighRiskCountryCrossing'))),
                                   t.guid, 'EXE-' || t.n, t.elevation_val,
                                   (SELECT r."Id" FROM "EntityAnalysisModelActivationRule" r WHERE r."EntityAnalysisModelId" = v_model AND r."Name" = t.rule1 AND coalesce(r."Deleted", 0) = 0 ORDER BY r."Id" LIMIT 1),
                                   v_model, CASE WHEN t.rule1 IS NULL THEN 0 WHEN t.rule2 IS NULL THEN 1 ELSE 2 END, t.at, t.at, 1
                            FROM (
                                SELECT q.*,
                                       CASE WHEN q.rule1 IS NULL THEN round(((q.elevation_roll / 1000.0) * 0.3 + CASE WHEN q.days_ago < 42 THEN 0.10 ELSE 0 END)::numeric, 3)::double precision
                                            ELSE (q.elevation_tier + round((q.elevation_roll / 1000.0)::numeric, 3))::double precision END AS elevation_val
                                FROM exe_txn7 q
                            ) t;

                            INSERT INTO "ArchiveKey" ("ProcessingTypeId", "Key", "KeyValueString", "KeyValueFloat", "EntityAnalysisModelInstanceEntryGuid", "Version")
                            SELECT 1, k."Key", k."Text", k."Number", t.guid, 1
                            FROM exe_txn7 t
                            CROSS JOIN LATERAL (VALUES ('AccountId', t.account, NULL::double precision), ('BillingCountry', t.country, NULL), ('ChannelId', t.channel, NULL), ('AmountEUR', NULL, t.amount::double precision)) AS k("Key", "Text", "Number");

                            INSERT INTO "ActivationWatcher" ("TenantRegistryId", "Key", "KeyValue", "Longitude", "Latitude", "ActivationRuleSummary", "ResponseElevationContent", "ResponseElevation",
                                "BackColor", "ForeColor", "CreatedDate", "ResponseElevationName")
                            SELECT v_tenant, 'AccountId', t.account, c."Lon" + ((abs(hashtext('lo' || t.n)) % 100) - 50) / 500.0, c."Lat" + ((abs(hashtext('la' || t.n)) % 100) - 50) / 500.0,
                                   t.rule1, 'Review the account', t.elevation_val, '#ffe4e1', '#000000', t.at, 'Level ' || greatest(t.elevation_tier, 1)
                            FROM (SELECT x.*, CASE WHEN x.rule1 IS NULL THEN 0 ELSE x.elevation_tier END AS elevation_val FROM exe_txn7 x) t
                            JOIN (VALUES ('GB', -0.1278, 51.5074), ('DE', 13.4050, 52.5200), ('FR', 2.3522, 48.8566), ('NL', 4.9041, 52.3676), ('ES', -3.7038, 40.4168), ('IT', 12.4964, 41.9028),
                                         ('PL', 21.0122, 52.2297), ('TR', 28.9784, 41.0082), ('AE', 55.2708, 25.2048), ('EG', 31.2357, 30.0444), ('NG', 3.3792, 6.5244),
                                         ('IR', 51.3890, 35.6892), ('KP', 125.7625, 39.0392), ('MM', 96.1951, 16.8661)) AS c("Country", "Lon", "Lat") ON c."Country" = t.country
                            WHERE t.rule1 IS NOT NULL AND t.days_ago < 30 AND t.elevation_tier >= 2;

                            INSERT INTO "EntityAnalysisModelActivationRuleCounterHistory" ("EntityAnalysisModelActivationRuleId", "EvaluationCounter", "ActivationCounter", "ForcedActivationCounter", "CreatedDate", "Instance")
                            SELECT r."Id", d.evaluated, d.activated, 0, d.day + interval '23 hours 59 minutes', 'demonstration'
                            FROM (
                                SELECT (v_now::date - t.days_ago)::timestamp AS day, rule_name, count(*) AS evaluated, count(*) FILTER (WHERE fired) AS activated
                                FROM (
                                    SELECT t.days_ago, rn.rule_name, (t.rule1 = rn.rule_name OR t.rule2 = rn.rule_name) AS fired
                                    FROM exe_txn7 t
                                    CROSS JOIN (SELECT "Name" AS rule_name FROM "EntityAnalysisModelActivationRule"
                                                WHERE "EntityAnalysisModelId" = v_model AND "Name" IN ('StructuringBroad', 'StructuringTuned', 'RapidVelocity', 'MuleRingFanOut', 'ImpossibleTravel', 'HighRiskCountryCrossing', 'VolumeThresholdByAccountId', 'ThresholdSanctionsDistance')) rn
                                    WHERE t.days_ago < 90
                                      AND NOT (rn.rule_name = 'StructuringBroad' AND t.month_ix >= 7)
                                      AND NOT (rn.rule_name = 'StructuringTuned' AND t.month_ix < 7)
                                ) t
                                GROUP BY 1, 2
                            ) d(day, rule_name, evaluated, activated)
                            JOIN "EntityAnalysisModelActivationRule" r ON r."EntityAnalysisModelId" = v_model AND r."Name" = d.rule_name AND coalesce(r."Deleted", 0) = 0;

                            UPDATE "EntityAnalysisModelActivationRule" r SET
                                "EvaluationCounter" = h.e, "ActivationCounter" = h.a, "ActivationCounterDate" = v_now
                            FROM (SELECT "EntityAnalysisModelActivationRuleId" AS id, sum("EvaluationCounter") AS e, sum("ActivationCounter") AS a
                                  FROM "EntityAnalysisModelActivationRuleCounterHistory" WHERE "Instance" = 'demonstration' GROUP BY 1) h
                            WHERE h.id = r."Id";

                            INSERT INTO "EntityAnalysisModelProcessingCounter" ("CreatedDate", "Instance", "ModelInvoke", "GatewayMatch", "ResponseElevation", "ResponseElevationSum", "ActivationWatcher",
                                "EntityAnalysisModelGuid", "ModelTotalResponseTime", "MinResponseTimeMicroseconds", "MaxResponseTimeMicroseconds", "ImplicitAsyncInvoke", "ImplicitAsyncTimeout", "ArchiveWalPendingCount")
                            SELECT h."Hour", 'demonstration', h.invoked, h.invoked, h.alerts, h.alerts * 2.0, h.alerts / 3,
                                   v_model_guid, h.invoked * (4200 + (abs(hashtext('rt' || h.ix)) % 1800))::bigint, 900 + (abs(hashtext('mn' || h.ix)) % 300),
                                   14000 + (abs(hashtext('mx' || h.ix)) % 60000) * CASE WHEN h.ix % 97 = 0 THEN 5 ELSE 1 END, h.invoked / 4, CASE WHEN h.ix % 53 = 0 THEN 2 ELSE 0 END, abs(hashtext('wal' || h.ix)) % 40
                            FROM (
                                SELECT g AS ix, v_now - make_interval(hours => g) AS "Hour",
                                       (SELECT count(*) FROM exe_txn7 t WHERE t.at >= v_now - make_interval(hours => g + 1) AND t.at < v_now - make_interval(hours => g))::integer AS invoked,
                                       (SELECT count(*) FROM exe_txn7 t WHERE t.rule1 IS NOT NULL AND t.at >= v_now - make_interval(hours => g + 1) AND t.at < v_now - make_interval(hours => g))::integer AS alerts
                                FROM generate_series(0, 719) g
                            ) h;

                            INSERT INTO "EntityAnalysisModelAsynchronousQueueBalance" ("EntityAnalysisModelGuid", "CreatedDate", "Archive", "ActivationWatcher", "Instance", "ImplicitAsyncOverdue")
                            SELECT v_model_guid, v_now - make_interval(mins => g * 15), abs(hashtext('qa' || g)) % 40, abs(hashtext('qw' || g)) % 12, 'demonstration', CASE WHEN g % 211 = 0 THEN 1 ELSE 0 END
                            FROM generate_series(0, 2879) g;
                        END
                        $seed$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
