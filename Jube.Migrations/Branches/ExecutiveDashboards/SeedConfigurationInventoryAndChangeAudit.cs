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

    [Migration(2026111792000012)]
    public class SeedConfigurationInventoryAndChangeAudit : Migration
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

                            IF EXISTS (SELECT 1 FROM "VisualisationRegistry" WHERE "Name" = 'Configuration: Inventory and Change Audit' AND "TenantRegistryId" = v_tenant AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "VisualisationRegistry" ("Name", "CreatedUser", "Active", "Locked", "CreatedDate", "TenantRegistryId", "Columns", "ColumnWidth", "RowHeight", "Version", "Guid", "ContextKindId", "Deleted")
                            VALUES ('Configuration: Inventory and Change Audit', 'Administrator', 1, 0, v_now, v_tenant, 12, 120, 120, 1, v_registry_guid, 1, 0)
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
                            VALUES (v_registry_id, 'Configuration by model and kind', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH ent AS (
                            SELECT 'Activation rules' AS kind, r."EntityAnalysisModelId" AS model_id, 1 AS n FROM "EntityAnalysisModelActivationRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL SELECT 'Gateway rules', g."EntityAnalysisModelId", 1 FROM "EntityAnalysisModelGatewayRule" g WHERE (g."Deleted" = 0 OR g."Deleted" IS NULL)
                            UNION ALL SELECT 'Abstraction rules', a."EntityAnalysisModelId", 1 FROM "EntityAnalysisModelAbstractionRule" a WHERE (a."Deleted" = 0 OR a."Deleted" IS NULL)
                            UNION ALL SELECT 'Abstraction calculations', c."EntityAnalysisModelId", 1 FROM "EntityAnalysisModelAbstractionCalculation" c WHERE (c."Deleted" = 0 OR c."Deleted" IS NULL)
                            UNION ALL SELECT 'TTL counters', t."EntityAnalysisModelId", 1 FROM "EntityAnalysisModelTtlCounter" t WHERE (t."Deleted" = 0 OR t."Deleted" IS NULL)
                            UNION ALL SELECT 'Tags', g."EntityAnalysisModelId", 1 FROM "EntityAnalysisModelTag" g WHERE (g."Deleted" = 0 OR g."Deleted" IS NULL)
                            UNION ALL SELECT 'Request fields', x."EntityAnalysisModelId", 1 FROM "EntityAnalysisModelRequestXpath" x WHERE (x."Deleted" = 0 OR x."Deleted" IS NULL)
                            UNION ALL SELECT 'Inline scripts', s."EntityAnalysisModelId", 1 FROM "EntityAnalysisModelInlineScript" s WHERE (s."Deleted" = 0 OR s."Deleted" IS NULL)
                            UNION ALL SELECT 'Sanction screens', s."EntityAnalysisModelId", 1 FROM "EntityAnalysisModelSanction" s WHERE (s."Deleted" = 0 OR s."Deleted" IS NULL)
                        )
                        SELECT coalesce(m."Name", 'Unknown') AS "Model", ent.kind AS "Kind", sum(ent.n) AS "Entities" FROM ent LEFT JOIN "EntityAnalysisModel" m ON m."Id" = ent.model_id GROUP BY 1, 2 ORDER BY 1, 2$cmd$, 10, 0, 6, 3, v_datasource_guid, 4, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Model', 1, 1, 1, 1),
                                (v_datasource_id, 'Kind', 1, 2, 3, 1),
                                (v_datasource_id, 'Entities', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Changes by user and month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH chg AS (
                            SELECT 'Activation rule' AS kind, v."Name" AS name, v."Version" AS version, v."CreatedUser" AS who, v."CreatedDate" AS at FROM "EntityAnalysisModelActivationRuleVersion" v
                            UNION ALL SELECT 'Activation rule', r."Name", r."Version", coalesce(r."UpdatedUser", r."CreatedUser"), coalesce(r."UpdatedDate", r."CreatedDate") FROM "EntityAnalysisModelActivationRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL SELECT 'Gateway rule', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelGatewayRuleVersion" v
                            UNION ALL SELECT 'Gateway rule', r."Name", r."Version", coalesce(r."UpdatedUser", r."CreatedUser"), coalesce(r."UpdatedDate", r."CreatedDate") FROM "EntityAnalysisModelGatewayRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL SELECT 'Abstraction rule', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelAbstractionRuleVersion" v
                            UNION ALL SELECT 'Abstraction rule', r."Name", r."Version", coalesce(r."UpdatedUser", r."CreatedUser"), coalesce(r."UpdatedDate", r."CreatedDate") FROM "EntityAnalysisModelAbstractionRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL SELECT 'TTL counter', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelTtlCounterVersion" v
                            UNION ALL SELECT 'Tag', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelTagVersion" v
                            UNION ALL SELECT 'Request field', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelRequestXpathVersion" v
                            UNION ALL SELECT 'List', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelListVersion" v
                            UNION ALL SELECT 'Model', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelVersion" v
                        )
                        SELECT coalesce(chg.who, 'Unknown') AS "User", to_char(date_trunc('month', chg.at), 'YYYY-MM') AS "Month", count(*) AS "Changes" FROM chg
                        WHERE chg.at >= date_trunc('month', (now() at time zone 'utc') - interval '11 months') GROUP BY 1, date_trunc('month', chg.at) ORDER BY date_trunc('month', chg.at), 1$cmd$, 20, 0, 6, 3, v_datasource_guid, 15, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'User', 1, 1, 24, 1),
                                (v_datasource_id, 'Month', 1, 2, 25, 1),
                                (v_datasource_id, 'Changes', 2, 3, 2, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Change log', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w),
                        chg AS (
                            SELECT 'Activation rule' AS kind, v."Name" AS name, v."Version" AS version, v."CreatedUser" AS who, v."CreatedDate" AS at FROM "EntityAnalysisModelActivationRuleVersion" v
                            UNION ALL SELECT 'Activation rule', r."Name", r."Version", coalesce(r."UpdatedUser", r."CreatedUser"), coalesce(r."UpdatedDate", r."CreatedDate") FROM "EntityAnalysisModelActivationRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL SELECT 'Gateway rule', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelGatewayRuleVersion" v
                            UNION ALL SELECT 'Gateway rule', r."Name", r."Version", coalesce(r."UpdatedUser", r."CreatedUser"), coalesce(r."UpdatedDate", r."CreatedDate") FROM "EntityAnalysisModelGatewayRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL SELECT 'Abstraction rule', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelAbstractionRuleVersion" v
                            UNION ALL SELECT 'Abstraction rule', r."Name", r."Version", coalesce(r."UpdatedUser", r."CreatedUser"), coalesce(r."UpdatedDate", r."CreatedDate") FROM "EntityAnalysisModelAbstractionRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL SELECT 'TTL counter', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelTtlCounterVersion" v
                            UNION ALL SELECT 'Tag', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelTagVersion" v
                            UNION ALL SELECT 'Request field', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelRequestXpathVersion" v
                            UNION ALL SELECT 'List', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelListVersion" v
                            UNION ALL SELECT 'Model', v."Name", v."Version", v."CreatedUser", v."CreatedDate" FROM "EntityAnalysisModelVersion" v
                        )
                        SELECT chg.at AS "When", chg.kind AS "Kind", chg.name AS "Name", chg.version AS "Version", chg.who AS "By" FROM chg CROSS JOIN w WHERE chg.at >= w.f AND chg.at < w.t ORDER BY chg.at DESC LIMIT 1000$cmd$, 30, 1, 12, 4, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'When', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Kind', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Name', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 4, NULL, NULL),
                                (v_datasource_id, 'By', 1, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Approvals register', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT e."CreatedDate" AS "Decided", CASE e."EntityApprovalKindId" WHEN 1 THEN 'Model' WHEN 2 THEN 'List' WHEN 3 THEN 'List value' WHEN 4 THEN 'Dictionary' WHEN 5 THEN 'Dictionary value' WHEN 6 THEN 'Request field'
                            WHEN 7 THEN 'Inline script' WHEN 8 THEN 'Inline function' WHEN 9 THEN 'Gateway rule' WHEN 10 THEN 'Sanction' WHEN 11 THEN 'Abstraction rule' WHEN 12 THEN 'Abstraction calculation'
                            WHEN 13 THEN 'TTL counter' WHEN 14 THEN 'HTTP adaptation' WHEN 15 THEN 'Exhaustive model' WHEN 16 THEN 'Activation rule' WHEN 17 THEN 'Tag' WHEN 18 THEN 'Advanced analytics instance'
                            WHEN 19 THEN 'Response elevation range' WHEN 20 THEN 'Graph recall' WHEN 21 THEN 'Svirfd' WHEN 22 THEN 'Override' WHEN 23 THEN 'Rule override' ELSE 'Other' END AS "Kind", e."EntityId" AS "Entity", e."EntityVersion" AS "Version", CASE e."StateId" WHEN 1 THEN 'Approved' ELSE 'Rejected' END AS "Decision", e."CreatedUser" AS "By", e."Note" AS "Note"
                        FROM "EntityApproval" e CROSS JOIN w WHERE e."CreatedDate" >= w.f AND e."CreatedDate" < w.t ORDER BY e."CreatedDate" DESC LIMIT 1000$cmd$, 40, 1, 12, 4, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Decided', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Kind', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Entity', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Decision', 1, 5, NULL, NULL),
                                (v_datasource_id, 'By', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Note', 1, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Awaiting approval by kind', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT p.kind AS "Kind", p.name AS "Name", p.version AS "Version", p.made_by AS "Made by", round(extract(epoch FROM ((now() at time zone 'utc') - p.made_at)) / 86400)::integer AS "Days waiting"
                        FROM (
                            SELECT 'Activation rule' AS kind, 16 AS k, r."Id" AS id, r."Name" AS name, r."Version" AS version, r."CreatedUser" AS made_by, r."CreatedDate" AS made_at FROM "EntityAnalysisModelActivationRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL SELECT 'Gateway rule', 9, g."Id", g."Name", g."Version", g."CreatedUser", g."CreatedDate" FROM "EntityAnalysisModelGatewayRule" g WHERE (g."Deleted" = 0 OR g."Deleted" IS NULL)
                            UNION ALL SELECT 'Abstraction rule', 11, a."Id", a."Name", a."Version", a."CreatedUser", a."CreatedDate" FROM "EntityAnalysisModelAbstractionRule" a WHERE (a."Deleted" = 0 OR a."Deleted" IS NULL)
                            UNION ALL SELECT 'TTL counter', 13, t."Id", t."Name", t."Version", t."CreatedUser", t."CreatedDate" FROM "EntityAnalysisModelTtlCounter" t WHERE (t."Deleted" = 0 OR t."Deleted" IS NULL)
                            UNION ALL SELECT 'Tag', 17, g."Id", g."Name", g."Version", g."CreatedUser", g."CreatedDate" FROM "EntityAnalysisModelTag" g WHERE (g."Deleted" = 0 OR g."Deleted" IS NULL)
                            UNION ALL SELECT 'List', 2, l."Id", l."Name", l."Version", l."CreatedUser", l."CreatedDate" FROM "EntityAnalysisModelList" l WHERE (l."Deleted" = 0 OR l."Deleted" IS NULL)
                        ) p
                        WHERE NOT EXISTS (SELECT 1 FROM "EntityApproval" e WHERE e."EntityApprovalKindId" = p.k AND e."EntityId" = p.id AND e."EntityVersion" = p.version AND e."StateId" = 1) ORDER BY 5 DESC NULLS LAST, 1, 2 LIMIT 500$cmd$, 50, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Kind', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Name', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Made by', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Days waiting', 2, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rejected and returned changes', 1, 0, v_now, 'Administrator', 0, 1, $cmd$WITH w AS (SELECT @From::timestamp AS f, @To::timestamp + interval '1 day' AS t),
                        pw AS (SELECT w.f - (w.t - w.f) AS f, w.f AS t FROM w)
                        SELECT e."CreatedDate" AS "Decided", CASE e."EntityApprovalKindId" WHEN 1 THEN 'Model' WHEN 2 THEN 'List' WHEN 3 THEN 'List value' WHEN 4 THEN 'Dictionary' WHEN 5 THEN 'Dictionary value' WHEN 6 THEN 'Request field'
                            WHEN 7 THEN 'Inline script' WHEN 8 THEN 'Inline function' WHEN 9 THEN 'Gateway rule' WHEN 10 THEN 'Sanction' WHEN 11 THEN 'Abstraction rule' WHEN 12 THEN 'Abstraction calculation'
                            WHEN 13 THEN 'TTL counter' WHEN 14 THEN 'HTTP adaptation' WHEN 15 THEN 'Exhaustive model' WHEN 16 THEN 'Activation rule' WHEN 17 THEN 'Tag' WHEN 18 THEN 'Advanced analytics instance'
                            WHEN 19 THEN 'Response elevation range' WHEN 20 THEN 'Graph recall' WHEN 21 THEN 'Svirfd' WHEN 22 THEN 'Override' WHEN 23 THEN 'Rule override' ELSE 'Other' END AS "Kind", e."EntityId" AS "Entity", e."EntityVersion" AS "Version", CASE e."StateId" WHEN 1 THEN 'Approved' ELSE 'Rejected' END AS "Decision", e."CreatedUser" AS "By", e."Note" AS "Note",
                               e."ReturnedDate" AS "Returned", e."ReturnedUser" AS "Returned by"
                        FROM "EntityApproval" e CROSS JOIN w WHERE e."CreatedDate" >= w.f AND e."CreatedDate" < w.t AND (e."StateId" = 2 OR e."ReturnedDate" IS NOT NULL) ORDER BY e."CreatedDate" DESC LIMIT 500$cmd$, 60, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Decided', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Kind', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Entity', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 4, NULL, NULL),
                                (v_datasource_id, 'Decision', 1, 5, NULL, NULL),
                                (v_datasource_id, 'By', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Note', 1, 7, NULL, NULL),
                                (v_datasource_id, 'Returned', 4, 8, NULL, NULL),
                                (v_datasource_id, 'Returned by', 1, 9, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Rule approval turnaround by month', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT to_char(date_trunc('month', e."CreatedDate"), 'YYYY-MM') AS "Month", round(min(extract(epoch FROM (e."CreatedDate" - v."CreatedDate")) / 3600)::numeric, 1) AS "Minimum",
                               round((percentile_cont(0.25) WITHIN GROUP (ORDER BY extract(epoch FROM (e."CreatedDate" - v."CreatedDate")) / 3600))::numeric, 1) AS "First quartile",
                               round((percentile_cont(0.5) WITHIN GROUP (ORDER BY extract(epoch FROM (e."CreatedDate" - v."CreatedDate")) / 3600))::numeric, 1) AS "Median",
                               round((percentile_cont(0.75) WITHIN GROUP (ORDER BY extract(epoch FROM (e."CreatedDate" - v."CreatedDate")) / 3600))::numeric, 1) AS "Third quartile",
                               round(max(extract(epoch FROM (e."CreatedDate" - v."CreatedDate")) / 3600)::numeric, 1) AS "Maximum"
                        FROM "EntityApproval" e JOIN "EntityAnalysisModelActivationRuleVersion" v ON e."EntityApprovalKindId" = 16 AND v."EntityAnalysisModelActivationRuleId" = e."EntityId" AND v."Version" = e."EntityVersion"
                        WHERE e."StateId" IN (1, 2) AND e."CreatedDate" >= date_trunc('month', (now() at time zone 'utc') - interval '11 months') GROUP BY date_trunc('month', e."CreatedDate") ORDER BY date_trunc('month', e."CreatedDate")$cmd$, 70, 1, 6, 3, v_datasource_guid, 17, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Month', 1, 1, 1, 1),
                                (v_datasource_id, 'Minimum', 3, 2, 11, 1),
                                (v_datasource_id, 'First quartile', 3, 3, 12, 1),
                                (v_datasource_id, 'Median', 3, 4, 13, 1),
                                (v_datasource_id, 'Third quartile', 3, 5, 14, 1),
                                (v_datasource_id, 'Maximum', 3, 6, 15, 1);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Changes approved by their maker', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT e."CreatedDate" AS "Approved", r."Name" AS "Rule", e."EntityVersion" AS "Version", e."CreatedUser" AS "Maker and approver"
                        FROM "EntityApproval" e JOIN (SELECT r."Id" AS id, r."Name" AS "Name", r."Version" AS version, r."CreatedUser" AS maker FROM "EntityAnalysisModelActivationRule" r
                                                      UNION ALL SELECT v."EntityAnalysisModelActivationRuleId", v."Name", v."Version", v."CreatedUser" FROM "EntityAnalysisModelActivationRuleVersion" v) r
                          ON e."EntityApprovalKindId" = 16 AND r.id = e."EntityId" AND r.version = e."EntityVersion" AND r.maker = e."CreatedUser" ORDER BY e."CreatedDate" DESC LIMIT 500$cmd$, 80, 1, 6, 2, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Approved', 4, 1, NULL, NULL),
                                (v_datasource_id, 'Rule', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Maker and approver', 1, 4, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Inactive and locked configuration', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT x.kind AS "Kind", x.name AS "Name", CASE WHEN x.active = 0 THEN 'Inactive' ELSE 'Active' END AS "State", CASE WHEN x.locked = 1 THEN 'Locked' ELSE 'Unlocked' END AS "Lock", x.version AS "Version"
                        FROM (
                            SELECT 'Activation rule' AS kind, r."Name" AS name, coalesce(r."Active", 0) AS active, coalesce(r."Locked", 0) AS locked, r."Version" AS version FROM "EntityAnalysisModelActivationRule" r WHERE (r."Deleted" = 0 OR r."Deleted" IS NULL)
                            UNION ALL SELECT 'Gateway rule', g."Name", coalesce(g."Active", 0), coalesce(g."Locked", 0), g."Version" FROM "EntityAnalysisModelGatewayRule" g WHERE (g."Deleted" = 0 OR g."Deleted" IS NULL)
                            UNION ALL SELECT 'Abstraction rule', a."Name", coalesce(a."Active", 0), coalesce(a."Locked", 0), a."Version" FROM "EntityAnalysisModelAbstractionRule" a WHERE (a."Deleted" = 0 OR a."Deleted" IS NULL)
                            UNION ALL SELECT 'TTL counter', t."Name", coalesce(t."Active", 0), coalesce(t."Locked", 0), t."Version" FROM "EntityAnalysisModelTtlCounter" t WHERE (t."Deleted" = 0 OR t."Deleted" IS NULL)
                        ) x WHERE x.active = 0 OR x.locked = 1 ORDER BY 1, 2 LIMIT 500$cmd$, 90, 1, 6, 2, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Kind', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Name', 1, 2, NULL, NULL),
                                (v_datasource_id, 'State', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Lock', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Request fields and reporting', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT x."Name" AS "Field", x."XPath" AS "Path", CASE x."DataTypeId" WHEN 1 THEN 'String' WHEN 2 THEN 'Integer' WHEN 3 THEN 'Float' WHEN 4 THEN 'Date' WHEN 5 THEN 'Boolean' ELSE 'Other' END AS "Type",
                               CASE WHEN coalesce(x."ReportTable", 0) = 1 THEN 'Yes' ELSE 'No' END AS "Available to reports", CASE WHEN x."SearchKey" = 1 THEN 'Yes' ELSE 'No' END AS "Search key"
                        FROM "EntityAnalysisModelRequestXpath" x WHERE (x."Deleted" = 0 OR x."Deleted" IS NULL) ORDER BY 4 DESC, 1 LIMIT 500$cmd$, 100, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Field', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Path', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Type', 1, 3, NULL, NULL),
                                (v_datasource_id, 'Available to reports', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Search key', 1, 5, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'TTL counter inventory', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT t."Name" AS "Counter", t."TtlCounterDataName" AS "Counts field", t."TtlCounterValue" AS "Window", t."TtlCounterInterval" AS "Interval", CASE WHEN t."OnlineAggregation" = 1 THEN 'Yes' ELSE 'No' END AS "Online aggregation",
                               CASE WHEN t."Active" = 1 THEN 'Yes' ELSE 'No' END AS "Active", t."Version" AS "Version" FROM "EntityAnalysisModelTtlCounter" t WHERE (t."Deleted" = 0 OR t."Deleted" IS NULL) ORDER BY t."Name" LIMIT 500$cmd$, 110, 1, 6, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'Counter', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Counts field', 1, 2, NULL, NULL),
                                (v_datasource_id, 'Window', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Interval', 1, 4, NULL, NULL),
                                (v_datasource_id, 'Online aggregation', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Active', 1, 6, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 7, NULL, NULL);
                            INSERT INTO "VisualisationRegistryDatasourceRole" ("Guid", "VisualisationRegistryDatasourceGuid", "RoleRegistryGuid", "CreatedDate", "CreatedUser", "Deleted", "Version")
                            VALUES (gen_random_uuid(), v_datasource_guid, v_role, v_now, 'Administrator', 0, 1);

                            v_datasource_guid := gen_random_uuid();
                            INSERT INTO "VisualisationRegistryDatasource" ("VisualisationRegistryId", "Name", "Active", "Locked", "CreatedDate", "CreatedUser", "Deleted", "Version", "Command", "Priority", "IncludeGrid", "ColumnSpan", "RowSpan", "Guid", "DisplayKindId", "TemplateText")
                            VALUES (v_registry_id, 'Lists and values expiring', 1, 0, v_now, 'Administrator', 0, 1, $cmd$SELECT l."Name" AS "List", count(v."Id") AS "Values", count(v."Id") FILTER (WHERE v."DeleteExpiryDate" IS NOT NULL AND v."DeleteExpiryDate" <= (now() at time zone 'utc') + interval '30 days') AS "Expiring in 30 days",
                               min(v."DeleteExpiryDate") AS "Soonest expiry", CASE WHEN l."Active" = 1 THEN 'Yes' ELSE 'No' END AS "Active", l."Version" AS "Version"
                        FROM "EntityAnalysisModelList" l LEFT JOIN "EntityAnalysisModelListValue" v ON v."EntityAnalysisModelListId" = l."Id" AND (v."Deleted" = 0 OR v."Deleted" IS NULL)
                        WHERE (l."Deleted" = 0 OR l."Deleted" IS NULL) GROUP BY l."Id", l."Name", l."Active", l."Version" ORDER BY 1$cmd$, 120, 1, 12, 3, v_datasource_guid, NULL, NULL)
                            RETURNING "Id" INTO v_datasource_id;
                            INSERT INTO "VisualisationRegistryDatasourceSeries" ("VisualisationRegistryDatasourceId", "Name", "DataTypeId", "Ordinal", "RoleId", "RoleOrder")
                            VALUES
                                (v_datasource_id, 'List', 1, 1, NULL, NULL),
                                (v_datasource_id, 'Values', 2, 2, NULL, NULL),
                                (v_datasource_id, 'Expiring in 30 days', 2, 3, NULL, NULL),
                                (v_datasource_id, 'Soonest expiry', 4, 4, NULL, NULL),
                                (v_datasource_id, 'Active', 1, 5, NULL, NULL),
                                (v_datasource_id, 'Version', 2, 6, NULL, NULL);
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
