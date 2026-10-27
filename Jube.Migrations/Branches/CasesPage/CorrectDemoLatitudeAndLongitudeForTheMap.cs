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

namespace Jube.Migrations.Branches.CasesPage
{
    using FluentMigrator;

    [Migration(20263108090000)]
    public class CorrectDemoLatitudeAndLongitudeForTheMap : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        WITH corrected AS (
                            UPDATE "EntityAnalysisModelRequestXpath" f
                            SET "DataTypeId" = CASE f."Name" WHEN 'AccountLatitude' THEN 6 ELSE 7 END,
                                "UpdatedDate" = now() at time zone 'utc',
                                "UpdatedUser" = 'Administrator'
                            WHERE f."XPath" = '$.' || f."Name"
                              AND ((f."Name" = 'AccountLatitude' AND f."DataTypeId" = 7)
                                OR (f."Name" = 'AccountLongitude' AND f."DataTypeId" = 6))
                              AND coalesce(f."Deleted", 0) = 0
                            RETURNING f."Id", f."EntityAnalysisModelId"
                        ),
                        history AS (
                            UPDATE "EntityAnalysisModelRequestXpathVersion" v
                            SET "DataTypeId" = CASE v."Name" WHEN 'AccountLatitude' THEN 6 ELSE 7 END
                            FROM corrected c
                            WHERE v."EntityAnalysisModelRequestXpathId" = c."Id"
                              AND v."Name" IN ('AccountLatitude', 'AccountLongitude')
                            RETURNING v."Id"
                        )
                        INSERT INTO "EntityAnalysisModelSynchronisationSchedule"
                            ("CreatedDate", "ScheduleDate", "TenantRegistryId", "CreatedUser", "EntityAnalysisModelId")
                        SELECT now() at time zone 'utc', now() at time zone 'utc', m."TenantRegistryId", 'Administrator', m."Id"
                        FROM "EntityAnalysisModel" m
                        WHERE m."Id" IN (SELECT c."EntityAnalysisModelId" FROM corrected c)
                          AND m."TenantRegistryId" IS NOT NULL;
                        """);

            Execute.Sql("""
                        INSERT INTO "CaseWorkflowXPath"
                            ("CaseWorkflowId", "Name", "XPath", "Active", "CreatedDate", "CreatedUser", "Version", "Guid",
                             "BoldLineFormatForeColor", "BoldLineFormatBackColor",
                             "ConditionalFormatForeColor", "ConditionalFormatBackColor")
                        SELECT w."Id", n."Name", 'payload.' || n."Name", 1, now() at time zone 'utc', 'Administrator', 1,
                               gen_random_uuid(), '#000000', '#ffffff', '#000000', '#ffffff'
                        FROM "CaseWorkflow" w
                        CROSS JOIN (VALUES ('AccountLatitude'), ('AccountLongitude')) AS n("Name")
                        WHERE coalesce(w."Deleted", 0) = 0
                          AND EXISTS (SELECT 1
                                      FROM "EntityAnalysisModelRequestXpath" f
                                      WHERE f."EntityAnalysisModelId" = w."EntityAnalysisModelId"
                                        AND f."Name" = n."Name"
                                        AND f."XPath" = '$.' || n."Name"
                                        AND coalesce(f."Deleted", 0) = 0)
                          AND NOT EXISTS (SELECT 1
                                          FROM "CaseWorkflowXPath" x
                                          WHERE x."CaseWorkflowId" = w."Id"
                                            AND x."XPath" = 'payload.' || n."Name"
                                            AND coalesce(x."Deleted", 0) = 0);
                        """);

            Execute.Sql("""
                        INSERT INTO "CaseWorkflowXPathRole"
                            ("RoleRegistryGuid", "CaseWorkflowXPathGuid", "Guid", "CreatedDate", "CreatedUser", "Version")
                        SELECT g."RoleRegistryGuid", g."CaseWorkflowXPathGuid", gen_random_uuid(), now() at time zone 'utc',
                               'Administrator', 1
                        FROM (SELECT DISTINCT r."RoleRegistryGuid", x."Guid" AS "CaseWorkflowXPathGuid"
                              FROM "CaseWorkflowXPath" x
                              JOIN "CaseWorkflowXPath" seen ON seen."CaseWorkflowId" = x."CaseWorkflowId"
                                                           AND seen."Id" <> x."Id"
                                                           AND coalesce(seen."Deleted", 0) = 0
                              JOIN "CaseWorkflowXPathRole" r ON r."CaseWorkflowXPathGuid" = seen."Guid"
                                                            AND coalesce(r."Deleted", 0) = 0
                              WHERE x."XPath" IN ('payload.AccountLatitude', 'payload.AccountLongitude')
                                AND coalesce(x."Deleted", 0) = 0) g
                        WHERE NOT EXISTS (SELECT 1
                                          FROM "CaseWorkflowXPathRole" own
                                          WHERE own."CaseWorkflowXPathGuid" = g."CaseWorkflowXPathGuid"
                                            AND own."RoleRegistryGuid" = g."RoleRegistryGuid");
                        """);
        }

        public override void Down()
        {
        }
    }
}
