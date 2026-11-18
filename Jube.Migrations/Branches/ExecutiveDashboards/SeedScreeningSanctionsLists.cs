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

    [Migration(2026111792000011)]
    public class SeedScreeningSanctionsLists : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Screening: Sanctions Lists' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Screening: Sanctions Lists', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
                            RETURNING "Id" INTO v_registry_id;

                            INSERT INTO "VisualisationRegistryRole" ("Guid", "VisualisationRegistryGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_registry_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'From', 1, 0, v_now, 'Administrator', 0, 1, 4, '90', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'To', 1, 0, v_now, 'Administrator', 0, 1, 4, '0', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Listed entries by source', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT coalesce(s."Name", 'Unknown') AS "Source", count(*) AS "Entries" FROM "SanctionEntry" e LEFT JOIN "SanctionEntrySource" s ON s."Id" = e."SanctionEntrySourceId" WHERE (e."Deleted" = 0 OR e."Deleted" IS NULL) GROUP BY 1 ORDER BY 2 DESC$cmd$, 10, 0, 4, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Source', 1, 1, 1, 1),
                                (v_datasource_id, 'Entries', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Entries added and removed by import', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', i."StartDate"), 'YYYY-MM-DD') AS "Import", sum(i."InsertedCount") AS "Inserted", sum(i."RemovedCount") AS "Removed", sum(i."RejectedCount") AS "Rejected"
                        FROM "SanctionEntryImport" i CROSS JOIN w WHERE i."StartDate" >= w.f AND i."StartDate" < w.t GROUP BY date_trunc('day', i."StartDate") ORDER BY date_trunc('day', i."StartDate")$cmd$, 20, 0, 8, 3, v_datasource_guid, 16, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Import', 1, 1, 1, 1),
                                (v_datasource_id, 'Inserted', 2, 2, 2, 1),
                                (v_datasource_id, 'Removed', 2, 3, 2, 2),
                                (v_datasource_id, 'Rejected', 2, 4, 27, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Import runs', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT s."Name" AS "Source", i."StartDate" AS "Started", round(extract(epoch FROM (i."EndDate" - i."StartDate")) / 60)::integer AS "Minutes", i."TotalRows" AS "Rows read", i."InsertedCount" AS "Inserted", i."RevivedCount" AS "Revived",
                               i."UnchangedCount" AS "Unchanged", i."RemovedCount" AS "Removed", i."RejectedCount" AS "Rejected", CASE WHEN i."Successful" = 1 THEN 'Succeeded' ELSE 'Failed' END AS "Outcome", i."ErrorMessage" AS "Error"
                        FROM "SanctionEntryImport" i CROSS JOIN w LEFT JOIN "SanctionEntrySource" s ON s."Id" = i."SanctionEntrySourceId" WHERE i."StartDate" >= w.f AND i."StartDate" < w.t ORDER BY i."StartDate" DESC LIMIT 500$cmd$, 30, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Source', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Started', 4, 2, NULL, NULL),
                                (v_datasource_id, 'Minutes', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Rows read', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Inserted', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Revived', 2, 6, NULL, NULL),
                                (v_datasource_id, 'Unchanged', 2, 7, NULL, NULL),
                                (v_datasource_id, 'Removed', 2, 8, NULL, NULL),
                                (v_datasource_id, 'Rejected', 2, 9, NULL, NULL),
                                (v_datasource_id, 'Outcome', 1, 10, NULL, NULL),
                                (v_datasource_id, 'Error', 1, 11, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rejected rows by reason', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT CASE r."ReasonId" WHEN 1 THEN 'Too few fields' WHEN 2 THEN 'No reference column' WHEN 3 THEN 'Could not be parsed' ELSE 'Other' END AS "Reason", count(*) AS "Rows" FROM "SanctionEntryRejection" r GROUP BY 1 ORDER BY 2 DESC$cmd$, 40, 0, 4, 3, v_datasource_guid, 12, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Reason', 1, 1, 1, 1),
                                (v_datasource_id, 'Rows', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rejected rows', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT r."CreatedDate" AS "When", s."Name" AS "Source", r."RowNumber" AS "Row", CASE r."ReasonId" WHEN 1 THEN 'Too few fields' WHEN 2 THEN 'No reference column' WHEN 3 THEN 'Could not be parsed' ELSE 'Other' END AS "Reason", left(r."RawData", 80) AS "Start of the row"
                        FROM "SanctionEntryRejection" r LEFT JOIN "SanctionEntrySource" s ON s."Id" = r."SanctionEntrySourceId" ORDER BY r."CreatedDate" DESC LIMIT 500$cmd$, 50, 1, 8, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'When', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Source', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Row', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Reason', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Start of the row', 1, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Stop tokens', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT t."Token" AS "Token", t."CategoryId" AS "Category" FROM "SanctionStopToken" t ORDER BY t."CategoryId", t."Token" LIMIT 500$cmd$, 60, 1, 4, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Token', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Category', 2, 2, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Hits by match distance', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT 'Distance ' || (j.value)::text AS "Distance", count(*) AS "Hits" FROM "Archive" a CROSS JOIN w CROSS JOIN LATERAL jsonb_each(COALESCE(a."Json"->'sanction', '{}'::jsonb)) AS j(key, value)
                        WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t GROUP BY 1 ORDER BY 1$cmd$, 70, 0, 4, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Distance', 1, 1, 1, 1),
                                (v_datasource_id, 'Hits', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Hits by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT to_char(date_trunc('month', a."CreatedDate"), 'YYYY-MM') AS "Month", count(*) FILTER (WHERE a."Json"->'sanction' IS NOT NULL AND a."Json"->'sanction' <> '{}'::jsonb) AS "Hits"
                        FROM "Archive" a WHERE a."CreatedDate" >= date_trunc('month', (now() at time zone 'utc') - interval '11 months') GROUP BY date_trunc('month', a."CreatedDate") ORDER BY date_trunc('month', a."CreatedDate")$cmd$, 80, 0, 8, 3, v_datasource_guid, 7, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Hits', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Screening configuration', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT s."Name" AS "Screening", s."MultipartStringDataName" AS "Field screened", s."Distance" AS "Distance", s."MatchAlgorithmId" AS "Algorithm", s."MinimumSimilarity" AS "Minimum similarity", s."CacheValue" AS "Cache", s."CacheInterval" AS "Cache interval",
                               CASE WHEN s."Active" = 1 THEN 'Yes' ELSE 'No' END AS "Active", s."Version" AS "Version" FROM "EntityAnalysisModelSanction" s WHERE (s."Deleted" = 0 OR s."Deleted" IS NULL) ORDER BY s."Name"$cmd$, 90, 1, 12, 2, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Screening', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Field screened', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Distance', 5, 3, NULL, NULL),
                                (v_datasource_id, 'Algorithm', 5, 4, NULL, NULL),
                                (v_datasource_id, 'Minimum similarity', 3, 5, NULL, NULL),
                                (v_datasource_id, 'Cache', 2, 6, NULL, NULL),
                                (v_datasource_id, 'Cache interval', 1, 7, NULL, NULL),
                                (v_datasource_id, 'Active', 1, 8, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 9, NULL, NULL);
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
