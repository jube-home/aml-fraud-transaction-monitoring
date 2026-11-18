\set ON_ERROR_STOP on
-- Times the main case management and archive access paths against a database seeded by ScaleSeed.sql.
-- Run:  psql -d <database> -f ScaleBenchmark.sql   (psql is needed for the \gset lines that find the busiest account and a sample key)
-- Each statement is explained with ANALYZE and BUFFERS; read the Execution Time line and look for Seq Scan on the large tables.
SELECT "Guid" AS wf FROM "CaseWorkflow" WHERE "Name" = 'Scale: Case Volume' \gset
SELECT "KeyValueString" AS hot FROM "ArchiveKey" WHERE "Key" = 'AccountId' AND "KeyValueString" LIKE 'SCL-ACC-%' GROUP BY 1 ORDER BY count(*) DESC LIMIT 1 \gset
SELECT "KeyValueString" AS acc FROM "ArchiveKey" WHERE "Key" = 'AccountId' AND "KeyValueString" LIKE 'SCL-ACC-%' ORDER BY md5("KeyValueString") LIMIT 1 \gset
SELECT "EntityAnalysisModelInstanceEntryGuid" AS entry, "EntryKeyValue" AS entrykey FROM "Archive" WHERE "EntryKeyValue" LIKE 'SCL-TXN-%' ORDER BY md5("EntryKeyValue") LIMIT 1 \gset
SET statement_timeout = 60000;

\echo === Cases page: Triage list, 100 rows, All cases view (about 25k open)
EXPLAIN (ANALYZE, BUFFERS) with "Matched" as (select distinct on ("Case"."Id") "Case"."Id" as "CaseId", "Case"."CreatedDate", "Case"."RiskLaneId", "Case"."RiskPriority", "Case"."RiskLikelihoodProbability", "Case"."RiskNextDeadlineDate", "Case"."RiskValue", "Case"."Locked", "Case"."LockedUser", "Case"."QaSampleStatusId", "Case"."ClosedStatusId" from "Case" where "Case"."CaseWorkflowGuid" = :'wf' and ("Case"."ClosedStatusId" = 0 or "Case"."ClosedStatusId" = 1)), "Pre" as (select m."CaseId" from "Matched" m join "CaseView" v on v."CaseId" = m."CaseId" and v."User" = 'Administrator' union select "CaseId" from "Matched" where "Locked" = 1 and "LockedUser" = 'Administrator' union select "CaseId" from "Matched" where "QaSampleStatusId" = 2 and "ClosedStatusId" in (0, 1) union (select "CaseId" from "Matched" order by coalesce("RiskLaneId", 4), "RiskPriority" desc nulls last, "CreatedDate", "CaseId" limit 100)), "Rows" as (select c."Id" as "CaseId", c."CaseKey", c."CaseKeyValue", c."CreatedDate", c."Locked", c."LockedUser", c."LockedDate", c."ClosedStatusId", c."ClosedDate", c."Diary", c."DiaryDate", c."DiaryTypeId", (c."QaSampleStatusId" = 2 and c."ClosedStatusId" in (0, 1)) as "QaSample", c."CaseWorkflowGuid", c."CaseWorkflowStatusGuid", c."Rating", s."Name" as "StatusName", s."MaxDwellMinutes", w."Name" as "WorkflowName", l."Sequence" as "RiskSequence", c."RiskLaneId" as "LaneId", c."RiskPriority" as "Priority", c."RiskLikelihoodProbability" as "LikelihoodProbability", l."LikelihoodBandId", l."ExpectedHarm", c."RiskNextDeadlineDate" as "NextDeadlineDate", l."DeadlineBreached", l."ContributionsJson"->0->>'label' as "TopDriver", c."RiskValue" as "Value", (v."CaseId" is not null) as "Viewed", v."LastViewedDate", v."LastViewedRiskSequence", (c."Locked" = 1 and c."LockedUser" = 'Administrator') as "Mine", coalesce(v."CaseId" is not null and exists (select 1 from "CaseStatusTransition" t where t."CaseId" = c."Id" and t."EnteredDate" > v."LastViewedDate" and t."EnterReasonId" = 3), false) as "AttReopened", case when v."CaseId" is not null then (select count(*) from "CaseEvent" e where e."CaseId" = c."Id" and e."Id" > coalesce(v."LastViewedCaseEventId", 0) and e."CaseEventTypeId" in (5, 7, 8, 9, 10, 11, 12, 13, 14) and e."CreatedUser" is distinct from 'Administrator') else 0 end as "EventsSince", case when c."ClosedStatusId" in (0, 1) then (select count(*) from "CaseChecklistItem" k where k."CaseId" = c."Id" and k."CaseWorkflowStatusGuid" = c."CaseWorkflowStatusGuid" and k."StateId" = 1) else 0 end as "ChecklistOpen" from "Pre" m join "Case" c on c."Id" = m."CaseId" join "CaseWorkflowStatus" s on s."Guid" = c."CaseWorkflowStatusGuid" and coalesce(s."Deleted", 0) = 0 join "CaseWorkflow" w on w."Guid" = c."CaseWorkflowGuid" and coalesce(w."Deleted", 0) = 0 left join lateral (select a.* from "CaseRiskAssessment" a where a."CaseId" = c."Id" order by a."Sequence" desc limit 1) l on true left join "CaseView" v on v."CaseId" = c."Id" and v."User" = 'Administrator') select x.*, (select count(*) from "Matched") as "Total" from "Rows" x order by case when "LaneId" in (1, 2) then "LaneId" else coalesce("LaneId", 4) end, "Priority" desc nulls last, "CreatedDate", "CaseId" limit 100 offset 0;

\echo === Cases chip: open count
EXPLAIN (ANALYZE, BUFFERS) select count(*) from "Case" where "CaseWorkflowGuid"=:'wf' and ("Case"."ClosedStatusId" = 0 or "Case"."ClosedStatusId" = 1);

\echo === Cases chip: Mine
EXPLAIN (ANALYZE, BUFFERS) select count(*) from "Case" where "CaseWorkflowGuid"=:'wf' and ("Case"."ClosedStatusId" = 0 or "Case"."ClosedStatusId" = 1) and "Locked"=1 and "LockedUser"='Administrator';

\echo === Cases chip: Free to take
EXPLAIN (ANALYZE, BUFFERS) select count(*) from "Case" where "CaseWorkflowGuid"=:'wf' and ("Case"."ClosedStatusId" = 0 or "Case"."ClosedStatusId" = 1) and ("Locked"=0 or "Locked" is null);

\echo === Cases chip: Urgent (lane 1-3)
EXPLAIN (ANALYZE, BUFFERS) select count(*) from "Case" where "CaseWorkflowGuid"=:'wf' and ("Case"."ClosedStatusId" = 0 or "Case"."ClosedStatusId" = 1) and "RiskLaneId" in (1,2,3);

\echo === Cases chip: Quality review
EXPLAIN (ANALYZE, BUFFERS) select count(*) from "Case" where "CaseWorkflowGuid"=:'wf' and ("Case"."ClosedStatusId" = 0 or "Case"."ClosedStatusId" = 1) and "QaSampleStatusId"=2;

\echo === Take next: first free case in triage order
EXPLAIN (ANALYZE, BUFFERS) select "Id" from "Case" where "CaseWorkflowGuid"=:'wf' and ("Case"."ClosedStatusId" = 0 or "Case"."ClosedStatusId" = 1) and ("Locked"=0 or "Locked" is null) order by coalesce("RiskLaneId",4), "RiskPriority" desc nulls last, "CreatedDate", "Id" limit 6;

\echo === Stuck locks: open and locked, oldest first
EXPLAIN (ANALYZE, BUFFERS) select "Id","LockedUser","LockedDate" from "Case" where "CaseWorkflowGuid"=:'wf' and ("Case"."ClosedStatusId" = 0 or "Case"."ClosedStatusId" = 1) and "Locked"=1 and "LockedUser" is not null order by "LockedDate" limit 200;

\echo === Supervision workload: open cases by holder
EXPLAIN (ANALYZE, BUFFERS) select "LockedUser", count(*), min("LockedDate") from "Case" where "CaseWorkflowGuid"=:'wf' and ("Case"."ClosedStatusId" = 0 or "Case"."ClosedStatusId" = 1) and "Locked"=1 group by 1;

\echo === Open a case by id
EXPLAIN (ANALYZE, BUFFERS) select * from "Case" where "Id"=(select max("Id") from "Case");

\echo === Case by key (open case guard, workflow + key + value)
EXPLAIN (ANALYZE, BUFFERS) select "Id" from "Case" where "CaseWorkflowGuid"=:'wf' and "CaseKey"='AccountId' and "CaseKeyValue"=:'hot' and "ClosedStatusId" in (0,1,2,4);

\echo === Other cases for a hot key (all statuses)
EXPLAIN (ANALYZE, BUFFERS) select "Id","CreatedDate","ClosedStatusId" from "Case" where "CaseWorkflowGuid"=:'wf' and "CaseKey"='AccountId' and "CaseKeyValue"=:'hot' order by "CreatedDate" desc;

\echo === Case events for one case
EXPLAIN (ANALYZE, BUFFERS) select * from "CaseEvent" where "CaseId"=(select "CaseId" from "CaseEvent" group by 1 order by count(*) desc limit 1) order by "Id" desc limit 200;

\echo === Archive: one transaction by entry guid
EXPLAIN (ANALYZE, BUFFERS) select "Json" from "Archive" where "EntityAnalysisModelInstanceEntryGuid"=:'entry';

\echo === Archive: one transaction by entry key value
EXPLAIN (ANALYZE, BUFFERS) select "Id" from "Archive" where "EntityAnalysisModelId"=1 and  "EntryKeyValue" = :'entrykey';

\echo === Archive: last 24 hours by reference date
EXPLAIN (ANALYZE, BUFFERS) select count(*) from "Archive" where "EntityAnalysisModelId"=1 and "ReferenceDate">=(now() at time zone 'utc')-interval '1 day';

\echo === Archive: last 7 days alerts by rule
EXPLAIN (ANALYZE, BUFFERS) select "EntityAnalysisModelActivationRuleId", count(*) from "Archive" where "EntityAnalysisModelId"=1 and "ReferenceDate">=(now() at time zone 'utc')-interval '7 days' and "ActivationRuleCount">0 group by 1;

\echo === Archive: 90 days, transactions per day
EXPLAIN (ANALYZE, BUFFERS) select date_trunc('day',"ReferenceDate"), count(*) from "Archive" where "EntityAnalysisModelId"=1 and "ReferenceDate">=(now() at time zone 'utc')-interval '90 days' group by 1 order by 1;

\echo === Archive: full year, transactions per month
EXPLAIN (ANALYZE, BUFFERS) select date_trunc('month',"ReferenceDate"), count(*) from "Archive" where "ReferenceDate">=(now() at time zone 'utc')-interval '365 days' group by 1 order by 1;

\echo === Key journal: transactions for a hot key via ArchiveKey
EXPLAIN (ANALYZE, BUFFERS) select a."Id" from "ArchiveKey" k join "Archive" a on a."EntityAnalysisModelInstanceEntryGuid"=k."EntityAnalysisModelInstanceEntryGuid" where k."Key"='AccountId' and k."KeyValueString"=:'hot' order by a."ReferenceDate" desc limit 200;

\echo === Key journal: transactions for an ordinary key
EXPLAIN (ANALYZE, BUFFERS) select a."Id" from "ArchiveKey" k join "Archive" a on a."EntityAnalysisModelInstanceEntryGuid"=k."EntityAnalysisModelInstanceEntryGuid" where k."Key"='AccountId' and k."KeyValueString"=:'acc' order by a."ReferenceDate" desc limit 200;

\echo === Archive: jsonb containment on payload country (GIN)
EXPLAIN (ANALYZE, BUFFERS) select count(*) from "Archive" where "Json" @> '{"payload": {"BillingCountry": "EG"}}' and "ReferenceDate">=(now() at time zone 'utc')-interval '30 days';

\echo === Archive: rule league (alerts by rule, 365 days)
EXPLAIN (ANALYZE, BUFFERS) select "Json"->>'prevailingEntityAnalysisModelActivationRuleName', count(*) from "Archive" where "ReferenceDate">=(now() at time zone 'utc')-interval '365 days' and "ActivationRuleCount">0 group by 1;

\echo === Cases: opened per day, 90 days (analytics)
EXPLAIN (ANALYZE, BUFFERS) select date_trunc('day',"CreatedDate"), count(*) from "Case" where "CaseWorkflowGuid"=:'wf' and "CreatedDate">=(now() at time zone 'utc')-interval '90 days' group by 1;

\echo === Cases: closed per day, 90 days
EXPLAIN (ANALYZE, BUFFERS) select date_trunc('day',"ClosedDate"), count(*) from "Case" where "CaseWorkflowGuid"=:'wf' and "ClosedDate">=(now() at time zone 'utc')-interval '90 days' group by 1;
