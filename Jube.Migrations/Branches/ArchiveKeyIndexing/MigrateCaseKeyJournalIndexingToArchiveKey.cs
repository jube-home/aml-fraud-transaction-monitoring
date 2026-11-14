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

namespace Jube.Migrations.Branches.ArchiveKeyIndexing
{
    using FluentMigrator;

    [Migration(20263108470000)]
    public class MigrateCaseKeyJournalIndexingToArchiveKey : Migration
    {
        public override void Up()
        {
            CreateArchiveKeyBackfillTables();
            RetireCaseKeyJournalKeyIndexing();
            GuardRequestKeyIdentityOnSmallTables();
            AddCaseWorkflowXPathSource();
            AddLatestRiskToCase();
        }

        public override void Down()
        {
        }

        private void CreateArchiveKeyBackfillTables()
        {
            Execute.Sql("""
                        CREATE TABLE IF NOT EXISTS public."ArchiveKeyBackfillRun" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "HighWaterMarkId" bigint NOT NULL,
                            "LastArchiveId" bigint NOT NULL DEFAULT 0,
                            "StateId" smallint NOT NULL DEFAULT 1,
                            "Batches" integer NOT NULL DEFAULT 0,
                            "Inserted" bigint NOT NULL DEFAULT 0,
                            "CastFailed" bigint NOT NULL DEFAULT 0,
                            "ModelIds" text NULL,
                            "Error" text NULL,
                            "StartedDate" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                            "UpdatedDate" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'UTC'),
                            "CompletedDate" timestamp without time zone NULL
                        );

                        CREATE TABLE IF NOT EXISTS public."ArchiveKeyBackfillKey" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "RunId" bigint NOT NULL REFERENCES public."ArchiveKeyBackfillRun" ("Id"),
                            "EntityAnalysisModelId" integer NOT NULL,
                            "Key" text NOT NULL,
                            "DataTypeId" integer NOT NULL,
                            "Inserted" bigint NOT NULL DEFAULT 0,
                            "CastFailed" bigint NOT NULL DEFAULT 0,
                            "ArchiveWithKeyCount" bigint NULL,
                            "ArchiveKeyCount" bigint NULL,
                            "SampleCompared" integer NULL,
                            "SampleMismatched" integer NULL,
                            "StateId" smallint NOT NULL DEFAULT 1,
                            CONSTRAINT "UX_ArchiveKeyBackfillKey_Run_Model_Key" UNIQUE ("RunId", "EntityAnalysisModelId", "Key")
                        );

                        COMMENT ON TABLE public."ArchiveKeyBackfillRun" IS 'One row per run of the standalone script MigrateCaseKeyJournalIndexesToArchiveKey.sql, which writes ArchiveKey rows for historical Archive rows so that every field flagged Archive Key is indexed for its whole history, not only from the moment it was flagged. Holds the high-water mark and the resume point, so an interrupted run continues where it stopped.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."Id" IS 'Server-assigned identity of the run.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."HighWaterMarkId" IS 'The newest Archive identity the run covers. Rows above it are younger than the settle interval and are left to the engine, which writes their ArchiveKey rows itself.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."LastArchiveId" IS 'The Archive identity the last committed batch finished at; the run resumes from here.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."StateId" IS '1 Running, 2 Backfilled (every batch committed, not yet validated), 3 Validated (every key passed validation), 4 Retired (the old Case Key Journal expression indexes were marked unused), 9 Failed (see Error).';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."Batches" IS 'How many batches have been committed.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."Inserted" IS 'How many ArchiveKey rows the run inserted.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."CastFailed" IS 'How many payload values could not be cast to the field data type and so were not keyed, as the engine would also have skipped them.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."ModelIds" IS 'The model identities the run was restricted to, as a comma-separated list, or null for every active model.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."Error" IS 'The error that failed the run, null otherwise.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."StartedDate" IS 'When the run started (UTC).';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."UpdatedDate" IS 'When the run last committed a batch or changed state (UTC).';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillRun"."CompletedDate" IS 'When the run reached the Retired state (UTC), null until it has.';

                        COMMENT ON TABLE public."ArchiveKeyBackfillKey" IS 'One row per model and field flagged Archive Key within a backfill run, carrying the counts the run inserted and the validation that compares the Archive rows carrying the field with the ArchiveKey rows for it.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."Id" IS 'Server-assigned identity of the record.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."RunId" IS 'The run this key belongs to.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."EntityAnalysisModelId" IS 'The model whose request XPath this is. Key names repeat across models, so every count is scoped to the model through Archive.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."Key" IS 'The request XPath name, which is the ArchiveKey key.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."DataTypeId" IS 'The field data type: 1 String, 2 Integer, 3 Float, 4 Date, 5 Boolean, 6 Latitude, 7 Longitude. It decides which typed ArchiveKey column the value is written to, as in the engine.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."Inserted" IS 'How many ArchiveKey rows the run inserted for this key.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."CastFailed" IS 'How many payload values for this key could not be cast to the data type and were not keyed.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."ArchiveWithKeyCount" IS 'Validation: how many Archive rows of the model up to the high-water mark carry the field in their payload.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."ArchiveKeyCount" IS 'Validation: how many ArchiveKey rows exist for the key on those Archive rows.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."SampleCompared" IS 'Validation: how many randomly sampled rows had their typed ArchiveKey value compared with the payload value.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."SampleMismatched" IS 'Validation: how many of the sampled rows disagreed.';
                        COMMENT ON COLUMN public."ArchiveKeyBackfillKey"."StateId" IS '1 Pending, 2 Backfilled, 3 Valid (counts reconcile within tolerance and no sample mismatch), 4 Invalid.';
                        """);
        }

        private void RetireCaseKeyJournalKeyIndexing()
        {
            Execute.Sql("""
                        COMMENT ON TABLE public."CaseKeyJournalKeyIndex" IS 'Register of the per-key expression indexes on Archive that the retired Case Key Journal indexing job built. The journal no longer uses them: every key lookup predicates through ArchiveKey. Rows remain so that the retired indexes can be dropped by hand once the standalone migration script has marked them Retired.';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."StateId" IS '1 Building, 2 Ready, 3 Failed (states of the retired indexing job), 4 Retired (superseded by ArchiveKey; the index still exists and is safe to drop by hand).';
                        COMMENT ON TABLE public."ArchiveKey" IS 'One typed row per archived transaction and field flagged Archive Key, written by the engine beside the Archive row. It is the only index the Case and Cases pages use to find the transactions carrying a value in a field (journal, lanes, drill, suppression, holds, graph): a field that is not flagged Archive Key cannot be looked up. Request XPath fields are processing type 1 and are unique per transaction and key.';
                        """);
        }

        private void AddCaseWorkflowXPathSource()
        {
            Execute.Sql("""
                        ALTER TABLE public."CaseWorkflowXPath" ADD COLUMN IF NOT EXISTS "SourceKindId" smallint NULL;
                        ALTER TABLE public."CaseWorkflowXPath" ADD COLUMN IF NOT EXISTS "SourceGuid" uuid NULL;
                        ALTER TABLE public."CaseWorkflowXPathVersion" ADD COLUMN IF NOT EXISTS "SourceKindId" smallint NULL;
                        ALTER TABLE public."CaseWorkflowXPathVersion" ADD COLUMN IF NOT EXISTS "SourceGuid" uuid NULL;
                        ALTER TABLE public."CaseWorkflowXPath" ALTER COLUMN "XPath" DROP NOT NULL;

                        CREATE TEMP TABLE cwx_source AS
                        SELECT x."Id",
                               w."EntityAnalysisModelId" AS model,
                               regexp_replace(regexp_replace(regexp_replace(btrim(x."XPath"), '^\$\.?', ''), '^(payload|data)\.', ''),
                                              '^payload\[''([^'']+)''\]$', '\1') AS name,
                               CASE WHEN btrim(x."XPath") ~ '^\$?\.?(payload|data)[.\[]' OR btrim(x."XPath") !~ '\.' THEN 'payload'
                                    ELSE split_part(regexp_replace(btrim(x."XPath"), '^\$\.?', ''), '.', 1) END AS facet
                        FROM public."CaseWorkflowXPath" x
                        JOIN public."CaseWorkflow" w ON w."Id" = x."CaseWorkflowId"
                        WHERE x."SourceGuid" IS NULL AND x."XPath" IS NOT NULL;

                        UPDATE cwx_source s SET name = regexp_replace(s.name, '^[A-Za-z]+\.', '') WHERE s.facet <> 'payload' AND s.name LIKE s.facet || '.%';

                        UPDATE public."CaseWorkflowXPath" x SET "SourceKindId" = 6, "SourceGuid" = r."Guid"
                        FROM cwx_source s
                        JOIN LATERAL (SELECT r."Guid" FROM public."EntityAnalysisModelRequestXpath" r
                                      WHERE r."EntityAnalysisModelId" = s.model AND r."Name" = s.name AND coalesce(r."Deleted", 0) = 0
                                      ORDER BY coalesce(r."Active", 0) DESC, r."Id" LIMIT 1) r ON TRUE
                        WHERE x."Id" = s."Id" AND s.facet = 'payload';

                        UPDATE public."CaseWorkflowXPath" x SET "SourceKindId" = 8, "SourceGuid" = f."Guid"
                        FROM cwx_source s
                        JOIN LATERAL (SELECT f."Guid" FROM public."EntityAnalysisModelInlineFunction" f
                                      WHERE f."EntityAnalysisModelId" = s.model AND f."Name" = s.name AND coalesce(f."Deleted", 0) = 0
                                      ORDER BY coalesce(f."Active", 0) DESC, f."Id" LIMIT 1) f ON TRUE
                        WHERE x."Id" = s."Id" AND x."SourceGuid" IS NULL AND s.facet = 'payload';

                        UPDATE public."CaseWorkflowXPath" x SET "SourceKindId" = 1, "SourceGuid" = m."Guid"
                        FROM cwx_source s
                        JOIN public."EntityAnalysisModel" m ON m."Id" = s.model
                        WHERE x."Id" = s."Id" AND x."SourceGuid" IS NULL AND s.facet = 'payload' AND m."EntryName" = s.name;

                        UPDATE public."CaseWorkflowXPath" x SET "SourceKindId" = 11, "SourceGuid" = a."Guid"
                        FROM cwx_source s
                        JOIN LATERAL (SELECT a."Guid" FROM public."EntityAnalysisModelAbstractionRule" a
                                      WHERE a."EntityAnalysisModelId" = s.model AND a."Name" = s.name AND coalesce(a."Deleted", 0) = 0
                                      ORDER BY coalesce(a."Active", 0) DESC, a."Id" LIMIT 1) a ON TRUE
                        WHERE x."Id" = s."Id" AND x."SourceGuid" IS NULL AND s.facet = 'abstraction';

                        UPDATE public."CaseWorkflowXPath" x SET "SourceKindId" = 12, "SourceGuid" = a."Guid"
                        FROM cwx_source s
                        JOIN LATERAL (SELECT a."Guid" FROM public."EntityAnalysisModelAbstractionCalculation" a
                                      WHERE a."EntityAnalysisModelId" = s.model AND a."Name" = s.name AND coalesce(a."Deleted", 0) = 0
                                      ORDER BY coalesce(a."Active", 0) DESC, a."Id" LIMIT 1) a ON TRUE
                        WHERE x."Id" = s."Id" AND x."SourceGuid" IS NULL AND s.facet = 'abstractionCalculation';

                        UPDATE public."CaseWorkflowXPath" x SET "SourceKindId" = 13, "SourceGuid" = t."Guid"
                        FROM cwx_source s
                        JOIN LATERAL (SELECT t."Guid" FROM public."EntityAnalysisModelTtlCounter" t
                                      WHERE t."EntityAnalysisModelId" = s.model AND t."Name" = s.name AND coalesce(t."Deleted", 0) = 0
                                      ORDER BY coalesce(t."Active", 0) DESC, t."Id" LIMIT 1) t ON TRUE
                        WHERE x."Id" = s."Id" AND x."SourceGuid" IS NULL AND s.facet = 'ttlCounter';

                        UPDATE public."CaseWorkflowXPath" x SET "SourceKindId" = 14, "SourceGuid" = h."Guid"
                        FROM cwx_source s
                        JOIN LATERAL (SELECT h."Guid" FROM public."EntityAnalysisModelHttpAdaptation" h
                                      WHERE h."EntityAnalysisModelId" = s.model AND h."Name" = s.name AND coalesce(h."Deleted", 0) = 0
                                      ORDER BY coalesce(h."Active", 0) DESC, h."Id" LIMIT 1) h ON TRUE
                        WHERE x."Id" = s."Id" AND x."SourceGuid" IS NULL AND lower(s.facet) = 'httpadaptation';

                        UPDATE public."CaseWorkflowXPath" x SET "SourceKindId" = 16, "SourceGuid" = r."Guid"
                        FROM cwx_source s
                        JOIN LATERAL (SELECT r."Guid" FROM public."EntityAnalysisModelActivationRule" r
                                      WHERE r."EntityAnalysisModelId" = s.model AND r."Name" = s.name AND coalesce(r."Deleted", 0) = 0
                                      ORDER BY coalesce(r."Active", 0) DESC, r."Id" LIMIT 1) r ON TRUE
                        WHERE x."Id" = s."Id" AND x."SourceGuid" IS NULL AND s.facet = 'activation';

                        DO $unmapped$
                        DECLARE v_count integer;
                        BEGIN
                            UPDATE public."CaseWorkflowXPath" SET "Active" = 0
                            WHERE "SourceGuid" IS NULL AND coalesce("Deleted", 0) = 0 AND coalesce("Active", 0) = 1;
                            GET DIAGNOSTICS v_count = ROW_COUNT;
                            IF v_count > 0 THEN
                                RAISE NOTICE 'CaseWorkflowXPath: % rows whose XPath named no field of the workflow model were deactivated; pick a source field to reactivate them.', v_count;
                            END IF;
                        END
                        $unmapped$;

                        DROP TABLE cwx_source;

                        COMMENT ON COLUMN public."CaseWorkflowXPath"."SourceKindId" IS 'The kind of model entity the extraction reads, by the EntityApprovalKind numbering: 1 the model entry key, 6 request XPath, 8 inline function, 11 abstraction rule, 12 abstraction calculation, 13 TTL counter, 14 HTTP adaptation, 16 activation rule. With SourceGuid it replaces the free text XPath: the JSON path, the archive key and the data type all derive from the entity.';
                        COMMENT ON COLUMN public."CaseWorkflowXPath"."SourceGuid" IS 'The Guid of the model entity the extraction reads (the model Guid for the entry key). The extraction is drillable exactly when that entity is flagged Archive Key.';
                        COMMENT ON COLUMN public."CaseWorkflowXPath"."XPath" IS 'Deprecated: the free text JSON path the extraction used before SourceKindId and SourceGuid named the field. Migrated by name, never read; rows whose XPath matched no field were deactivated.';
                        COMMENT ON COLUMN public."CaseWorkflowXPath"."Drill" IS 'Deprecated: drill is no longer a flag. An extraction can be drilled exactly when its source field is flagged Archive Key.';
                        COMMENT ON COLUMN public."CaseWorkflowXPathVersion"."SourceKindId" IS 'The SourceKindId of the earlier version.';
                        COMMENT ON COLUMN public."CaseWorkflowXPathVersion"."SourceGuid" IS 'The SourceGuid of the earlier version.';
                        """);
        }

        private void AddLatestRiskToCase()
        {
            Execute.Sql("""
                        ALTER TABLE public."Case" ADD COLUMN IF NOT EXISTS "RiskLaneId" smallint NULL;
                        ALTER TABLE public."Case" ADD COLUMN IF NOT EXISTS "RiskPriority" integer NULL;
                        ALTER TABLE public."Case" ADD COLUMN IF NOT EXISTS "RiskLikelihoodProbability" double precision NULL;
                        ALTER TABLE public."Case" ADD COLUMN IF NOT EXISTS "RiskNextDeadlineDate" timestamp without time zone NULL;
                        ALTER TABLE public."Case" ADD COLUMN IF NOT EXISTS "RiskValue" double precision NULL;
                        ALTER TABLE public."Case" ADD COLUMN IF NOT EXISTS "RiskSequence" integer NULL;

                        UPDATE public."Case" c
                        SET "RiskLaneId" = l."LaneId", "RiskPriority" = l."Priority",
                            "RiskLikelihoodProbability" = l."LikelihoodProbability", "RiskNextDeadlineDate" = l."NextDeadlineDate",
                            "RiskValue" = (l."ImpactComponentsJson" ->> 'value')::double precision, "RiskSequence" = l."Sequence"
                        FROM (SELECT DISTINCT ON (a."CaseId") a.* FROM public."CaseRiskAssessment" a ORDER BY a."CaseId", a."Sequence" DESC) l
                        WHERE l."CaseId" = c."Id" AND c."RiskSequence" IS DISTINCT FROM l."Sequence";

                        COMMENT ON COLUMN public."Case"."RiskLaneId" IS 'The lane of the latest risk assessment of the case, copied from CaseRiskAssessment when an assessment is written so that the Cases page can order and filter by it from an index. CaseRiskAssessment remains the history.';
                        COMMENT ON COLUMN public."Case"."RiskPriority" IS 'The Priority of the latest risk assessment, copied as RiskLaneId is.';
                        COMMENT ON COLUMN public."Case"."RiskLikelihoodProbability" IS 'The likelihood probability of the latest risk assessment, copied as RiskLaneId is.';
                        COMMENT ON COLUMN public."Case"."RiskNextDeadlineDate" IS 'The next deadline of the latest risk assessment (UTC), copied as RiskLaneId is.';
                        COMMENT ON COLUMN public."Case"."RiskValue" IS 'The money value the latest risk assessment weighed, from its impact components, copied as RiskLaneId is.';
                        COMMENT ON COLUMN public."Case"."RiskSequence" IS 'The Sequence of the CaseRiskAssessment row the Risk columns were copied from; null until the case has been assessed.';
                        """);
        }

        private void GuardRequestKeyIdentityOnSmallTables()
        {
            Execute.Sql("""
                        DO $archivekey$
                        BEGIN
                            IF (SELECT coalesce(reltuples, 0) FROM pg_class WHERE oid = 'public."ArchiveKey"'::regclass) < 1000000
                               AND to_regclass('public."IX_ArchiveKey_Entry_Request"') IS NULL THEN
                                DELETE FROM public."ArchiveKeyVersion" v
                                WHERE v."ArchiveKeyId" IN (
                                    SELECT k."Id" FROM public."ArchiveKey" k
                                    JOIN (SELECT "EntityAnalysisModelInstanceEntryGuid" AS guid, "Key" AS key, min("Id") AS keep
                                          FROM public."ArchiveKey" WHERE "ProcessingTypeId" = 1
                                          GROUP BY 1, 2 HAVING count(*) > 1) d
                                      ON d.guid = k."EntityAnalysisModelInstanceEntryGuid" AND d.key = k."Key" AND k."ProcessingTypeId" = 1
                                    WHERE k."Id" <> d.keep);

                                DELETE FROM public."ArchiveKey" k
                                USING (SELECT "EntityAnalysisModelInstanceEntryGuid" AS guid, "Key" AS key, min("Id") AS keep
                                       FROM public."ArchiveKey" WHERE "ProcessingTypeId" = 1
                                       GROUP BY 1, 2 HAVING count(*) > 1) d
                                WHERE d.guid = k."EntityAnalysisModelInstanceEntryGuid" AND d.key = k."Key"
                                  AND k."ProcessingTypeId" = 1 AND k."Id" <> d.keep;

                                CREATE UNIQUE INDEX IF NOT EXISTS "IX_ArchiveKey_Entry_Request"
                                    ON public."ArchiveKey" ("EntityAnalysisModelInstanceEntryGuid", "Key")
                                    WHERE "ProcessingTypeId" = 1;
                            END IF;
                        END
                        $archivekey$;
                        """);
        }
    }
}
