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

namespace Jube.Migrations.Branches.Graph
{
    using FluentMigrator;

    [Migration(20263108400000)]
    public class CreateEntityAnalysisModelGraphRecallTables : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE TABLE public."EntityAnalysisModelGraphRecall" (
                            "Id" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "EntityAnalysisModelId" integer NOT NULL REFERENCES public."EntityAnalysisModel"("Id"),
                            "Name" text NOT NULL,
                            "RequestXPathName" text NOT NULL,
                            "NodeKey" text NULL,
                            "GraphRegistryGuid" uuid NULL,
                            "CounterpartName" text NULL,
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
                        CREATE INDEX "IX_EntityAnalysisModelGraphRecall_Model" ON public."EntityAnalysisModelGraphRecall" ("EntityAnalysisModelId", "Deleted");
                        CREATE UNIQUE INDEX "IX_EntityAnalysisModelGraphRecall_Name" ON public."EntityAnalysisModelGraphRecall" ("EntityAnalysisModelId", lower("Name")) WHERE COALESCE("Deleted", 0) = 0;

                        CREATE TABLE public."EntityAnalysisModelGraphRecallVersion" (
                            "Id" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "EntityAnalysisModelGraphRecallId" integer NOT NULL,
                            "EntityAnalysisModelId" integer NOT NULL,
                            "Name" text NOT NULL,
                            "RequestXPathName" text NOT NULL,
                            "NodeKey" text NULL,
                            "GraphRegistryGuid" uuid NULL,
                            "CounterpartName" text NULL,
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
                        CREATE INDEX "IX_EntityAnalysisModelGraphRecallVersion_Parent" ON public."EntityAnalysisModelGraphRecallVersion" ("EntityAnalysisModelGraphRecallId");

                        COMMENT ON TABLE public."EntityAnalysisModelGraphRecall" IS 'An entity a model recalls from the graph at invocation: the payload field whose value names a graph node, read as a typed object under Graph.{Name} in rules (distances to confirmed cases and to tags, hub flag, degrees, community rate, centralities, pattern membership and, against a named counterpart entity, the edge facts). The HTTP Adaptation twin for the network graph.';
                        COMMENT ON COLUMN public."EntityAnalysisModelGraphRecall"."Name" IS 'The entity name used in rules as Graph.{Name}; unique within the model, identifier-safe.';
                        COMMENT ON COLUMN public."EntityAnalysisModelGraphRecall"."RequestXPathName" IS 'The model request XPath (flagged Report Table) whose value identifies the entity.';
                        COMMENT ON COLUMN public."EntityAnalysisModelGraphRecall"."NodeKey" IS 'The graph node key the value is filed under when it differs from the field name, as GraphRegistryEdge.ToNodeKey; null means the field name.';
                        COMMENT ON COLUMN public."EntityAnalysisModelGraphRecall"."GraphRegistryGuid" IS 'The Graph Registry whose edge definitions scope the edge facts against the counterpart; null uses every active definition of the tenant.';
                        COMMENT ON COLUMN public."EntityAnalysisModelGraphRecall"."CounterpartName" IS 'The name of another entity of the same model this one is paired with for edge facts and bridge detection; joins are explicit, never guessed.';
                        COMMENT ON COLUMN public."EntityAnalysisModelGraphRecall"."ResponsePayload" IS 'When 1 the entity object is returned in the response JSON under Graph.';
                        COMMENT ON COLUMN public."EntityAnalysisModelGraphRecall"."ReportTable" IS 'When 1 the entity object is written into the archive JSON for reporting and training.';
                        COMMENT ON TABLE public."EntityAnalysisModelGraphRecallVersion" IS 'Version history of EntityAnalysisModelGraphRecall, one row per saved change.';
                        """);

            Execute.Sql("""
                        DO $demo$
                        DECLARE
                            v_model integer;
                            v_tenant integer;
                            v_registry uuid;
                        BEGIN
                            SELECT "Id", "TenantRegistryId" INTO v_model, v_tenant FROM "EntityAnalysisModel"
                            WHERE "Guid" = '90c425fd-101a-420b-91d1-cb7a24a969cc' AND coalesce("Deleted", 0) = 0;

                            IF v_model IS NULL THEN
                                RETURN;
                            END IF;

                            IF EXISTS (SELECT 1 FROM "EntityAnalysisModelGraphRecall" WHERE "EntityAnalysisModelId" = v_model) THEN
                                RETURN;
                            END IF;

                            SELECT "Guid" INTO v_registry FROM "GraphRegistry"
                            WHERE "Guid" = '6f1a3c2e-9b7d-4e2a-8c3f-2d9e1b7a5c41' AND coalesce("Deleted", 0) = 0;

                            INSERT INTO "EntityAnalysisModelGraphRecall" ("EntityAnalysisModelId", "Name", "RequestXPathName", "NodeKey", "GraphRegistryGuid", "CounterpartName",
                                "ResponsePayload", "ReportTable", "Active", "Locked", "Version", "CreatedDate", "CreatedUser", "Deleted")
                            VALUES
                                (v_model, 'AccountId', 'AccountId', NULL, v_registry, 'Payee', 1, 0, 1, 0, 1, now() at time zone 'utc', 'Administrator', 0),
                                (v_model, 'Payee', 'ToAccountId', 'AccountId', v_registry, 'AccountId', 1, 0, 1, 0, 1, now() at time zone 'utc', 'Administrator', 0),
                                (v_model, 'Device', 'FingerprintHash', NULL, v_registry, 'AccountId', 1, 0, 1, 0, 1, now() at time zone 'utc', 'Administrator', 0),
                                (v_model, 'Network', 'MAC', NULL, v_registry, 'AccountId', 1, 0, 1, 0, 1, now() at time zone 'utc', 'Administrator', 0),
                                (v_model, 'Email', 'Email', NULL, v_registry, 'AccountId', 1, 0, 1, 0, 1, now() at time zone 'utc', 'Administrator', 0),
                                (v_model, 'Card', 'CreditCardHash', NULL, v_registry, 'AccountId', 1, 0, 1, 0, 1, now() at time zone 'utc', 'Administrator', 0);

                            INSERT INTO "EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId", "EntityVersion", "StateId", "CreatedDate", "CreatedUser", "Guid")
                            SELECT v_tenant, 20, r."Id", r."Version", 1, now() at time zone 'utc', 'Administrator', gen_random_uuid()
                            FROM "EntityAnalysisModelGraphRecall" r WHERE r."EntityAnalysisModelId" = v_model;

                            INSERT INTO "EntityAnalysisModelActivationRule" ("EntityAnalysisModelId", "Name", "BuilderRuleScript", "CoderRuleScript", "RuleScriptTypeId", "Json",
                                "ResponseElevation", "EnableCaseWorkflow", "CaseKey", "Active", "Locked", "Version", "Guid", "CreatedUser", "CreatedDate",
                                "EnableTtlCounter", "ResponsePayload", "ReportTable", "EnableNotification", "EnableResponseElevation", "EnableBypass", "Visible", "EnableReprocessing",
                                "ActivationSample", "Deleted")
                            VALUES
                                (v_model, 'GraphAccountNearConfirmedFraud',
                                    E'If (Graph.AccountId.DistanceToConfirmed <= 2) Then\n  Return True\nEnd If', 'Return False', '1',
                                    '{"not": false, "rules": [{"id": "Graph.AccountId.DistanceToConfirmed", "type": "integer", "field": "Graph.AccountId.DistanceToConfirmed", "input": "number", "value": 2, "operator": "less_or_equal"}], "valid": true, "condition": "AND"}',
                                    0, 0, '', 1, 0, 1, gen_random_uuid(), 'Administrator', now() at time zone 'utc', 0, 1, 1, 0, 0, 0, 1, 1, 1, 0),
                                (v_model, 'GraphPayeeIsHubOrInRiskyCommunity', 'Return False',
                                    E'If Graph.Payee.Hub Or Graph.Payee.CommunityConfirmedRate > 0.2 Then\n  Return True\nEnd If', '2', NULL,
                                    0, 0, '', 1, 0, 1, gen_random_uuid(), 'Administrator', now() at time zone 'utc', 0, 1, 1, 0, 0, 0, 1, 1, 1, 0),
                                (v_model, 'GraphFirstPaymentToKnownPayee', 'Return False',
                                    E'If Graph.Payee.Known And Not Graph.Payee.Edge.Exists Then\n  Return True\nEnd If', '2', NULL,
                                    0, 0, '', 1, 0, 1, gen_random_uuid(), 'Administrator', now() at time zone 'utc', 0, 1, 1, 0, 0, 0, 1, 1, 1, 0),
                                (v_model, 'GraphSharedDeviceBridgesToConfirmedFraud', 'Return False',
                                    E'If Graph.Device.BridgesToTagged Or Graph.Device.RelaxedDistanceToConfirmed <= 1 Then\n  Return True\nEnd If', '2', NULL,
                                    0, 0, '', 1, 0, 1, gen_random_uuid(), 'Administrator', now() at time zone 'utc', 0, 1, 1, 0, 0, 0, 1, 1, 1, 0);

                            INSERT INTO "EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId", "EntityVersion", "StateId", "CreatedDate", "CreatedUser", "Guid")
                            SELECT v_tenant, 16, r."Id", r."Version", 1, now() at time zone 'utc', 'Administrator', gen_random_uuid()
                            FROM "EntityAnalysisModelActivationRule" r WHERE r."EntityAnalysisModelId" = v_model AND r."Name" LIKE 'Graph%' AND coalesce(r."Deleted", 0) = 0;

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
