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

    [Migration(2026111792000015)]
    public class SeedPlatformHealthAndPerformance : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $seed$
                        DECLARE
                            v_tenant integer;
                            v_role uuid;
                            v_registry_id integer;
                            v_registry_guid uuid := gen_random_uuid();
                            v_datasource_id integer;
                            v_datasource_guid uuid;
                            v_parameter_guid uuid;
                            v_now timestamp := now() at time zone 'utc';
                        BEGIN
                            SELECT "Id" INTO v_tenant FROM "TenantRegistry" WHERE "Id" = 1 AND coalesce("Deleted", 0) = 0;
                            SELECT "Guid" INTO v_role FROM "RoleRegistry" WHERE "Id" = 1 AND coalesce("Deleted", 0) = 0;

                            IF v_tenant IS NULL OR v_role IS NULL THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Platform: Health and Performance' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Platform: Health and Performance', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
                            RETURNING "Id" INTO v_registry_id;

                            INSERT INTO "VisualisationRegistryRole" ("Guid", "VisualisationRegistryGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_registry_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'From', 1, 0, v_now, 'Administrator', 0, 1, 4, '7', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'To', 1, 0, v_now, 'Administrator', 0, 1, 4, '0', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Invocations by hour', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('hour', c."CreatedDate"), 'MM-DD HH24:00') AS "Hour", sum(c."ModelInvoke") AS "Invocations", sum(c."ResponseElevation") AS "Alerts"
                        FROM "EntityAnalysisModelProcessingCounter" c CROSS JOIN w WHERE c."CreatedDate" >= w.f AND c."CreatedDate" < w.t GROUP BY date_trunc('hour', c."CreatedDate") ORDER BY date_trunc('hour', c."CreatedDate")$cmd$, 10, 0, 6, 3, v_datasource_guid, 7, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Hour', 1, 1, 1, 1),
                                (v_datasource_id, 'Invocations', 2, 2, 2, 1),
                                (v_datasource_id, 'Alerts', 2, 3, 2, 2);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Response time', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', c."CreatedDate"), 'YYYY-MM-DD') AS "Day", round((sum(c."ModelTotalResponseTime")::numeric / NULLIF(sum(c."ModelInvoke"), 0)), 0) AS "Mean microseconds", max(c."MaxResponseTimeMicroseconds") AS "Slowest microseconds"
                        FROM "EntityAnalysisModelProcessingCounter" c CROSS JOIN w WHERE c."CreatedDate" >= w.f AND c."CreatedDate" < w.t GROUP BY date_trunc('day', c."CreatedDate") ORDER BY date_trunc('day', c."CreatedDate")$cmd$, 20, 0, 6, 3, v_datasource_guid, 16, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Mean microseconds', 3, 2, 2, 1),
                                (v_datasource_id, 'Slowest microseconds', 2, 3, 27, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Time spent by stage', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', p."CreatedDate"), 'YYYY-MM-DD') AS "Day", CASE p."StageId" WHEN 1 THEN 'Parse' WHEN 2 THEN 'Extraction' WHEN 3 THEN 'Gateway' WHEN 4 THEN 'Abstraction' WHEN 5 THEN 'Calculation' WHEN 6 THEN 'Activation' WHEN 7 THEN 'Response' ELSE 'Stage ' || p."StageId" END AS "Stage", round(sum(p."TotalMicroseconds") / 1000000.0, 2) AS "Seconds"
                        FROM "EntityAnalysisModelStagePerformanceCounter" p CROSS JOIN w WHERE p."CreatedDate" >= w.f AND p."CreatedDate" < w.t GROUP BY date_trunc('day', p."CreatedDate"), 2 ORDER BY date_trunc('day', p."CreatedDate"), 2$cmd$, 30, 0, 6, 3, v_datasource_guid, 4, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Stage', 1, 2, 3, 1),
                                (v_datasource_id, 'Seconds', 3, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Allocated memory by stage', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT CASE p."StageId" WHEN 1 THEN 'Parse' WHEN 2 THEN 'Extraction' WHEN 3 THEN 'Gateway' WHEN 4 THEN 'Abstraction' WHEN 5 THEN 'Calculation' WHEN 6 THEN 'Activation' WHEN 7 THEN 'Response' ELSE 'Stage ' || p."StageId" END AS "Stage", round(sum(p."TotalAllocatedBytes") / 1048576.0, 1) AS "Megabytes"
                        FROM "EntityAnalysisModelResponseTimePipelineCounter" p CROSS JOIN w WHERE p."CreatedDate" >= w.f AND p."CreatedDate" < w.t GROUP BY 1 ORDER BY 2 DESC$cmd$, 40, 0, 6, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Stage', 1, 1, 1, 1),
                                (v_datasource_id, 'Megabytes', 3, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Queue balances', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('hour', q."CreatedDate"), 'MM-DD HH24:00') AS "Hour", max(q."Archive") AS "Archive queue", max(q."ActivationWatcher") AS "Activation watcher queue"
                        FROM "EntityAnalysisModelAsynchronousQueueBalance" q CROSS JOIN w WHERE q."CreatedDate" >= w.f AND q."CreatedDate" < w.t GROUP BY date_trunc('hour', q."CreatedDate") ORDER BY date_trunc('hour', q."CreatedDate")$cmd$, 50, 0, 6, 3, v_datasource_guid, 7, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Hour', 1, 1, 1, 1),
                                (v_datasource_id, 'Archive queue', 2, 2, 2, 1),
                                (v_datasource_id, 'Activation watcher queue', 2, 3, 2, 2);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Implicit asynchronous timeouts', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', c."CreatedDate"), 'YYYY-MM-DD') AS "Day", sum(c."ImplicitAsyncTimeout") AS "Timeouts", sum(c."ImplicitAsyncInvoke") AS "Invocations that went asynchronous"
                        FROM "EntityAnalysisModelProcessingCounter" c CROSS JOIN w WHERE c."CreatedDate" >= w.f AND c."CreatedDate" < w.t GROUP BY date_trunc('day', c."CreatedDate") ORDER BY date_trunc('day', c."CreatedDate")$cmd$, 60, 0, 6, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Timeouts', 2, 2, 2, 1),
                                (v_datasource_id, 'Invocations that went asynchronous', 2, 3, 2, 2);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Archiver stage timings', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', p."CreatedDate"), 'YYYY-MM-DD') AS "Day", CASE p."StageId" WHEN 1 THEN 'Build JSON' WHEN 2 THEN 'Case dispatch' WHEN 3 THEN 'Database write' WHEN 4 THEN 'Bulk copy' ELSE 'Stage ' || p."StageId" END AS "Archiver stage",
                               round(sum(p."TotalMicroseconds") / 1000000.0, 2) AS "Seconds" FROM "ArchiverStagePerformanceCounter" p CROSS JOIN w WHERE p."CreatedDate" >= w.f AND p."CreatedDate" < w.t GROUP BY date_trunc('day', p."CreatedDate"), 2 ORDER BY date_trunc('day', p."CreatedDate"), 2$cmd$, 70, 0, 6, 3, v_datasource_guid, 4, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Archiver stage', 1, 2, 3, 1),
                                (v_datasource_id, 'Seconds', 3, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Archiver and case creation warnings', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT x.at AS "When", x.source AS "Source", x.detail AS "Detail", x.micros AS "Microseconds" FROM (
                            SELECT a."OccurredDate" AS at, 'Archiver' AS source, a."EntityAnalysisModelName" AS detail, a."DurationMicroseconds" AS micros FROM "ArchiverWarning" a
                            UNION ALL SELECT c."OccurredDate", 'Case creation', coalesce(c."Destination", c."CaseKeyValue"), c."DurationMicroseconds" FROM "CaseCreationWarning" c
                        ) x CROSS JOIN w WHERE x.at >= w.f AND x.at < w.t ORDER BY x.at DESC LIMIT 500$cmd$, 80, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'When', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Source', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Detail', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Microseconds', 2, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Nodes and synchronisation', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT n."Instance" AS "Node", n."HeartbeatDate" AS "Last heartbeat", n."SynchronisedDate" AS "Last synchronised", round(extract(epoch FROM ((now() at time zone 'utc') - n."HeartbeatDate")) / 60)::integer AS "Minutes since heartbeat"
                        FROM "EntityAnalysisModelSynchronisationNodeStatusEntry" n ORDER BY n."HeartbeatDate" DESC LIMIT 200$cmd$, 90, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Node', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Last heartbeat', 4, 2, NULL, NULL),
                                (v_datasource_id, 'Last synchronised', 4, 3, NULL, NULL),
                                (v_datasource_id, 'Minutes since heartbeat', 2, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Synchronisation schedule', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT s."CreatedDate" AS "Requested", s."CreatedUser" AS "By", s."ScheduleDate" AS "Scheduled for" FROM "EntityAnalysisModelSynchronisationSchedule" s CROSS JOIN w WHERE s."CreatedDate" >= w.f AND s."CreatedDate" < w.t ORDER BY s."CreatedDate" DESC LIMIT 200$cmd$, 100, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Requested', 4, 1, NULL, NULL),
                                (v_datasource_id, 'By', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Scheduled for', 4, 3, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Reprocessing runs', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT r."Name" AS "Reprocessing rule", i."CreatedDate" AS "Started", CASE i."StatusId" WHEN 0 THEN 'Not allocated' WHEN 1 THEN 'Allocated' WHEN 2 THEN 'Counting' WHEN 3 THEN 'Processing' WHEN 4 THEN 'Completed' ELSE 'Failed' END AS "Status",
                               i."AvailableCount" AS "Available", i."SampledCount" AS "Sampled", i."MatchedCount" AS "Matched", i."ProcessedCount" AS "Processed", i."ErrorCount" AS "Errors"
                        FROM "EntityAnalysisModelReprocessingRuleInstance" i JOIN "EntityAnalysisModelReprocessingRule" r ON r."Id" = i."EntityAnalysisModelReprocessingRuleId" WHERE (i."Deleted" = 0 OR i."Deleted" IS NULL) ORDER BY i."CreatedDate" DESC LIMIT 200$cmd$, 110, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Reprocessing rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Started', 4, 2, NULL, NULL),
                                (v_datasource_id, 'Status', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Available', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Sampled', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Matched', 2, 6, NULL, NULL),
                                (v_datasource_id, 'Processed', 2, 7, NULL, NULL),
                                (v_datasource_id, 'Errors', 2, 8, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Exports, imports, snapshots and comparisons', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT x.at AS "When", x.kind AS "Kind", x.by AS "By", CASE WHEN x.failed = 1 THEN 'Failed' WHEN x.done IS NULL THEN 'Running' ELSE 'Completed' END AS "Outcome" FROM (
                            SELECT e."CreatedDate" AS at, 'Export' AS kind, e."CreatedUser" AS by, e."InError" AS failed, e."CompletedDate" AS done FROM "Export" e
                            UNION ALL SELECT i."CreatedDate", 'Import', i."CreatedUser", i."InError", i."CompletedDate" FROM "Import" i
                            UNION ALL SELECT s."CreatedDate", 'Snapshot', s."CreatedUser", s."InError", s."CompletedDate" FROM "PreservationSnapshot" s WHERE (s."Deleted" = 0 OR s."Deleted" IS NULL)
                            UNION ALL SELECT d."CreatedDate", 'Comparison', d."CreatedUser", d."InError", d."CompletedDate" FROM "PreservationDiff" d
                        ) x ORDER BY x.at DESC LIMIT 500$cmd$, 120, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'When', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Kind', 1, 2, NULL, NULL),
                                (v_datasource_id, 'By', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Outcome', 1, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);
                        END
                        $seed$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
