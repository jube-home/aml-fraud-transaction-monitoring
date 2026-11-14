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

namespace Jube.Migrations.Branches.Svirfd
{
    using FluentMigrator;

    [Migration(20263108500000)]
    public class CreateEntityAnalysisModelSvirfdTables : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE TABLE public."EntityAnalysisModelSvirfd" (
                            "Id" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "EntityAnalysisModelId" integer NOT NULL REFERENCES public."EntityAnalysisModel"("Id"),
                            "Name" text NOT NULL,
                            "RequestXPathName" text NOT NULL,
                            "ResponsePayload" smallint NOT NULL DEFAULT 0,
                            "ReportTable" smallint NOT NULL DEFAULT 1,
                            "Active" smallint NOT NULL DEFAULT 0,
                            "Locked" smallint NOT NULL DEFAULT 0,
                            "Version" integer NULL,
                            "Guid" uuid NOT NULL DEFAULT gen_random_uuid(),
                            "CreatedDate" timestamp without time zone NULL,
                            "CreatedUser" text NULL,
                            "UpdatedDate" timestamp without time zone NULL,
                            "UpdatedUser" text NULL,
                            "Deleted" smallint NULL,
                            "DeletedDate" timestamp without time zone NULL,
                            "DeletedUser" text NULL,
                            "ImportId" integer NULL
                        );
                        CREATE INDEX "IX_EntityAnalysisModelSvirfd_Model" ON public."EntityAnalysisModelSvirfd" ("EntityAnalysisModelId", "Deleted");
                        CREATE UNIQUE INDEX "IX_EntityAnalysisModelSvirfd_Name" ON public."EntityAnalysisModelSvirfd" ("EntityAnalysisModelId", lower("Name")) WHERE COALESCE("Deleted", 0) = 0;

                        CREATE TABLE public."EntityAnalysisModelSvirfdVersion" (
                            "Id" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "EntityAnalysisModelSvirfdId" integer NOT NULL,
                            "EntityAnalysisModelId" integer NOT NULL,
                            "Name" text NOT NULL,
                            "RequestXPathName" text NOT NULL,
                            "ResponsePayload" smallint NOT NULL DEFAULT 0,
                            "ReportTable" smallint NOT NULL DEFAULT 1,
                            "Active" smallint NOT NULL DEFAULT 0,
                            "Locked" smallint NOT NULL DEFAULT 0,
                            "Version" integer NULL,
                            "Guid" uuid NULL,
                            "CreatedDate" timestamp without time zone NULL,
                            "CreatedUser" text NULL,
                            "UpdatedDate" timestamp without time zone NULL,
                            "UpdatedUser" text NULL,
                            "Deleted" smallint NULL,
                            "DeletedDate" timestamp without time zone NULL,
                            "DeletedUser" text NULL
                        );
                        CREATE INDEX "IX_EntityAnalysisModelSvirfdVersion_Parent" ON public."EntityAnalysisModelSvirfdVersion" ("EntityAnalysisModelSvirfdId");

                        COMMENT ON TABLE public."EntityAnalysisModelSvirfd" IS 'An IP address field of a model that is looked up for IP intelligence (svirfd.com) at invocation: geolocation with its evidence and confidence, ASN and routing with RPKI, VPN, Tor and relay lists, cloud and connection type. The whole record is read as a typed object under Svirfd.{Name}.{Member} in every rule type, returned in the response when ResponsePayload is 1 and archived for the case journal when ReportTable is 1. The HTTP Adaptation twin for IP intelligence.';
                        COMMENT ON COLUMN public."EntityAnalysisModelSvirfd"."Name" IS 'The entry name used in rules as Svirfd.{Name}.{Member}; unique within the model, identifier-safe.';
                        COMMENT ON COLUMN public."EntityAnalysisModelSvirfd"."RequestXPathName" IS 'The model request XPath of data type IP (8) whose value is the address looked up.';
                        COMMENT ON COLUMN public."EntityAnalysisModelSvirfd"."ResponsePayload" IS 'When 1 the whole IP intelligence record is returned in the response JSON under Svirfd.';
                        COMMENT ON COLUMN public."EntityAnalysisModelSvirfd"."ReportTable" IS 'When 1 the record is written into the archive JSON under svirfd, which reporting, training and the case journal drift comparison read.';
                        COMMENT ON TABLE public."EntityAnalysisModelSvirfdVersion" IS 'Version history of EntityAnalysisModelSvirfd, one row per saved change.';
                        """);

            Execute.Sql("""
                        DO $demo$
                        DECLARE
                            v_model integer;
                            v_tenant integer;
                        BEGIN
                            SELECT "Id", "TenantRegistryId" INTO v_model, v_tenant FROM "EntityAnalysisModel"
                            WHERE "Guid" = '90c425fd-101a-420b-91d1-cb7a24a969cc' AND coalesce("Deleted", 0) = 0;

                            IF v_model IS NULL THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "EntityAnalysisModelSvirfd" WHERE "EntityAnalysisModelId" = v_model) THEN
                                RETURN;
                            END IF;

                            UPDATE "EntityAnalysisModelRequestXpath" SET "DataTypeId" = 8, "DefaultValue" = CASE WHEN "DefaultValue" = '123.456.789.200' THEN '8.8.8.8' ELSE "DefaultValue" END
                            WHERE "EntityAnalysisModelId" = v_model AND "Name" = 'IP' AND coalesce("DataTypeId", 1) = 1 AND coalesce("Deleted", 0) = 0;

                            UPDATE "EntityAnalysisModelRequestXpathVersion" SET "DataTypeId" = 8, "DefaultValue" = CASE WHEN "DefaultValue" = '123.456.789.200' THEN '8.8.8.8' ELSE "DefaultValue" END
                            WHERE "EntityAnalysisModelId" = v_model AND "Name" = 'IP' AND coalesce("DataTypeId", 1) = 1;

                            IF NOT EXISTS (SELECT 1 FROM "EntityAnalysisModelRequestXpath"
                                WHERE "EntityAnalysisModelId" = v_model AND "Name" = 'IP' AND "DataTypeId" = 8 AND coalesce("Deleted", 0) = 0) THEN
                                RETURN;
                            END IF;

                            INSERT INTO "EntityAnalysisModelSvirfd" ("EntityAnalysisModelId", "Name", "RequestXPathName",
                                "ResponsePayload", "ReportTable", "Active", "Locked", "Version", "CreatedDate", "CreatedUser", "Deleted")
                            VALUES
                                (v_model, 'IP', 'IP', 1, 1, 1, 0, 1, now() at time zone 'utc', 'Administrator', 0);

                            INSERT INTO "EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId", "EntityVersion", "StateId", "CreatedDate", "CreatedUser", "Guid")
                            SELECT v_tenant, 21, r."Id", r."Version", 1, now() at time zone 'utc', 'Administrator', gen_random_uuid()
                            FROM "EntityAnalysisModelSvirfd" r WHERE r."EntityAnalysisModelId" = v_model;

                            INSERT INTO "EntityAnalysisModelActivationRule" ("EntityAnalysisModelId", "Name", "BuilderRuleScript", "CoderRuleScript", "RuleScriptTypeId", "Json",
                                "ResponseElevation", "EnableCaseWorkflow", "CaseKey", "Active", "Locked", "Version", "Guid", "CreatedUser", "CreatedDate",
                                "EnableTtlCounter", "ResponsePayload", "ReportTable", "EnableNotification", "EnableResponseElevation", "EnableBypass", "Visible", "EnableReprocessing",
                                "ActivationSample", "Deleted")
                            VALUES
                                (v_model, 'SvirfdIpIsVpnOrTor', 'Return False',
                                    E'If Svirfd.IP.Privacy.Vpn = True Or Svirfd.IP.Privacy.Tor = True Or Svirfd.IP.Privacy.Relay = True Then\n  Return True\nEnd If', '2', NULL,
                                    0, 0, '', 1, 0, 1, gen_random_uuid(), 'Administrator', now() at time zone 'utc', 0, 1, 1, 0, 0, 0, 1, 1, 1, 0),
                                (v_model, 'SvirfdIpIsHostingOrInvalidRoute', 'Return False',
                                    E'If Svirfd.IP.Connection.Hosting = True Or Svirfd.IP.Routing.Origin.Rpki = "invalid" Or Svirfd.IP.Bogon = True Then\n  Return True\nEnd If', '2', NULL,
                                    0, 0, '', 1, 0, 1, gen_random_uuid(), 'Administrator', now() at time zone 'utc', 0, 1, 1, 0, 0, 0, 1, 1, 1, 0),
                                (v_model, 'SvirfdIpFarFromAccount', 'Return False',
                                    E'If Svirfd.IP.DistanceKm(Data.AccountLatitude, Data.AccountLongitude) > 500 Then\n  Return True\nEnd If', '2', NULL,
                                    0, 0, '', 1, 0, 1, gen_random_uuid(), 'Administrator', now() at time zone 'utc', 0, 1, 1, 0, 0, 0, 1, 1, 1, 0);

                            INSERT INTO "EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId", "EntityVersion", "StateId", "CreatedDate", "CreatedUser", "Guid")
                            SELECT v_tenant, 16, r."Id", r."Version", 1, now() at time zone 'utc', 'Administrator', gen_random_uuid()
                            FROM "EntityAnalysisModelActivationRule" r WHERE r."EntityAnalysisModelId" = v_model AND r."Name" LIKE 'Svirfd%' AND coalesce(r."Deleted", 0) = 0;

                            INSERT INTO "EntityAnalysisModelSynchronisationSchedule" ("CreatedDate", "ScheduleDate", "TenantRegistryId", "CreatedUser", "EntityAnalysisModelId")
                            VALUES (now() at time zone 'utc', now() at time zone 'utc', v_tenant, 'Administrator', v_model);
                        END
                        $demo$;
                        """);
        }

        public override void Down()
        {
        }
    }
}
