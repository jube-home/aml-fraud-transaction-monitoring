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

    [Migration(2026111792000010)]
    public class SeedCasesSuppressionsAndOverrides : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Cases: Suppressions and Overrides' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Cases: Suppressions and Overrides', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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
                            VALUES (v_registry_id, 'Suppression requests by state', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT CASE s."StateId" WHEN 1 THEN 'Pending' WHEN 2 THEN 'Approved' WHEN 3 THEN 'Rejected' ELSE 'Retracted' END AS "State", count(*) AS "Requests"
                        FROM "CaseSuppression" s CROSS JOIN w WHERE s."CreatedDate" >= w.f AND s."CreatedDate" < w.t GROUP BY 1 ORDER BY 2 DESC$cmd$, 10, 0, 4, 3, v_datasource_guid, 12, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'State', 1, 1, 1, 1),
                                (v_datasource_id, 'Requests', 2, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Suppression requests by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms)
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month", CASE s."StateId" WHEN 1 THEN 'Pending' WHEN 2 THEN 'Approved' WHEN 3 THEN 'Rejected' ELSE 'Retracted' END AS "State", count(*) AS "Requests"
                        FROM m JOIN "CaseSuppression" s ON s."CreatedDate" >= m.ms AND s."CreatedDate" < m.ms + interval '1 month' GROUP BY m.ms, 2 ORDER BY m.ms, 2$cmd$, 20, 0, 8, 3, v_datasource_guid, 3, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'State', 1, 2, 3, 1),
                                (v_datasource_id, 'Requests', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Hours to decide a suppression', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT round((extract(epoch FROM (s."DecidedDate" - s."CreatedDate")) / 3600.0)::numeric, 1) AS "Hours" FROM "CaseSuppression" s CROSS JOIN w
                        WHERE s."DecidedDate" IS NOT NULL AND s."CreatedDate" >= w.f AND s."CreatedDate" < w.t LIMIT 5000$cmd$, 30, 0, 4, 3, v_datasource_guid, 14, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Hours', 3, 1, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Suppressions expiring in 30 days', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT s."Id" AS "Request", s."CaseKeyValue" AS "Key", s."ExpiryDate" AS "Expires", round(extract(epoch FROM (s."ExpiryDate" - (now() at time zone 'utc'))) / 86400)::integer AS "Days left",
                               s."CreatedUser" AS "Requested by", s."DecidedUser" AS "Approved by", s."Reason" AS "Reason"
                        FROM "CaseSuppression" s WHERE s."StateId" = 2 AND s."ExpiryDate" >= (now() at time zone 'utc') AND s."ExpiryDate" <= (now() at time zone 'utc') + interval '30 days' ORDER BY s."ExpiryDate" LIMIT 500$cmd$, 40, 1, 8, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Request', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Expires', 4, 3, NULL, NULL),
                                (v_datasource_id, 'Days left', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Requested by', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Approved by', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Reason', 1, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Wider impact of suppressions', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT s."Id" AS "Request", s."CaseKeyValue" AS "Key", CASE s."StateId" WHEN 1 THEN 'Pending' WHEN 2 THEN 'Approved' WHEN 3 THEN 'Rejected' ELSE 'Retracted' END AS "State",
                               s."CountInCase" AS "Transactions in the case", s."WiderTransactions" AS "Other transactions covered", s."WiderKeys" AS "Other keys covered", CASE WHEN s."WiderCapped" = 1 THEN 'Capped' ELSE 'Complete' END AS "Count"
                        FROM "CaseSuppression" s CROSS JOIN w WHERE s."CreatedDate" >= w.f AND s."CreatedDate" < w.t ORDER BY s."WiderTransactions" DESC NULLS LAST LIMIT 500$cmd$, 50, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Request', 2, 1, NULL, NULL),
                                (v_datasource_id, 'Key', 1, 2, NULL, NULL),
                                (v_datasource_id, 'State', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Transactions in the case', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Other transactions covered', 2, 5, NULL, NULL),
                                (v_datasource_id, 'Other keys covered', 2, 6, NULL, NULL),
                                (v_datasource_id, 'Count', 1, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Overrides in force by rule and effect', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT o."EntityAnalysisModelActivationRuleName" AS "Rule", CASE o."OverrideKind" WHEN 1 THEN 'Force' ELSE 'Suppress' END AS "Effect", count(*) AS "Overrides", min(o."DeleteExpiryDate") AS "Soonest expiry"
                        FROM "EntityAnalysisModelActivationRuleOverride" o WHERE (o."Deleted" = 0 OR o."Deleted" IS NULL) AND (o."DeleteExpiryDate" IS NULL OR o."DeleteExpiryDate" > (now() at time zone 'utc')) GROUP BY 1, 2 ORDER BY 3 DESC$cmd$, 60, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Rule', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Effect', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Overrides', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Soonest expiry', 4, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Overrides created by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH m AS (SELECT generate_series(date_trunc('month', now() at time zone 'utc') - interval '11 months', date_trunc('month', now() at time zone 'utc'), interval '1 month') AS ms)
                        SELECT to_char(m.ms, 'YYYY-MM') AS "Month", count(o."Id") AS "Overrides" FROM m LEFT JOIN "EntityAnalysisModelActivationRuleOverride" o ON o."CreatedDate" >= m.ms AND o."CreatedDate" < m.ms + interval '1 month' GROUP BY m.ms ORDER BY m.ms$cmd$, 70, 0, 6, 3, v_datasource_guid, 1, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Overrides', 2, 2, 2, 1);
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
