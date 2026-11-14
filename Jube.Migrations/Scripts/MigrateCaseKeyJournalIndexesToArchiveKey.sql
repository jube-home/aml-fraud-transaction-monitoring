-- MigrateCaseKeyJournalIndexesToArchiveKey.sql
--
-- Moves the Case Key Journal from the per-key expression indexes on Archive (IX_Archive_Journal_*) to the
-- ArchiveKey table, which is the only index the Case and Cases pages use from now on. It writes an ArchiveKey
-- row for every historical Archive row and every request XPath flagged Archive Key, validates the result and
-- then marks the old indexes Retired without dropping them. The DROP statements are printed at the end for a
-- manual step outside the band.
--
-- Run as the migration role (the owner of Archive, ArchiveKey and the journal indexes), after the application
-- migrations, with reprocessing paused:
--
--     psql "$MIGRATION_CONNECTION" -v ON_ERROR_STOP=1 -f MigrateCaseKeyJournalIndexesToArchiveKey.sql
--
-- Settings, all optional, set in the same session before running (SET jube.backfill_batch_size = '5000'):
--   jube.backfill_batch_size      Archive identities per committed batch (5000)
--   jube.backfill_pause_seconds   pause between batches (0.05)
--   jube.backfill_settle          leave Archive rows younger than this to the engine ('10 minutes')
--   jube.backfill_max_batches     stop after this many batches and resume on the next run (0 = all)
--   jube.backfill_model_ids       restrict to these model identities, comma separated ('' = every active model)
--   jube.backfill_sample_percent  TABLESAMPLE percentage for the value comparison ('1')
--   jube.backfill_sample          at most this many sampled rows per key (500)
--   jube.backfill_tolerance       allowed relative count difference per key (0.001)
--   jube.backfill_reset           'true' starts a new run instead of resuming the open one ('false')
--
-- Every top-level statement is separated by a "-- @jube:step" line so that a runner other than psql can send
-- them one at a time: the procedures commit inside and the CONCURRENTLY statements cannot run in a transaction.
-- The script is idempotent: a second run inserts nothing and resumes or re-validates.

-- @jube:step preconditions
DO
$s$
    BEGIN
        IF to_regclass('public."ArchiveKeyBackfillRun"') IS NULL OR
           to_regclass('public."ArchiveKeyBackfillKey"') IS NULL THEN
            RAISE EXCEPTION 'The ArchiveKeyBackfill tables are missing: run the application migrations first.';
        END IF;

        IF current_setting('server_version_num')::int < 160000 THEN
            RAISE EXCEPTION 'PostgreSQL 16 or later is required (pg_input_is_valid).';
        END IF;
    END
$s$;

-- @jube:step create_dedupe_procedure
CREATE OR REPLACE PROCEDURE public."ArchiveKeyBackfill_Dedupe"()
    LANGUAGE plpgsql AS
$p$
DECLARE
    v_batch   int    := coalesce(nullif(current_setting('jube.backfill_batch_size', true), '')::int, 5000);
    v_pause   float8 := coalesce(nullif(current_setting('jube.backfill_pause_seconds', true), '')::float8, 0.05);
    v_deleted bigint;
    v_total   bigint := 0;
BEGIN
    PERFORM set_config('lock_timeout', '5s', false);

    CREATE TEMP TABLE IF NOT EXISTS archive_key_dupes
    (
        id bigint PRIMARY KEY
    ) ON COMMIT PRESERVE ROWS;
    TRUNCATE archive_key_dupes;

    INSERT INTO archive_key_dupes (id)
    SELECT k."Id"
    FROM public."ArchiveKey" k
             JOIN (SELECT "EntityAnalysisModelInstanceEntryGuid" AS guid, "Key" AS key, min("Id") AS keep
                   FROM public."ArchiveKey"
                   WHERE "ProcessingTypeId" = 1
                   GROUP BY 1, 2
                   HAVING count(*) > 1) d
                  ON d.guid = k."EntityAnalysisModelInstanceEntryGuid" AND d.key = k."Key" AND k."ProcessingTypeId" = 1
    WHERE k."Id" <> d.keep;
    COMMIT;

    LOOP
        WITH chunk AS (SELECT id FROM archive_key_dupes ORDER BY id LIMIT v_batch),
             v AS (DELETE FROM public."ArchiveKeyVersion" WHERE "ArchiveKeyId" IN (SELECT id FROM chunk)),
             k AS (DELETE FROM public."ArchiveKey" WHERE "Id" IN (SELECT id FROM chunk))
        DELETE
        FROM archive_key_dupes
        WHERE id IN (SELECT id FROM chunk);

        GET DIAGNOSTICS v_deleted = ROW_COUNT;
        v_total := v_total + v_deleted;
        COMMIT;

        EXIT WHEN v_deleted = 0;
        PERFORM pg_sleep(v_pause);
    END LOOP;

    RAISE NOTICE 'ArchiveKey dedupe removed % duplicate request key rows.', v_total;
END
$p$;

-- @jube:step dedupe
CALL public."ArchiveKeyBackfill_Dedupe"();

-- @jube:step unique_index_attempt_1
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS "IX_ArchiveKey_Entry_Request"
    ON public."ArchiveKey" ("EntityAnalysisModelInstanceEntryGuid", "Key")
    WHERE "ProcessingTypeId" = 1;

-- @jube:step unique_index_check_1
DO
$s$
    DECLARE
        v_valid boolean;
    BEGIN
        SELECT i.indisvalid
        INTO v_valid
        FROM pg_index i
                 JOIN pg_class c ON c.oid = i.indexrelid
        WHERE c.relname = 'IX_ArchiveKey_Entry_Request'
          AND c.relnamespace = 'public'::regnamespace;

        IF v_valid IS FALSE THEN
            ALTER INDEX public."IX_ArchiveKey_Entry_Request" RENAME TO "IX_ArchiveKey_Entry_Request_invalid";
            RAISE NOTICE 'The unique index build failed because duplicates reappeared; it is retried once after a second dedupe.';
        END IF;
    END
$s$;

-- @jube:step drop_invalid_1
DROP INDEX CONCURRENTLY IF EXISTS public."IX_ArchiveKey_Entry_Request_invalid";

-- @jube:step dedupe_2
CALL public."ArchiveKeyBackfill_Dedupe"();

-- @jube:step unique_index_attempt_2
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS "IX_ArchiveKey_Entry_Request"
    ON public."ArchiveKey" ("EntityAnalysisModelInstanceEntryGuid", "Key")
    WHERE "ProcessingTypeId" = 1;

-- @jube:step unique_index_check_2
DO
$s$
    DECLARE
        v_valid boolean;
    BEGIN
        SELECT i.indisvalid
        INTO v_valid
        FROM pg_index i
                 JOIN pg_class c ON c.oid = i.indexrelid
        WHERE c.relname = 'IX_ArchiveKey_Entry_Request'
          AND c.relnamespace = 'public'::regnamespace;

        IF v_valid IS FALSE THEN
            ALTER INDEX public."IX_ArchiveKey_Entry_Request" RENAME TO "IX_ArchiveKey_Entry_Request_invalid";
            RAISE NOTICE 'The unique index could not be built; the backfill continues with NOT EXISTS only and the engine keeps its read-then-insert path.';
        END IF;
    END
$s$;

-- @jube:step drop_invalid_2
DROP INDEX CONCURRENTLY IF EXISTS public."IX_ArchiveKey_Entry_Request_invalid";

-- @jube:step create_backfill_procedure
CREATE OR REPLACE PROCEDURE public."ArchiveKeyBackfill_Run"()
    LANGUAGE plpgsql AS
$p$
DECLARE
    v_batch        bigint   := coalesce(nullif(current_setting('jube.backfill_batch_size', true), '')::bigint, 5000);
    v_pause        float8   := coalesce(nullif(current_setting('jube.backfill_pause_seconds', true), '')::float8, 0.05);
    v_settle       interval := coalesce(nullif(current_setting('jube.backfill_settle', true), '')::interval,
                                        interval '10 minutes');
    v_max_batches  int      := coalesce(nullif(current_setting('jube.backfill_max_batches', true), '')::int, 0);
    v_models       text     := nullif(current_setting('jube.backfill_model_ids', true), '');
    v_reset        boolean  := coalesce(nullif(current_setting('jube.backfill_reset', true), '')::boolean, false);
    v_use_conflict boolean;
    v_run          bigint;
    v_hwm          bigint;
    v_lo           bigint;
    v_hi           bigint;
    v_batches      int      := 0;
    v_inserted     bigint;
    v_failed       bigint;
    v_sql          text;
    v_error        text;
BEGIN
    PERFORM set_config('TimeZone', 'UTC', false);
    PERFORM set_config('lock_timeout', '5s', false);

    IF NOT pg_try_advisory_lock(7304012) THEN
        RAISE EXCEPTION 'Another ArchiveKey backfill holds advisory lock 7304012.';
    END IF;

    SELECT coalesce(bool_or(i.indisvalid), false)
    INTO v_use_conflict
    FROM pg_index i
             JOIN pg_class c ON c.oid = i.indexrelid
    WHERE c.relname = 'IX_ArchiveKey_Entry_Request'
      AND c.relnamespace = 'public'::regnamespace;

    SELECT "Id"
    INTO v_run
    FROM public."ArchiveKeyBackfillRun"
    WHERE "StateId" = 1
      AND "ModelIds" IS NOT DISTINCT FROM v_models
    ORDER BY "Id" DESC
    LIMIT 1;

    IF v_run IS NULL OR v_reset THEN
        IF v_run IS NOT NULL THEN
            UPDATE public."ArchiveKeyBackfillRun"
            SET "StateId"     = 9,
                "Error"       = 'Reset by a later run.',
                "UpdatedDate" = now() AT TIME ZONE 'UTC'
            WHERE "Id" = v_run;
        END IF;

        SELECT coalesce((SELECT min(y."Id") - 1
                         FROM public."Archive" y
                         WHERE y."CreatedDate" >= (now() AT TIME ZONE 'UTC') - v_settle),
                        (SELECT max(a."Id") FROM public."Archive" a),
                        0)
        INTO v_hwm;

        INSERT INTO public."ArchiveKeyBackfillRun" ("HighWaterMarkId", "ModelIds")
        VALUES (v_hwm, v_models)
        RETURNING "Id" INTO v_run;

        INSERT INTO public."ArchiveKeyBackfillKey" ("RunId", "EntityAnalysisModelId", "Key", "DataTypeId")
        SELECT v_run, x."EntityAnalysisModelId", x."Name", coalesce(x."DataTypeId", 1)
        FROM public."EntityAnalysisModelRequestXpath" x
                 JOIN public."EntityAnalysisModel" m ON m."Id" = x."EntityAnalysisModelId"
        WHERE x."ReportTable" = 1
          AND coalesce(x."Active", 0) = 1
          AND coalesce(x."Deleted", 0) = 0
          AND coalesce(m."Active", 0) = 1
          AND coalesce(m."Deleted", 0) = 0
          AND (v_models IS NULL OR x."EntityAnalysisModelId" = ANY (string_to_array(v_models, ',')::int[]))
        ON CONFLICT ("RunId", "EntityAnalysisModelId", "Key") DO NOTHING;
    END IF;

    SELECT "HighWaterMarkId", "LastArchiveId"
    INTO v_hwm, v_lo
    FROM public."ArchiveKeyBackfillRun"
    WHERE "Id" = v_run;

    COMMIT;

    v_sql := format($q$
        WITH src AS (
            SELECT a."Id" AS archive_id, a."EntityAnalysisModelInstanceEntryGuid" AS guid,
                   x."Id" AS key_id, x."Key" AS key, x."DataTypeId" AS dt, v.txt,
                   CASE WHEN x."DataTypeId" = 2 THEN pg_input_is_valid(v.txt, 'integer')
                        WHEN x."DataTypeId" IN (3, 6, 7) THEN pg_input_is_valid(v.txt, 'float8')
                        WHEN x."DataTypeId" = 4 THEN pg_input_is_valid(v.txt, 'timestamptz')
                        ELSE true END AS ok
            FROM public."Archive" a
            JOIN public."ArchiveKeyBackfillKey" x
              ON x."RunId" = $1 AND x."EntityAnalysisModelId" = a."EntityAnalysisModelId"
            CROSS JOIN LATERAL (SELECT a."Json" -> 'payload' ->> x."Key" AS txt) v
            WHERE a."Id" > $2 AND a."Id" <= $3
              AND a."EntityAnalysisModelInstanceEntryGuid" IS NOT NULL
              AND v.txt IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM public."ArchiveKey" e
                              WHERE e."EntityAnalysisModelInstanceEntryGuid" = a."EntityAnalysisModelInstanceEntryGuid"
                                AND e."ProcessingTypeId" = 1 AND e."Key" = x."Key")
        ), ins AS (
            INSERT INTO public."ArchiveKey" ("ProcessingTypeId", "Key", "KeyValueString", "KeyValueInteger", "KeyValueFloat",
                                             "KeyValueBoolean", "KeyValueDate", "EntityAnalysisModelInstanceEntryGuid", "Version")
            SELECT 1, s.key,
                   CASE WHEN s.dt NOT IN (2, 3, 4, 5, 6, 7) THEN s.txt END,
                   CASE WHEN s.dt = 2 THEN s.txt::int END,
                   CASE WHEN s.dt IN (3, 6, 7) THEN s.txt::float8 END,
                   CASE WHEN s.dt = 5 THEN (CASE WHEN lower(s.txt) = 'true' OR s.txt = '1' THEN 1 ELSE 0 END)::smallint END,
                   CASE WHEN s.dt = 4 THEN (s.txt::timestamptz AT TIME ZONE 'UTC') END,
                   s.guid, 1
            FROM src s
            WHERE s.ok
            %s
            RETURNING "Key", "EntityAnalysisModelInstanceEntryGuid"
        ), per_key AS (
            SELECT s.key_id,
                   count(i."Key") AS inserted,
                   count(*) FILTER (WHERE NOT s.ok) AS failed
            FROM src s
            LEFT JOIN ins i ON i."Key" = s.key AND i."EntityAnalysisModelInstanceEntryGuid" = s.guid
            GROUP BY s.key_id
        ), upd AS (
            UPDATE public."ArchiveKeyBackfillKey" k
            SET "Inserted" = k."Inserted" + p.inserted, "CastFailed" = k."CastFailed" + p.failed
            FROM per_key p
            WHERE k."Id" = p.key_id
        )
        SELECT (SELECT count(*) FROM ins), (SELECT count(*) FROM src WHERE NOT ok)
        $q$,
                    CASE
                        WHEN v_use_conflict
                            THEN 'ON CONFLICT ("EntityAnalysisModelInstanceEntryGuid", "Key") WHERE "ProcessingTypeId" = 1 DO NOTHING'
                        ELSE '' END);

    WHILE v_lo < v_hwm
        LOOP
            v_hi := least(v_lo + v_batch, v_hwm);

            BEGIN
                EXECUTE v_sql INTO v_inserted, v_failed USING v_run, v_lo, v_hi;
            EXCEPTION
                WHEN OTHERS THEN
                    v_error := left(SQLERRM, 500);
            END;

            EXIT WHEN v_error IS NOT NULL;

            UPDATE public."ArchiveKeyBackfillRun"
            SET "LastArchiveId" = v_hi,
                "Batches"       = "Batches" + 1,
                "Inserted"      = "Inserted" + v_inserted,
                "CastFailed"    = "CastFailed" + v_failed,
                "UpdatedDate"   = now() AT TIME ZONE 'UTC'
            WHERE "Id" = v_run;

            COMMIT;

            v_lo := v_hi;
            v_batches := v_batches + 1;

            EXIT WHEN v_max_batches > 0 AND v_batches >= v_max_batches;
            PERFORM pg_sleep(v_pause);
        END LOOP;

    IF v_lo >= v_hwm THEN
        UPDATE public."ArchiveKeyBackfillRun"
        SET "StateId"     = 2,
            "UpdatedDate" = now() AT TIME ZONE 'UTC'
        WHERE "Id" = v_run;

        UPDATE public."ArchiveKeyBackfillKey"
        SET "StateId" = 2
        WHERE "RunId" = v_run
          AND "StateId" = 1;

        COMMIT;
        RAISE NOTICE 'ArchiveKey backfill run % completed up to Archive identity %.', v_run, v_hwm;
    ELSE
        RAISE NOTICE 'ArchiveKey backfill run % paused at Archive identity % of %; run the script again to resume.', v_run, v_lo, v_hwm;
    END IF;

    IF v_error IS NOT NULL THEN
        UPDATE public."ArchiveKeyBackfillRun"
        SET "StateId"     = 9,
            "Error"       = v_error,
            "UpdatedDate" = now() AT TIME ZONE 'UTC'
        WHERE "Id" = v_run;

        COMMIT;
        PERFORM pg_advisory_unlock(7304012);
        RAISE EXCEPTION 'ArchiveKey backfill run % failed: %', v_run, v_error;
    END IF;

    PERFORM pg_advisory_unlock(7304012);
END
$p$;

-- @jube:step backfill
CALL public."ArchiveKeyBackfill_Run"();

-- @jube:step create_validate_procedure
CREATE OR REPLACE PROCEDURE public."ArchiveKeyBackfill_Validate"()
    LANGUAGE plpgsql AS
$p$
DECLARE
    v_models    text   := nullif(current_setting('jube.backfill_model_ids', true), '');
    v_sample    int    := coalesce(nullif(current_setting('jube.backfill_sample', true), '')::int, 500);
    v_percent   float8 := coalesce(nullif(current_setting('jube.backfill_sample_percent', true), '')::float8, 1);
    v_tolerance float8 := coalesce(nullif(current_setting('jube.backfill_tolerance', true), '')::float8, 0.001);
    v_run       bigint;
    v_hwm       bigint;
    r           record;
BEGIN
    PERFORM set_config('TimeZone', 'UTC', false);

    SELECT "Id", "HighWaterMarkId"
    INTO v_run, v_hwm
    FROM public."ArchiveKeyBackfillRun"
    WHERE "StateId" IN (2, 3, 4)
      AND "ModelIds" IS NOT DISTINCT FROM v_models
    ORDER BY "Id" DESC
    LIMIT 1;

    IF v_run IS NULL THEN
        RAISE NOTICE 'There is no completed backfill run to validate.';
        RETURN;
    END IF;

    FOR r IN SELECT DISTINCT "EntityAnalysisModelId" AS model FROM public."ArchiveKeyBackfillKey" WHERE "RunId" = v_run
        LOOP
            UPDATE public."ArchiveKeyBackfillKey" k
            SET "ArchiveWithKeyCount" = c.n
            FROM (SELECT x."Id" AS key_id, count(*) AS n
                  FROM public."Archive" a
                           JOIN public."ArchiveKeyBackfillKey" x
                                ON x."RunId" = v_run AND x."EntityAnalysisModelId" = a."EntityAnalysisModelId"
                  WHERE a."EntityAnalysisModelId" = r.model
                    AND a."Id" <= v_hwm
                    AND (a."Json" -> 'payload' ->> x."Key") IS NOT NULL
                  GROUP BY x."Id") c
            WHERE k."Id" = c.key_id;

            UPDATE public."ArchiveKeyBackfillKey"
            SET "ArchiveWithKeyCount" = 0
            WHERE "RunId" = v_run
              AND "EntityAnalysisModelId" = r.model
              AND "ArchiveWithKeyCount" IS NULL;

            COMMIT;
        END LOOP;

    FOR r IN SELECT "Id", "EntityAnalysisModelId" AS model, "Key", "DataTypeId" AS dt
             FROM public."ArchiveKeyBackfillKey"
             WHERE "RunId" = v_run
        LOOP
            UPDATE public."ArchiveKeyBackfillKey"
            SET "ArchiveKeyCount" = (SELECT count(*)
                                     FROM public."ArchiveKey" k
                                              JOIN public."Archive" a ON a."EntityAnalysisModelInstanceEntryGuid" =
                                                                         k."EntityAnalysisModelInstanceEntryGuid"
                                     WHERE k."ProcessingTypeId" = 1
                                       AND k."Key" = r."Key"
                                       AND a."EntityAnalysisModelId" = r.model
                                       AND a."Id" <= v_hwm)
            WHERE "Id" = r."Id";

            WITH s AS (SELECT a."EntityAnalysisModelInstanceEntryGuid" AS guid, a."Json" -> 'payload' ->> r."Key" AS txt
                       FROM public."Archive" a TABLESAMPLE SYSTEM (v_percent)
                       WHERE a."EntityAnalysisModelId" = r.model
                         AND a."Id" <= v_hwm
                         AND (a."Json" -> 'payload' ->> r."Key") IS NOT NULL
                       LIMIT v_sample),
                 cmp AS (SELECT s.guid,
                                CASE r.dt
                                    WHEN 2 THEN CASE
                                                    WHEN pg_input_is_valid(s.txt, 'integer')
                                                        THEN k."KeyValueInteger" = s.txt::int
                                                    ELSE k."Id" IS NULL END
                                    WHEN 5 THEN k."KeyValueBoolean" =
                                                CASE WHEN lower(s.txt) = 'true' OR s.txt = '1' THEN 1 ELSE 0 END
                                    WHEN 4 THEN CASE
                                                    WHEN pg_input_is_valid(s.txt, 'timestamptz')
                                                        THEN abs(extract(epoch FROM
                                                                         (k."KeyValueDate" - (s.txt::timestamptz AT TIME ZONE 'UTC')))) <
                                                             0.000002
                                                    ELSE k."Id" IS NULL END
                                    WHEN 3 THEN CASE
                                                    WHEN pg_input_is_valid(s.txt, 'float8')
                                                        THEN abs(k."KeyValueFloat" - s.txt::float8) <=
                                                             1e-9 * greatest(1, abs(s.txt::float8))
                                                    ELSE k."Id" IS NULL END
                                    WHEN 6 THEN CASE
                                                    WHEN pg_input_is_valid(s.txt, 'float8')
                                                        THEN abs(k."KeyValueFloat" - s.txt::float8) <=
                                                             1e-9 * greatest(1, abs(s.txt::float8))
                                                    ELSE k."Id" IS NULL END
                                    WHEN 7 THEN CASE
                                                    WHEN pg_input_is_valid(s.txt, 'float8')
                                                        THEN abs(k."KeyValueFloat" - s.txt::float8) <=
                                                             1e-9 * greatest(1, abs(s.txt::float8))
                                                    ELSE k."Id" IS NULL END
                                    ELSE k."KeyValueString" = s.txt END AS same
                         FROM s
                                  LEFT JOIN public."ArchiveKey" k
                                            ON k."EntityAnalysisModelInstanceEntryGuid" = s.guid AND
                                               k."ProcessingTypeId" = 1 AND k."Key" = r."Key")
            UPDATE public."ArchiveKeyBackfillKey"
            SET "SampleCompared"   = (SELECT count(*) FROM cmp),
                "SampleMismatched" = (SELECT count(*) FROM cmp WHERE same IS DISTINCT FROM true)
            WHERE "Id" = r."Id";

            UPDATE public."ArchiveKeyBackfillKey"
            SET "StateId" = CASE
                                WHEN "SampleMismatched" = 0
                                    AND abs("ArchiveWithKeyCount" - "CastFailed" - "ArchiveKeyCount")
                                         <= greatest(1, "ArchiveWithKeyCount" * v_tolerance)
                                    THEN 3
                                ELSE 4 END
            WHERE "Id" = r."Id";

            COMMIT;
        END LOOP;

    UPDATE public."ArchiveKeyBackfillRun"
    SET "StateId"     = CASE
                            WHEN EXISTS (SELECT 1
                                         FROM public."ArchiveKeyBackfillKey"
                                         WHERE "RunId" = v_run AND "StateId" = 4)
                                THEN 2
                            ELSE greatest("StateId", 3) END,
        "UpdatedDate" = now() AT TIME ZONE 'UTC'
    WHERE "Id" = v_run;

    COMMIT;
END
$p$;

-- @jube:step validate
CALL public."ArchiveKeyBackfill_Validate"();

-- @jube:step retire_old_indexes
DO
$s$
    DECLARE
        v_models text := nullif(current_setting('jube.backfill_model_ids', true), '');
        v_run    bigint;
        r        record;
    BEGIN
        SELECT "Id"
        INTO v_run
        FROM public."ArchiveKeyBackfillRun"
        WHERE "StateId" IN (3, 4)
          AND "ModelIds" IS NOT DISTINCT FROM v_models
        ORDER BY "Id" DESC
        LIMIT 1;

        IF v_run IS NULL THEN
            RAISE NOTICE 'Validation did not pass for every key; the IX_Archive_Journal_* indexes were left as they are.';
            RETURN;
        END IF;

        FOR r IN SELECT c.relname
                 FROM pg_class c
                 WHERE c.relnamespace = 'public'::regnamespace
                   AND c.relkind = 'i'
                   AND c.relname LIKE 'IX\_Archive\_Journal\_%'
            LOOP
                EXECUTE format('COMMENT ON INDEX public.%I IS %L', r.relname,
                               'Retired ' || to_char(now() AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS"Z"') ||
                               ' by MigrateCaseKeyJournalIndexesToArchiveKey.sql: Case Key Journal lookups predicate through ArchiveKey. Safe to DROP INDEX CONCURRENTLY.');
            END LOOP;

        UPDATE public."CaseKeyJournalKeyIndex"
        SET "StateId"     = 4,
            "Error"       = NULL,
            "UpdatedDate" = now() AT TIME ZONE 'UTC'
        WHERE "StateId" IN (1, 2, 3);

        UPDATE public."ArchiveKeyBackfillRun"
        SET "StateId"       = 4,
            "CompletedDate" = now() AT TIME ZONE 'UTC',
            "UpdatedDate"   = now() AT TIME ZONE 'UTC'
        WHERE "Id" = v_run;

        RAISE NOTICE 'ArchiveKey backfill run % retired the Case Key Journal indexes.', v_run;
    END
$s$;

-- @jube:step analyze
ANALYZE public."ArchiveKey";

-- @jube:step drop_procedures
DROP PROCEDURE IF EXISTS public."ArchiveKeyBackfill_Dedupe"();

-- @jube:step drop_procedures_2
DROP PROCEDURE IF EXISTS public."ArchiveKeyBackfill_Run"();

-- @jube:step drop_procedures_3
DROP PROCEDURE IF EXISTS public."ArchiveKeyBackfill_Validate"();

-- @jube:step summary
SELECT k."EntityAnalysisModelId",
       k."Key",
       k."DataTypeId",
       k."Inserted",
       k."CastFailed",
       k."ArchiveWithKeyCount",
       k."ArchiveKeyCount",
       k."SampleCompared",
       k."SampleMismatched",
       k."StateId"
FROM public."ArchiveKeyBackfillKey" k
         JOIN public."ArchiveKeyBackfillRun" r ON r."Id" = k."RunId"
WHERE r."Id" = (SELECT max("Id") FROM public."ArchiveKeyBackfillRun")
ORDER BY 1, 2;

-- @jube:step manual_drop_statements
SELECT format('DROP INDEX CONCURRENTLY IF EXISTS public.%I;', c.relname) AS "ManualStep"
FROM pg_class c
WHERE c.relnamespace = 'public'::regnamespace
  AND c.relkind = 'i'
  AND c.relname LIKE 'IX\_Archive\_Journal\_%'
ORDER BY 1;
