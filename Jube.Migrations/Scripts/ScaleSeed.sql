-- ScaleSeed.sql
--
-- Seeds a large archive and a large case table so that the indexing strategy and the main queries can be
-- measured at volume. It is opt-in and never runs as a migration. Run it only against a demonstration or test
-- database that has the example model, tenant 1 and role 1; never against a database that holds real data.
--
--     PGOPTIONS="-c jube.scale_archive=1000000 -c jube.scale_cases=150000" psql "$CONNECTION" -v ON_ERROR_STOP=1 -f ScaleSeed.sql
--
-- Settings, both optional, set in the same session before running:
--   jube.scale_archive   archived transactions to create over the last 365 days (1000000)
--   jube.scale_cases     cases to open, each attached to an alerted transaction (150000)
--
-- It creates its own workflow, Scale: Case Volume, granted to the Administrator role, and loads in chunks of
-- 100000 rows with a commit after each, so progress is printed and a failure keeps what was loaded. A second run
-- stops at once because the scale transactions are already present. The tables are analysed at the end.
-- Every top-level statement is separated by a "-- @jube:step" line so that a runner other than psql can send them
-- one at a time: the DO block commits inside.

-- @jube:step seed
DO $scale$
DECLARE
    v_archive integer := coalesce(nullif(current_setting('jube.scale_archive', true), ''), '1000000')::integer;
    v_cases integer := coalesce(nullif(current_setting('jube.scale_cases', true), ''), '150000')::integer;
    v_chunk integer := 100000;
    v_model integer;
    v_model_guid uuid := '90c425fd-101a-420b-91d1-cb7a24a969cc';
    v_tenant integer;
    v_role uuid;
    v_wf_id integer;
    v_wf uuid;
    v_queue uuid;
    v_done uuid;
    v_step integer := greatest(1, v_archive / greatest(v_cases, 1));
    v_now timestamp := date_trunc('minute', now() at time zone 'utc');
    v_lo integer;
    v_hi integer;
BEGIN
    SELECT m."Id" INTO v_model FROM "EntityAnalysisModel" m WHERE m."Guid" = v_model_guid AND coalesce(m."Deleted", 0) = 0;
    SELECT t."Id" INTO v_tenant FROM "TenantRegistry" t WHERE t."Id" = 1 AND coalesce(t."Deleted", 0) = 0;
    SELECT r."Guid" INTO v_role FROM "RoleRegistry" r WHERE r."Id" = 1 AND coalesce(r."Deleted", 0) = 0;

    IF v_model IS NULL OR v_tenant IS NULL OR v_role IS NULL THEN
        RAISE EXCEPTION 'The example model, tenant 1 and role 1 must exist';
    END IF;

    IF EXISTS (SELECT 1 FROM "Archive" WHERE "EntryKeyValue" LIKE 'SCL-TXN-%' LIMIT 1) THEN
        RAISE NOTICE 'Scale data is already present; nothing to do';
        RETURN;
    END IF;

    SELECT "Id", "Guid" INTO v_wf_id, v_wf FROM "CaseWorkflow" WHERE "Name" = 'Scale: Case Volume' AND coalesce("Deleted", 0) = 0;

    IF v_wf IS NULL THEN
        v_wf := gen_random_uuid();
        INSERT INTO "CaseWorkflow" ("Name", "TenantRegistryId", "Active", "Locked", "EnableVisualisation", "CreatedDate", "CreatedUser", "Deleted", "EntityAnalysisModelId", "Version", "Guid",
            "FirstViewTargetMinutes", "OwnershipTargetMinutes", "ResolutionTargetMinutes", "DailyGoal", "TypologySeverityId")
        VALUES ('Scale: Case Volume', v_tenant, 1, 0, 0, v_now, 'Administrator', 0, v_model, 1, v_wf, 240, 480, 2880, 25, 3)
        RETURNING "Id" INTO v_wf_id;
        INSERT INTO "CaseWorkflowRole" ("Guid", "CaseWorkflowGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
        VALUES (gen_random_uuid(), v_wf, v_role, v_now, 'Administrator', 0, 1);
        v_queue := gen_random_uuid();
        v_done := gen_random_uuid();
        INSERT INTO "CaseWorkflowStatus" ("CaseWorkflowId", "Priority", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Guid", "OutcomeId", "MaxDwellMinutes", "EscalationReleaseLock")
        VALUES (v_wf_id, 2, 'Queue', 1, 0, v_now, 'Administrator', 0, 1, v_queue, 0, 2880, 0), (v_wf_id, 1, 'Resolved', 1, 0, v_now, 'Administrator', 0, 1, v_done, 2, NULL, 0);
        INSERT INTO "CaseWorkflowStatusRole" ("Guid", "CaseWorkflowStatusGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
        VALUES (gen_random_uuid(), v_queue, v_role, v_now, 'Administrator', 0, 1), (gen_random_uuid(), v_done, v_role, v_now, 'Administrator', 0, 1);
    ELSE
        SELECT "Guid" INTO v_queue FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_wf_id AND "Name" = 'Queue';
        SELECT "Guid" INTO v_done FROM "CaseWorkflowStatus" WHERE "CaseWorkflowId" = v_wf_id AND "Name" = 'Resolved';
    END IF;

    COMMIT;

    CREATE TEMP TABLE scl_centroid (country text, weight integer, lat double precision, lon double precision);
    INSERT INTO scl_centroid VALUES ('GB', 30, 51.5074, -0.1278), ('DE', 12, 52.52, 13.405), ('FR', 9, 48.8566, 2.3522), ('NL', 7, 52.3676, 4.9041), ('ES', 6, 40.4168, -3.7038),
        ('IT', 5, 41.9028, 12.4964), ('PL', 4, 52.2297, 21.0122), ('EG', 3, 30.0444, 31.2357), ('TR', 3, 41.0082, 28.9784), ('AE', 2, 25.2048, 55.2708);
    CREATE TEMP TABLE scl_rule AS
    SELECT row_number() OVER (ORDER BY r."Id") AS ix, r."Id" AS id, r."Name" AS name, r."Guid" AS guid
    FROM "EntityAnalysisModelActivationRule" r WHERE r."EntityAnalysisModelId" = v_model AND coalesce(r."Deleted", 0) = 0 AND r."EnableCaseWorkflow" = 1;

    FOR v_lo IN SELECT g FROM generate_series(1, v_archive, v_chunk) g LOOP
        v_hi := least(v_lo + v_chunk - 1, v_archive);

        CREATE TEMP TABLE scl_a ON COMMIT DROP AS
        SELECT n, gen_random_uuid() AS guid, abs(hashtext('h' || n)) AS h,
               v_now - ((abs(hashtext('d' || n)) % (365 * 86400)) * interval '1 second') AS at,
               floor(150000 * power((abs(hashtext('u' || n)) % 100000) / 100000.0, 2.2))::integer AS acct,
               round((5 + power((abs(hashtext('m' || n)) % 100000) / 100000.0, 4) * 20000)::numeric, 2) AS amount,
               (n % v_step = 0) AS alerted
        FROM generate_series(v_lo, v_hi) n;

        CREATE TEMP TABLE scl_b ON COMMIT DROP AS
        SELECT a.*, c.country, c.lat, c.lon, r.id AS rule_id, r.name AS rule_name
        FROM scl_a a
        JOIN LATERAL (SELECT * FROM scl_centroid ORDER BY (abs(hashtext('c' || a.n)) % 100) * 1.0 / weight LIMIT 1) c ON true
        LEFT JOIN scl_rule r ON a.alerted AND r.ix = 1 + (a.h % (SELECT count(*) FROM scl_rule));

        INSERT INTO "Archive" ("Json", "EntityAnalysisModelInstanceEntryGuid", "EntryKeyValue", "ResponseElevation", "EntityAnalysisModelActivationRuleId", "EntityAnalysisModelId", "ActivationRuleCount", "CreatedDate", "ReferenceDate", "Version")
        SELECT jsonb_build_object(
                   'payload', jsonb_build_object('TxnId', 'SCL-TXN-' || b.n, 'TxnDateTime', to_char(b.at, 'YYYY-MM-DD"T"HH24:MI:SS'), 'AccountId', 'SCL-ACC-' || b.acct, 'CurrencyAmount', b.amount, 'AmountEUR', b.amount,
                       'Currency', 'EUR', 'BillingCountry', b.country, 'ChannelId', (ARRAY['Card', 'ATM', 'Transfer'])[1 + (b.h % 3)], 'ResponseCode', '0',
                       'AccountLatitude', round((b.lat + ((b.h % 100) - 50) / 100.0)::numeric, 4), 'AccountLongitude', round((b.lon + (((b.h / 7) % 100) - 50) / 100.0)::numeric, 4)),
                   'tag', '[]'::jsonb,
                   'activation', CASE WHEN b.rule_name IS NULL THEN '{}'::jsonb ELSE jsonb_build_object(b.rule_name, jsonb_build_object('visible', true, 'forced', false)) END,
                   'shadowActivation', '{}'::jsonb, 'abstraction', '{}'::jsonb, 'ttlCounter', '{}'::jsonb,
                   'responseElevation', jsonb_build_object('value', round((CASE WHEN b.alerted THEN 1 + (b.h % 400) / 100.0 ELSE (b.h % 100) / 400.0 END)::numeric, 3), 'name', 'Level 1'),
                   'prevailingEntityAnalysisModelActivationRuleName', b.rule_name, 'entityAnalysisModelActivationRuleCount', CASE WHEN b.alerted THEN 1 ELSE 0 END,
                   'entityAnalysisModelGuid', v_model_guid::text, 'entityAnalysisModelName', 'Detailed Account Financial Transactions',
                   'entityAnalysisModelInstanceEntryGuid', b.guid::text, 'createdDate', to_char(b.at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'), 'referenceDate', to_char(b.at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'), 'createCase', b.alerted),
               b.guid, 'SCL-TXN-' || b.n, CASE WHEN b.alerted THEN 1 + (b.h % 400) / 100.0 ELSE (b.h % 100) / 400.0 END, b.rule_id, v_model, CASE WHEN b.alerted THEN 1 ELSE 0 END, b.at, b.at, 1
        FROM scl_b b;

        INSERT INTO "ArchiveKey" ("ProcessingTypeId", "Key", "KeyValueString", "KeyValueFloat", "EntityAnalysisModelInstanceEntryGuid", "Version")
        SELECT 1, k.key, k.sv, k.fv, b.guid, 1
        FROM scl_b b CROSS JOIN LATERAL (VALUES ('AccountId', 'SCL-ACC-' || b.acct, NULL::double precision), ('AmountEUR', NULL, b.amount::double precision), ('BillingCountry', b.country, NULL)) AS k(key, sv, fv);

        DROP TABLE scl_a, scl_b;
        COMMIT;
        RAISE NOTICE 'Archive % of %', v_hi, v_archive;
    END LOOP;

    CREATE TEMP TABLE scl_src AS
    SELECT row_number() OVER (ORDER BY a."Id") AS cn, a."EntityAnalysisModelInstanceEntryGuid" AS guid, a."CreatedDate" AS created, a."Json"->'payload'->>'AccountId' AS acct,
           (a."Json"->'payload'->>'AmountEUR')::double precision AS amount, a."Json"->>'prevailingEntityAnalysisModelActivationRuleName' AS rule_name, a."Json" AS json,
           abs(hashtext('s' || a."Id")) AS h
    FROM "Archive" a WHERE a."EntryKeyValue" LIKE 'SCL-TXN-%' AND a."ActivationRuleCount" > 0;

    FOR v_lo IN SELECT g FROM generate_series(1, (SELECT count(*) FROM scl_src)::integer, v_chunk) g LOOP
        v_hi := v_lo + v_chunk - 1;

        CREATE TEMP TABLE scl_c ON COMMIT DROP AS
        SELECT s.*, CASE WHEN s.h % 100 < 15 THEN 'open' ELSE 'closed' END AS kind,
               (ARRAY['Amara Okafor', 'Tomasz Kowalski', 'Priya Raman', 'Jonas Berg', 'Mei Lin', 'Carlos Mendes', 'Administrator'])[1 + (s.h % 7)] AS analyst,
               CASE WHEN s.h % 100 < 4 THEN 1 WHEN s.h % 100 < 8 THEN 2 WHEN s.h % 100 < 14 THEN 3 ELSE 4 END AS lane
        FROM scl_src s WHERE s.cn BETWEEN v_lo AND v_hi;

        INSERT INTO "Case" ("EntityAnalysisModelInstanceEntryGuid", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "CreatedDate", "Locked", "LockedUser", "LockedDate", "ClosedStatusId", "ClosedDate", "ClosedUser",
            "CaseKey", "CaseKeyValue", "Diary", "DiaryTypeId", "Rating", "Json", "LastClosedStatus", "QaSampleStatusId", "RiskLaneId", "RiskPriority", "RiskLikelihoodProbability", "RiskNextDeadlineDate", "RiskValue",
            "RiskSequence", "RiskCheckedDate", "ClosedReasonId", "ClosedReasonNote", "LegalHold")
        SELECT c.guid, v_wf, CASE WHEN c.kind = 'closed' THEN v_done ELSE v_queue END, c.created + interval '2 minutes',
               CASE WHEN c.kind = 'open' AND c.h % 4 = 0 THEN 1 ELSE 0 END, CASE WHEN c.kind = 'open' AND c.h % 4 = 0 THEN c.analyst END,
               CASE WHEN c.kind = 'open' AND c.h % 4 = 0 THEN least(v_now, c.created + interval '1 hour') END,
               CASE WHEN c.kind = 'closed' THEN 3 ELSE 0 END, CASE WHEN c.kind = 'closed' THEN least(v_now, c.created + (10 + c.h % 3000) * interval '1 minute') END, CASE WHEN c.kind = 'closed' THEN c.analyst END,
               'AccountId', c.acct, 0, 1, 0, c.json, CASE WHEN c.kind = 'closed' THEN 3 ELSE 0 END, CASE WHEN c.h % 100 < 5 THEN 2 ELSE 1 END, c.lane, 5 + (c.h % 90), (c.h % 1000) / 1000.0,
               c.created + interval '2 days', c.amount, 1, v_now + interval '10 years', CASE WHEN c.kind = 'closed' THEN 1 + (c.h % 8) END, CASE WHEN c.kind = 'closed' THEN 'Closed in the scale seed.' END, 0
        FROM scl_c c;

        CREATE TEMP TABLE scl_k ON COMMIT DROP AS
        SELECT c.*, k."Id" AS case_id FROM scl_c c JOIN "Case" k ON k."EntityAnalysisModelInstanceEntryGuid" = c.guid AND k."CaseWorkflowGuid" = v_wf;

        INSERT INTO "CaseEvent" ("CaseEventTypeId", "Before", "After", "CreatedUser", "CreatedDate", "CaseKey", "CaseKeyValue", "CaseId")
        SELECT e.t, NULL, NULL, c.analyst, e.at, 'AccountId', c.acct, c.case_id
        FROM scl_k c CROSS JOIN LATERAL (VALUES (4, c.created + interval '10 minutes', c.h % 100 < 95), (6, c.created + interval '15 minutes', c.h % 3 = 0), (5, c.created + (10 + c.h % 3000) * interval '1 minute', c.kind = 'closed')) AS e(t, at, applies)
        WHERE e.applies AND e.at <= v_now;

        INSERT INTO "CaseStatusTransition" ("CaseId", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "PreviousCaseWorkflowStatusGuid", "EnterReasonId", "EnteredDate", "EnteredUser", "ExitReasonId", "ExitedDate", "ExitedUser", "DurationSeconds")
        SELECT c.case_id, v_wf, CASE WHEN c.kind = 'closed' THEN v_done ELSE v_queue END, NULL, 1, c.created + interval '2 minutes', 'Automation', CASE WHEN c.kind = 'closed' THEN 2 END,
               CASE WHEN c.kind = 'closed' THEN least(v_now, c.created + (10 + c.h % 3000) * interval '1 minute') END, CASE WHEN c.kind = 'closed' THEN c.analyst END,
               CASE WHEN c.kind = 'closed' THEN extract(epoch FROM (least(v_now, c.created + (10 + c.h % 3000) * interval '1 minute') - (c.created + interval '2 minutes')))::integer END
        FROM scl_k c;

        INSERT INTO "CaseRiskAssessment" ("CaseId", "Sequence", "CreatedDate", "TriggerId", "MethodId", "MethodVersion", "BaseRate", "LikelihoodProbability", "LikelihoodBandId", "EvidenceCount", "ContributionsJson", "ImpactId", "ImpactComponentsJson", "LaneId", "LaneReason", "NextDeadlineDate", "DeadlineBreached", "ExpectedHarm", "Priority")
        SELECT c.case_id, 1, c.created + interval '5 minutes', 1, 1, 1, 0.1, (c.h % 1000) / 1000.0, 1 + (c.h % 4), 3, '[{"label": "Amount against the account history", "family": "Amount", "weight": 0.4}]'::jsonb, 2, '{}'::jsonb,
               c.lane, 'Scale seed', c.created + interval '2 days', 0, 1.2, 5 + (c.h % 90)
        FROM scl_k c;

        INSERT INTO "CaseNote" ("Note", "ActionId", "PriorityId", "CreatedDate", "CaseKey", "CreatedUser", "CaseKeyValue", "CaseId", "SourceId", "KindId", "IsFinding", "Version")
        SELECT 'Reviewed and closed after enquiries.', NULL, 3, c.created + interval '20 minutes', 'AccountId', c.analyst, c.acct, c.case_id, 1, 1, false, 1 FROM scl_k c WHERE c.kind = 'closed' AND c.h % 100 < 60;

        DROP TABLE scl_c, scl_k;
        COMMIT;
        RAISE NOTICE 'Cases up to % of %', v_hi, (SELECT count(*) FROM scl_src);
    END LOOP;
END
$scale$;

-- @jube:step analyse
ANALYZE "Archive";
ANALYZE "ArchiveKey";
ANALYZE "Case";
ANALYZE "CaseEvent";
ANALYZE "CaseStatusTransition";
ANALYZE "CaseRiskAssessment";
ANALYZE "CaseNote";
