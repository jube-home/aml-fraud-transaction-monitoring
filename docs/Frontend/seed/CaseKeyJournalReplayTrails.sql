DO $seed$
DECLARE
    account_key   text := 'AccountId';
    account_value text := 'Test5';
    source        record;
    latitude_key  text;
    longitude_key text;
    trail         record;
    clone         jsonb;
    base_day      timestamp := date_trunc('day', now() at time zone 'utc');
BEGIN
    SELECT a.* INTO source
    FROM "Archive" a
    WHERE a."Json" -> 'payload' ->> account_key = account_value
      AND NOT (a."Json" -> 'payload' ? 'TrailSeed')
    ORDER BY a."Id" DESC
    LIMIT 1;

    IF source."Id" IS NULL THEN
        RAISE EXCEPTION 'No archived transaction found with payload % = %', account_key, account_value;
    END IF;

    SELECT k INTO latitude_key
    FROM jsonb_object_keys(source."Json" -> 'payload') k
    WHERE k ILIKE '%latitude%'
    ORDER BY length(k)
    LIMIT 1;

    SELECT k INTO longitude_key
    FROM jsonb_object_keys(source."Json" -> 'payload') k
    WHERE k ILIKE '%longitude%'
    ORDER BY length(k)
    LIMIT 1;

    IF latitude_key IS NULL OR longitude_key IS NULL THEN
        RAISE EXCEPTION 'The source transaction has no latitude and longitude payload fields to overwrite';
    END IF;

    IF EXISTS (SELECT 1 FROM "Archive"
               WHERE "EntityAnalysisModelId" = source."EntityAnalysisModelId"
                 AND "Json" -> 'payload' ->> account_key = account_value
                 AND "Json" -> 'payload' ? 'TrailSeed') THEN
        RAISE NOTICE 'Trail transactions already seeded for % = %, nothing to do', account_key, account_value;

        RETURN;
    END IF;

    FOR trail IN
        SELECT * FROM (VALUES
            (1, -6, 9,  'London',        51.5074,  -0.1278),
            (2, -6, 18, 'Heathrow',      51.4700,  -0.4543),
            (3, -5, 12, 'Paris',         48.8566,   2.3522),
            (4, -4, 10, 'Frankfurt',     50.1109,   8.6821),
            (5, -3, 8,  'Dubai',         25.2048,  55.2708),
            (6, -3, 20, 'Mumbai',        19.0760,  72.8777),
            (7, -2, 12, 'Singapore',      1.3521, 103.8198),
            (8, -2, 14, 'New York',      40.7128, -74.0060),
            (9, -1, 9,  'Manhattan',     40.7580, -73.9855),
            (10, -1, 22, 'Miami',        25.7617, -80.1918),
            (11, 0, 1,  'London',        51.5074,  -0.1278)
        ) AS t (step, day_offset, hour, place, latitude, longitude)
        ORDER BY step
    LOOP
        clone := jsonb_set(source."Json", '{payload}', source."Json" -> 'payload'
                       || jsonb_build_object(latitude_key, trail.latitude,
                                             longitude_key, trail.longitude,
                                             'TrailSeed', true,
                                             'TrailPlace', trail.place));

        INSERT INTO "Archive" ("Json", "EntityAnalysisModelInstanceEntryGuid", "EntryKeyValue",
                               "ResponseElevation", "EntityAnalysisModelActivationRuleId",
                               "EntityAnalysisModelId", "ActivationRuleCount", "CreatedDate", "ReferenceDate",
                               "Version", "EntityAnalysisModelsReprocessingRuleInstanceId")
        VALUES (clone, gen_random_uuid(), source."EntryKeyValue" || '-trail-' || trail.step,
                source."ResponseElevation", source."EntityAnalysisModelActivationRuleId",
                source."EntityAnalysisModelId", source."ActivationRuleCount",
                base_day + make_interval(days => trail.day_offset, hours => trail.hour),
                base_day + make_interval(days => trail.day_offset, hours => trail.hour),
                source."Version", source."EntityAnalysisModelsReprocessingRuleInstanceId");
    END LOOP;

    RAISE NOTICE 'Seeded 11 located transactions for % = % using %/%', account_key, account_value,
        latitude_key, longitude_key;
END
$seed$;
