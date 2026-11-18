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

    [Migration(2026111790000003)]
    public class SeedExecutiveDemonstrationSupporting : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_model integer;
                            v_tenant integer;
                            v_now timestamp := date_trunc('minute', now() at time zone 'utc');
                            v_reprocessing integer;
                        BEGIN
                            SELECT m."Id", m."TenantRegistryId" INTO v_model, v_tenant FROM "EntityAnalysisModel" m
                            WHERE m."Guid" = '90c425fd-101a-420b-91d1-cb7a24a969cc' AND coalesce(m."Deleted", 0) = 0;

                            IF v_model IS NULL THEN
                                RETURN;
                            END IF;

                            IF NOT EXISTS (SELECT 1 FROM "EntityAnalysisModelActivationRule" WHERE "EntityAnalysisModelId" = v_model AND "Name" = 'StructuringBroad') THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "EntityAnalysisModelBacktestInstance" WHERE "CreatedUser" = 'Priya Raman' AND "EntityAnalysisModelId" = v_model) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "EntityAnalysisModelBacktestInstance" ("Guid", "TenantRegistryId", "EntityAnalysisModelId", "RuleType", "RuleId", "Request", "Status", "Scanned", "Evaluated", "Progress",
                                "Result", "CreatedDate", "CreatedUser", "StartedDate", "CompletedDate", "Deleted")
                            SELECT gen_random_uuid(), v_tenant, v_model, 'Activation', r."Id",
                                   jsonb_build_object('RuleType', 'Activation', 'RuleText', r."CoderRuleScript", 'EntityAnalysisModelId', v_model),
                                   3, b."Scanned", b."Scanned", 1.0,
                                   jsonb_build_object('Scanned', b."Scanned", 'FilteredOut', 0, 'FilterErrors', 0, 'Evaluated', b."Scanned", 'Fired', b."Tp" + b."Fp", 'NotFired', b."Scanned" - b."Tp" - b."Fp",
                                       'RuntimeErrors', 0, 'ClassDefined', true, 'Positives', b."Tp" + b."Fn", 'TruePositives', b."Tp", 'FalsePositives', b."Fp", 'FalseNegatives', b."Fn",
                                       'TrueNegatives', b."Scanned" - b."Tp" - b."Fp" - b."Fn", 'LimitReached', false, 'Aborted', false, 'DurationMicroseconds', b."Micros"),
                                   v_now - make_interval(days => b."DaysAgo"), b."Maker", v_now - make_interval(days => b."DaysAgo"), v_now - make_interval(days => b."DaysAgo") + interval '1 minute' * b."Minutes", 0
                            FROM (VALUES
                                ('StructuringTuned', 210, 12000, 164, 376, 52, 'Priya Raman', 7, 41000000),
                                ('StructuringTuned', 195, 12000, 150, 270, 66, 'Priya Raman', 6, 39000000),
                                ('RapidVelocityTightenedShadow', 44, 9000, 61, 249, 38, 'Priya Raman', 5, 28000000),
                                ('MuleRingFanOut', 300, 12000, 88, 120, 14, 'Priya Raman', 6, 33000000)
                            ) AS b("Rule", "DaysAgo", "Scanned", "Tp", "Fp", "Fn", "Maker", "Minutes", "Micros")
                            JOIN "EntityAnalysisModelActivationRule" r ON r."EntityAnalysisModelId" = v_model AND r."Name" = b."Rule" AND coalesce(r."Deleted", 0) = 0;

                            INSERT INTO "EntityAnalysisModelReprocessingRule" ("EntityAnalysisModelId", "Active", "Locked", "Priority", "Name", "CreatedDate", "Version", "CreatedUser", "Deleted", "ReprocessingSample",
                                "CoderRuleScript", "RuleScriptTypeId", "ReprocessingValue", "ReprocessingInterval", "BuilderRuleScript")
                            SELECT v_model, 1, 0, 1, 'ReprocessHighValueWithCurrentRules', v_now - interval '120 days', 1, 'Tomasz Kowalski', 0, 1,
                                   'If (Payload.AmountEUR > 2000) Then' || chr(10) || '    Return True' || chr(10) || 'End If' || chr(10) || 'Return False', '2', 90, 'd', 'Return False'
                            WHERE NOT EXISTS (SELECT 1 FROM "EntityAnalysisModelReprocessingRule" WHERE "EntityAnalysisModelId" = v_model AND "Name" = 'ReprocessHighValueWithCurrentRules');

                            SELECT "Id" INTO v_reprocessing FROM "EntityAnalysisModelReprocessingRule" WHERE "EntityAnalysisModelId" = v_model AND "Name" = 'ReprocessHighValueWithCurrentRules' ORDER BY "Id" LIMIT 1;

                            IF v_reprocessing IS NOT NULL THEN
                                INSERT INTO "EntityAnalysisModelReprocessingRuleInstance" ("EntityAnalysisModelReprocessingRuleId", "StatusId", "CreatedDate", "StartedDate", "AvailableCount", "SampledCount",
                                    "MatchedCount", "ProcessedCount", "CompletedDate", "ErrorCount", "ReferenceDate", "Deleted", "CreatedUser", "Version")
                                SELECT v_reprocessing, i."Status", v_now - make_interval(days => i."DaysAgo"), v_now - make_interval(days => i."DaysAgo"), 24000, 24000, i."Matched", i."Processed",
                                       CASE WHEN i."Status" = 4 THEN v_now - make_interval(days => i."DaysAgo") + interval '14 minutes' END, i."Errors", v_now - make_interval(days => i."DaysAgo"), 0, 'Tomasz Kowalski', 1
                                FROM (VALUES (4, 110, 2310, 24000, 0), (4, 61, 2188, 24000, 0), (5, 20, 640, 9300, 12)) AS i("Status", "DaysAgo", "Matched", "Processed", "Errors");
                            END IF;

                            CREATE TEMP TABLE exe_import ON COMMIT DROP AS
                            SELECT s."Id" AS source_id, m AS month_ago,
                                   (v_now - make_interval(days => m * 30) - make_interval(hours => s."Id")) AS started,
                                   CASE WHEN s."Id" = 3 AND m = 4 THEN 0 WHEN s."Id" = 2 AND m = 9 THEN 0 ELSE 1 END AS ok
                            FROM "SanctionEntrySource" s CROSS JOIN generate_series(0, 11) m
                            WHERE s."Id" IN (1, 2, 3, 4);

                            INSERT INTO "SanctionEntryImport" ("SanctionEntrySourceId", "StartDate", "EndDate", "TotalRows", "InsertedCount", "RevivedCount", "UnchangedCount", "RemovedCount", "RejectedCount",
                                "Successful", "ErrorMessage", "CreatedUser", "CreatedDate")
                            SELECT i.source_id, i.started, i.started + interval '1 minute' * (3 + i.source_id * 2),
                                   CASE WHEN i.ok = 1 THEN 14000 + i.source_id * 3100 + (11 - i.month_ago) * 40 ELSE 0 END,
                                   CASE WHEN i.ok = 1 THEN 20 + (abs(hashtext('ins' || i.source_id || i.month_ago)) % 60) ELSE 0 END,
                                   CASE WHEN i.ok = 1 THEN abs(hashtext('rev' || i.source_id || i.month_ago)) % 5 ELSE 0 END,
                                   CASE WHEN i.ok = 1 THEN 13900 + i.source_id * 3100 ELSE 0 END,
                                   CASE WHEN i.ok = 1 THEN abs(hashtext('rem' || i.source_id || i.month_ago)) % 25 ELSE 0 END,
                                   CASE WHEN i.ok = 1 THEN abs(hashtext('rej' || i.source_id || i.month_ago)) % 6 ELSE 0 END,
                                   i.ok, CASE WHEN i.ok = 0 THEN 'The remote list could not be retrieved before the request timed out.' END, 'Automation', i.started
                            FROM exe_import i
                            WHERE NOT EXISTS (SELECT 1 FROM "SanctionEntryImport" x WHERE x."CreatedUser" = 'Automation' AND x."SanctionEntrySourceId" = i.source_id AND x."StartDate" = i.started);

                            INSERT INTO "SanctionEntryRejection" ("SanctionEntryImportId", "SanctionEntrySourceId", "RowNumber", "RawData", "CreatedDate", "ReasonId")
                            SELECT x."Id", x."SanctionEntrySourceId", 100 + (abs(hashtext('row' || x."Id" || g)) % 9000), 'malformed row', x."StartDate", 1 + (abs(hashtext('rs' || x."Id" || g)) % 3)
                            FROM "SanctionEntryImport" x
                            CROSS JOIN LATERAL generate_series(1, least(x."RejectedCount", 3)) g
                            WHERE x."CreatedUser" = 'Automation' AND x."RejectedCount" > 0;
                            INSERT INTO "SanctionEntry" ("SanctionEntryElementValue", "CreatedDate", "SanctionEntrySourceId", "SanctionEntryReference", "SanctionPayload", "SanctionEntryHash", "CreatedUser", "Deleted", "LastImportId")
                            SELECT 'Synthetic Listed Party ' || lpad(g::text, 4, '0'),
                                   (SELECT max(i."StartDate") FROM "SanctionEntryImport" i WHERE i."SanctionEntrySourceId" = s."Id" AND i."Successful" = 1 AND i."CreatedUser" = 'Automation') - make_interval(days => (g % 300)),
                                   s."Id", 'SYN-' || s."Id" || '-' || lpad(g::text, 4, '0'),
                                   'Synthetic Listed Party ' || lpad(g::text, 4, '0') || '|SYN-' || s."Id" || '-' || lpad(g::text, 4, '0'),
                                   md5('synthetic' || s."Id" || g), 'Automation', 0,
                                   (SELECT i."Id" FROM "SanctionEntryImport" i WHERE i."SanctionEntrySourceId" = s."Id" AND i."Successful" = 1 AND i."CreatedUser" = 'Automation' ORDER BY i."StartDate" DESC LIMIT 1)
                            FROM "SanctionEntrySource" s CROSS JOIN generate_series(1, 40) g
                            WHERE s."Id" IN (1, 2, 3, 4)
                              AND NOT EXISTS (SELECT 1 FROM "SanctionEntry" e WHERE e."SanctionEntryReference" = 'SYN-' || s."Id" || '-' || lpad(g::text, 4, '0'));
                        END
                        $seed$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
