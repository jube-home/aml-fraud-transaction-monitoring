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

    [Migration(2026111792000005)]
    public class SeedMonitoringTransactionVolumes : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Monitoring: Transaction Volumes' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Monitoring: Transaction Volumes', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
                            RETURNING "Id" INTO v_registry_id;

                            INSERT INTO "VisualisationRegistryRole" ("Guid", "VisualisationRegistryGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_registry_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'From', 1, 0, v_now, 'Administrator', 0, 1, 4, '30', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'To', 1, 0, v_now, 'Administrator', 0, 1, 4, '0', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Field', 1, 0, v_now, 'Administrator', 0, 1, 1, 'ChannelId', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Value Field', 1, 0, v_now, 'Administrator', 0, 1, 1, 'AmountEUR', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_parameter_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryParameter" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "DataTypeId", "DefaultValue", "Required", "Guid")
                            VALUES (v_registry_id, 'Country Field', 1, 0, v_now, 'Administrator', 0, 1, 1, 'BillingCountry', 0, v_parameter_guid);
                            INSERT INTO "VisualisationRegistryParameterRole" ("Guid", "VisualisationRegistryParameterGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_parameter_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Transactions by day', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', a."CreatedDate"), 'YYYY-MM-DD') AS "Day", count(*) AS "Transactions" FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t
                        GROUP BY date_trunc('day', a."CreatedDate") ORDER BY date_trunc('day', a."CreatedDate")$cmd$, 10, 0, 8, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Transactions', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Transactions by weekday and hour', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(a."CreatedDate", 'ID') || ' ' || to_char(a."CreatedDate", 'Dy') AS "Weekday", to_char(a."CreatedDate", 'HH24') AS "Hour", count(*) AS "Transactions"
                        FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t GROUP BY 1, 2 ORDER BY 1, 2$cmd$, 20, 0, 4, 3, v_datasource_guid, 15, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Weekday', 1, 1, 24, 1),
                                (v_datasource_id, 'Hour', 1, 2, 25, 1),
                                (v_datasource_id, 'Transactions', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Breakdown by a payload field', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        tx AS (SELECT a."Id", a."CreatedDate", a."ActivationRuleCount", a."ResponseElevation", a."Json", a."EntityAnalysisModelInstanceEntryGuid" AS g
                              FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t)
                        SELECT coalesce(tx."Json"->'payload'->>@Field, 'Not present') AS "Value of the field", count(*) AS "Transactions", count(*) FILTER (WHERE tx."ActivationRuleCount" > 0) AS "Alerts",
                               round(100.0 * count(*) FILTER (WHERE tx."ActivationRuleCount" > 0) / count(*), 1) AS "Alert rate %"
                        FROM tx GROUP BY 1 ORDER BY 2 DESC LIMIT 30$cmd$, 30, 1, 6, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Value of the field', 1, 1, 1, 1),
                                (v_datasource_id, 'Transactions', 2, 2, 2, 1),
                                (v_datasource_id, 'Alerts', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Alert rate %', 3, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Value by a payload field', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        tx AS (SELECT a."Id", a."CreatedDate", a."ActivationRuleCount", a."ResponseElevation", a."Json", a."EntityAnalysisModelInstanceEntryGuid" AS g
                              FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t)
                        SELECT coalesce(tx."Json"->'payload'->>@Field, 'Not present') AS "Value of the field", round(sum(CASE WHEN tx."Json"->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (tx."Json"->'payload'->>@Value_Field)::numeric END), 0) AS "Value" FROM tx GROUP BY 1 HAVING sum(CASE WHEN tx."Json"->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (tx."Json"->'payload'->>@Value_Field)::numeric END) > 0 ORDER BY 2 DESC LIMIT 40$cmd$, 40, 0, 6, 3, v_datasource_guid, 26, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Value of the field', 1, 1, 1, 1),
                                (v_datasource_id, 'Value', 3, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Amount distribution', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        tx AS (SELECT a."Id", a."CreatedDate", a."ActivationRuleCount", a."ResponseElevation", a."Json", a."EntityAnalysisModelInstanceEntryGuid" AS g
                              FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t)
                        SELECT CASE WHEN tx."Json"->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (tx."Json"->'payload'->>@Value_Field)::numeric END AS "Amount" FROM tx WHERE CASE WHEN tx."Json"->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (tx."Json"->'payload'->>@Value_Field)::numeric END IS NOT NULL LIMIT 5000$cmd$, 50, 0, 4, 3, v_datasource_guid, 14, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Amount', 3, 1, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Response elevation by day', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', a."CreatedDate"), 'YYYY-MM-DD') AS "Day",
                               CASE WHEN a."ResponseElevation" < 0.5 THEN '1: below 0.5' WHEN a."ResponseElevation" < 1 THEN '2: 0.5 to 1' WHEN a."ResponseElevation" < 2 THEN '3: 1 to 2' WHEN a."ResponseElevation" < 3 THEN '4: 2 to 3' ELSE '5: 3 and above' END AS "Band",
                               count(*) AS "Transactions"
                        FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t GROUP BY date_trunc('day', a."CreatedDate"), 2 ORDER BY date_trunc('day', a."CreatedDate"), 2$cmd$, 60, 0, 8, 3, v_datasource_guid, 3, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Band', 1, 2, 3, 1),
                                (v_datasource_id, 'Transactions', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Transactions by country', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        tx AS (SELECT a."Id", a."CreatedDate", a."ActivationRuleCount", a."ResponseElevation", a."Json", a."EntityAnalysisModelInstanceEntryGuid" AS g
                              FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t)
                        SELECT tx."Json"->'payload'->>@Country_Field AS "Country", count(*) AS "Transactions" FROM tx WHERE tx."Json"->'payload'->>@Country_Field IS NOT NULL GROUP BY 1 ORDER BY 2 DESC$cmd$, 70, 0, 6, 3, v_datasource_guid, 28, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Country', 1, 1, 18, 1),
                                (v_datasource_id, 'Transactions', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Alert rate by country', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        tx AS (SELECT a."Id", a."CreatedDate", a."ActivationRuleCount", a."ResponseElevation", a."Json", a."EntityAnalysisModelInstanceEntryGuid" AS g
                              FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t)
                        SELECT tx."Json"->'payload'->>@Country_Field AS "Country", count(*) AS "Transactions", count(*) FILTER (WHERE tx."ActivationRuleCount" > 0) AS "Alerts",
                               round(100.0 * count(*) FILTER (WHERE tx."ActivationRuleCount" > 0) / count(*), 1) AS "Alert rate %"
                        FROM tx WHERE tx."Json"->'payload'->>@Country_Field IS NOT NULL GROUP BY 1 HAVING count(*) >= 10 ORDER BY 4 DESC$cmd$, 80, 1, 6, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Country', 1, 1, 1, 1),
                                (v_datasource_id, 'Transactions', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Alerts', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Alert rate %', 3, 4, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Largest transactions', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        tx AS (SELECT a."Id", a."CreatedDate", a."ActivationRuleCount", a."ResponseElevation", a."Json", a."EntityAnalysisModelInstanceEntryGuid" AS g
                              FROM "Archive" a CROSS JOIN w WHERE a."CreatedDate" >= w.f AND a."CreatedDate" < w.t)
                        SELECT tx."CreatedDate" AS "When", tx."Json"->'payload'->>'TxnId' AS "Transaction", tx."Json"->'payload'->>'AccountId' AS "Account", CASE WHEN tx."Json"->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (tx."Json"->'payload'->>@Value_Field)::numeric END AS "Amount",
                               tx."Json"->'payload'->>@Country_Field AS "Country", tx."ActivationRuleCount" AS "Rules fired", tx."ResponseElevation" AS "Response elevation"
                        FROM tx WHERE CASE WHEN tx."Json"->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (tx."Json"->'payload'->>@Value_Field)::numeric END IS NOT NULL ORDER BY CASE WHEN tx."Json"->'payload'->>@Value_Field ~ '^-?[0-9.]+$' THEN (tx."Json"->'payload'->>@Value_Field)::numeric END DESC LIMIT 100$cmd$, 90, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'When', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Transaction', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Account', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Amount', 3, 4, NULL, NULL),
                                (v_datasource_id, 'Country', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Rules fired', 2, 6, NULL, NULL),
                                (v_datasource_id, 'Response elevation', 3, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Tags applied by day', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT to_char(date_trunc('day', t."CreatedDate"), 'YYYY-MM-DD') AS "Day", t."Name" AS "Tag", count(*) AS "Tags"
                        FROM "ArchiveTag" t CROSS JOIN w WHERE (t."Deleted" = 0 OR t."Deleted" IS NULL) AND t."CreatedDate" >= w.f AND t."CreatedDate" < w.t
                        GROUP BY date_trunc('day', t."CreatedDate"), t."Name" ORDER BY date_trunc('day', t."CreatedDate"), 2$cmd$, 100, 0, 6, 3, v_datasource_guid, 3, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Day', 1, 1, 1, 1),
                                (v_datasource_id, 'Tag', 1, 2, 3, 1),
                                (v_datasource_id, 'Tags', 2, 3, 2, 1);
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
