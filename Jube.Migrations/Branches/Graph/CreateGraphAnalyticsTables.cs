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

    [Migration(20263108380000)]
    public class CreateGraphAnalyticsTables : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE TABLE public."GraphCommunity" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "TenantRegistryId" integer NOT NULL,
                            "Size" integer NOT NULL,
                            "Density" double precision NOT NULL,
                            "ConfirmedCount" integer NOT NULL DEFAULT 0,
                            "ClosedCount" integer NOT NULL DEFAULT 0,
                            "ConfirmedRate" double precision NULL,
                            "TenantRateRatio" double precision NULL,
                            "Label" text NULL,
                            "ComputedDate" timestamp without time zone NOT NULL,
                            "Deleted" smallint NOT NULL DEFAULT 0
                        );
                        CREATE INDEX "IX_GraphCommunity_Tenant" ON public."GraphCommunity" ("TenantRegistryId", "Deleted");

                        CREATE TABLE public."GraphPattern" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "TenantRegistryId" integer NOT NULL,
                            "KindId" smallint NOT NULL,
                            "AnchorNodeId" bigint NOT NULL,
                            "Score" double precision NOT NULL,
                            "WindowFrom" timestamp without time zone NOT NULL,
                            "WindowTo" timestamp without time zone NOT NULL,
                            "Parameters" jsonb NULL,
                            "ArchiveIds" bigint[] NULL,
                            "MemberCount" integer NOT NULL DEFAULT 0,
                            "CaseId" integer NULL,
                            "StateId" smallint NOT NULL DEFAULT 1,
                            "FirstDetectedDate" timestamp without time zone NOT NULL,
                            "LastDetectedDate" timestamp without time zone NOT NULL,
                            "DecidedUser" text NULL,
                            "DecidedDate" timestamp without time zone NULL
                        );
                        CREATE UNIQUE INDEX "IX_GraphPattern_Identity" ON public."GraphPattern" ("TenantRegistryId", "KindId", "AnchorNodeId", "WindowFrom");
                        CREATE INDEX "IX_GraphPattern_Tenant_State" ON public."GraphPattern" ("TenantRegistryId", "StateId", "LastDetectedDate" DESC);

                        CREATE TABLE public."GraphPatternNode" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "GraphPatternId" bigint NOT NULL REFERENCES public."GraphPattern"("Id"),
                            "GraphNodeId" bigint NOT NULL,
                            "Role" text NOT NULL
                        );
                        CREATE INDEX "IX_GraphPatternNode_Pattern" ON public."GraphPatternNode" ("GraphPatternId");
                        CREATE INDEX "IX_GraphPatternNode_Node" ON public."GraphPatternNode" ("GraphNodeId");

                        COMMENT ON TABLE public."GraphCommunity" IS 'A community found by the nightly community detection over the hub-pruned tenant graph, with its size, density and the confirmed-case rate of its members. GraphNode.CommunityId points here. Rows not produced by the latest run are marked deleted.';
                        COMMENT ON COLUMN public."GraphCommunity"."Size" IS 'Nodes in the community.';
                        COMMENT ON COLUMN public."GraphCommunity"."Density" IS 'Edges inside the community divided by the possible edges.';
                        COMMENT ON COLUMN public."GraphCommunity"."ConfirmedCount" IS 'Members whose case key closed as Confirmed.';
                        COMMENT ON COLUMN public."GraphCommunity"."ClosedCount" IS 'Members whose case key closed with any outcome.';
                        COMMENT ON COLUMN public."GraphCommunity"."ConfirmedRate" IS 'ConfirmedCount over members, null when no member has a closed case.';
                        COMMENT ON COLUMN public."GraphCommunity"."TenantRateRatio" IS 'ConfirmedRate over the tenant-wide confirmed rate; above 1 means riskier than typical.';
                        COMMENT ON COLUMN public."GraphCommunity"."Label" IS 'The dominant node types of the community, for the panel.';
                        COMMENT ON TABLE public."GraphPattern" IS 'A motif instance found by the pattern detectors over the money-flow events (fan-in, fan-out, gather-scatter, scatter-gather, temporal cycle, pass-through, smurfing, structuring window, dense block), with its members, parameters, score and the archive ids that are its evidence.';
                        COMMENT ON COLUMN public."GraphPattern"."KindId" IS 'As GraphPatternKind: 1 FanOut, 2 FanIn, 3 GatherScatter, 4 ScatterGather, 5 TemporalCycle, 6 PassThrough, 7 SmurfTypeOne, 8 SmurfTypeTwo, 9 StructuringWindow, 10 DenseBlock.';
                        COMMENT ON COLUMN public."GraphPattern"."AnchorNodeId" IS 'The node the instance is anchored on, matching GraphNode.Id.';
                        COMMENT ON COLUMN public."GraphPattern"."Score" IS 'Detector score; its meaning depends on the kind and is stated in Parameters.';
                        COMMENT ON COLUMN public."GraphPattern"."Parameters" IS 'Detector parameters and measured values as JSON.';
                        COMMENT ON COLUMN public."GraphPattern"."ArchiveIds" IS 'Archive ids of the transactions that form the instance.';
                        COMMENT ON COLUMN public."GraphPattern"."StateId" IS 'As GraphPatternState: 1 New, 2 Seen, 3 Dismissed, 4 Cased.';
                        COMMENT ON COLUMN public."GraphPattern"."CaseId" IS 'The case opened from the pattern, when one was.';
                        COMMENT ON TABLE public."GraphPatternNode" IS 'The members of a GraphPattern with their role (source, intermediary, sink, member).';
                        """);
        }

        public override void Down()
        {
        }
    }
}
