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

    [Migration(2026111792000013)]
    public class SeedModelsMachineLearning : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Models: Machine Learning' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Models: Machine Learning', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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
                            VALUES (v_registry_id, 'Exhaustive models', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT x."Name" AS "Model", CASE x."StatusId" WHEN 16 THEN 'Finished' WHEN 19 THEN 'Stopped' WHEN 20 THEN 'No data' ELSE 'Status ' || x."StatusId" END AS "Status", x."Models" AS "Models tried", round(x."Score"::numeric, 3) AS "Score",
                               x."CreatedDate" AS "Started", x."CompletedDate" AS "Completed", round(extract(epoch FROM ((now() at time zone 'utc') - x."CompletedDate")) / 86400)::integer AS "Days since trained"
                        FROM "ExhaustiveSearchInstance" x WHERE (x."Deleted" = 0 OR x."Deleted" IS NULL) ORDER BY x."Name"$cmd$, 10, 1, 12, 2, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Model', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Status', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Models tried', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Score', 3, 4, NULL, NULL),
                                (v_datasource_id, 'Started', 4, 5, NULL, NULL),
                                (v_datasource_id, 'Completed', 4, 6, NULL, NULL),
                                (v_datasource_id, 'Days since trained', 2, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Promoted model confusion matrix', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT v.actual AS "Actual", v.predicted AS "Predicted", v.n AS "Records"
                        FROM (SELECT pt."TruePositive" AS tp, pt."TrueNegative" AS tn, pt."FalsePositive" AS fp, pt."FalseNegative" AS fn
                              FROM "ExhaustiveSearchInstancePromotedTrialInstance" pt WHERE (pt."Deleted" = 0 OR pt."Deleted" IS NULL) ORDER BY pt."Id" DESC LIMIT 1) p
                        CROSS JOIN LATERAL (VALUES ('Fraud', 'Fraud', p.tp), ('Fraud', 'Not fraud', p.fn), ('Not fraud', 'Fraud', p.fp), ('Not fraud', 'Not fraud', p.tn)) AS v(actual, predicted, n)$cmd$, 20, 0, 4, 3, v_datasource_guid, 15, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Actual', 1, 1, 24, 1),
                                (v_datasource_id, 'Predicted', 1, 2, 25, 1),
                                (v_datasource_id, 'Records', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'ROC curve of the promoted model', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT round((r."FalsePositive"::numeric / NULLIF(r."FalsePositive" + r."TrueNegative", 0)), 4) AS "False positive rate", round((r."TruePositive"::numeric / NULLIF(r."TruePositive" + r."FalseNegative", 0)), 4) AS "True positive rate"
                        FROM "ExhaustiveSearchInstancePromotedTrialInstanceRoc" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL) ORDER BY r."Threshold" LIMIT 2000$cmd$, 30, 0, 4, 3, v_datasource_guid, 13, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'False positive rate', 3, 1, 4, 1),
                                (v_datasource_id, 'True positive rate', 3, 2, 5, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Variable correlation with the outcome', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT v."Name" AS "Variable", round(v."Correlation"::numeric, 4) AS "Correlation", v."CorrelationAbsRank" AS "Rank" FROM "ExhaustiveSearchInstanceVariable" v
                        WHERE (v."Deleted" = 0 OR v."Deleted" IS NULL) AND v."Correlation" IS NOT NULL ORDER BY v."CorrelationAbsRank" LIMIT 25$cmd$, 40, 1, 4, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Variable', 1, 1, 1, 1),
                                (v_datasource_id, 'Correlation', 3, 2, 2, 1),
                                (v_datasource_id, 'Rank', 2, 3, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Score of each trial over the search', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT row_number() OVER (ORDER BY t."CreatedDate", t."Id")::text AS "Trial", round(t."Score"::numeric, 4) AS "Score"
                        FROM "ExhaustiveSearchInstanceTrialInstance" t WHERE (t."Deleted" = 0 OR t."Deleted" IS NULL) AND t."Score" IS NOT NULL ORDER BY t."CreatedDate", t."Id" LIMIT 2000$cmd$, 50, 0, 6, 3, v_datasource_guid, 7, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Trial', 1, 1, 1, 1),
                                (v_datasource_id, 'Score', 3, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Predicted against actual', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT round(p."Predicted"::numeric, 4) AS "Predicted", p."Actual" AS "Actual" FROM "ExhaustiveSearchInstancePromotedTrialInstancePredictedActual" p WHERE (p."Deleted" = 0 OR p."Deleted" IS NULL) LIMIT 2000$cmd$, 60, 0, 6, 3, v_datasource_guid, 13, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Predicted', 3, 1, 4, 1),
                                (v_datasource_id, 'Actual', 3, 2, 5, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Advanced analytics training runs', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT r."CreatedDate" AS "Created", r."CreatedUser" AS "By", r."KindId" AS "Kind", r."StatusId" AS "Status", round(r."Progress"::numeric, 2) AS "Progress", r."StartedDate" AS "Started", r."CompletedDate" AS "Completed", r."Error" AS "Error"
                        FROM "AdvancedAnalyticsTrainingRun" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL) ORDER BY r."CreatedDate" DESC LIMIT 500$cmd$, 70, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Created', 4, 1, NULL, NULL),
                                (v_datasource_id, 'By', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Kind', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Status', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Progress', 3, 5, NULL, NULL),
                                (v_datasource_id, 'Started', 4, 6, NULL, NULL),
                                (v_datasource_id, 'Completed', 4, 7, NULL, NULL),
                                (v_datasource_id, 'Error', 1, 8, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Advanced analytics metrics by run', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT m."Name" AS "Metric", round(avg(m."Value")::numeric, 4) AS "Value" FROM "AdvancedAnalyticsTrainingRunMetric" m GROUP BY 1 ORDER BY 1$cmd$, 80, 0, 6, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Metric', 1, 1, 1, 1),
                                (v_datasource_id, 'Value', 3, 2, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Advanced analytics variable importance', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT v."Name" AS "Variable", round(avg(v."Importance")::numeric, 4) AS "Importance" FROM "AdvancedAnalyticsTrainingRunVariable" v WHERE v."Importance" IS NOT NULL GROUP BY 1 ORDER BY 2 DESC LIMIT 25$cmd$, 90, 0, 6, 3, v_datasource_guid, 2, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Variable', 1, 1, 1, 1),
                                (v_datasource_id, 'Importance', 3, 2, 2, 1);
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
