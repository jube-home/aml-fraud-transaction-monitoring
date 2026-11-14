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

    [Migration(20263108340000)]
    public class CreateGraphDerivedTables : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        CREATE TABLE public."GraphNode" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "TenantRegistryId" integer NOT NULL,
                            "Key" text NOT NULL,
                            "Value" text NOT NULL,
                            "Hub" smallint NOT NULL DEFAULT 0,
                            "HubOverrideId" smallint NOT NULL DEFAULT 0,
                            "HubByEdgeGuids" uuid[] NULL,
                            "Degree" integer NOT NULL DEFAULT 0,
                            "DegreeMoney" integer NOT NULL DEFAULT 0,
                            "DegreeIdentity" integer NOT NULL DEFAULT 0,
                            "HubNeighbourCount" integer NOT NULL DEFAULT 0,
                            "ResolvedEntityId" bigint NULL,
                            "ComponentId" bigint NULL,
                            "ComponentSize" integer NULL,
                            "CommunityId" bigint NULL,
                            "CoreNumber" integer NULL,
                            "PageRank" double precision NULL,
                            "HubScore" double precision NULL,
                            "AuthorityScore" double precision NULL,
                            "ClusteringCoefficient" double precision NULL,
                            "DistanceToConfirmed" smallint NULL,
                            "ConfirmedWithin1" integer NULL,
                            "ConfirmedWithin2" integer NULL,
                            "ConfirmedWithin3" integer NULL,
                            "DistanceToNotConfirmed" smallint NULL,
                            "PropagationScore" double precision NULL,
                            "Statistics" jsonb NULL,
                            "FirstSeen" timestamp without time zone NOT NULL,
                            "LastSeen" timestamp without time zone NOT NULL,
                            "ComputedDate" timestamp without time zone NULL
                        );
                        CREATE UNIQUE INDEX "IX_GraphNode_Tenant_Key_Value" ON public."GraphNode" ("TenantRegistryId", "Key", "Value");
                        CREATE INDEX "IX_GraphNode_Tenant_Community" ON public."GraphNode" ("TenantRegistryId", "CommunityId") WHERE "CommunityId" IS NOT NULL;
                        CREATE INDEX "IX_GraphNode_Tenant_Component" ON public."GraphNode" ("TenantRegistryId", "ComponentId") WHERE "ComponentId" IS NOT NULL;
                        CREATE INDEX "IX_GraphNode_Tenant_Hub" ON public."GraphNode" ("TenantRegistryId", "Key") WHERE "Hub" = 1;

                        CREATE TABLE public."GraphEdge" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "TenantRegistryId" integer NOT NULL,
                            "GraphRegistryEdgeGuid" uuid NOT NULL,
                            "KindId" smallint NOT NULL,
                            "FromNodeId" bigint NOT NULL REFERENCES public."GraphNode"("Id"),
                            "ToNodeId" bigint NOT NULL REFERENCES public."GraphNode"("Id"),
                            "Count" integer NOT NULL,
                            "AmountSum" double precision NOT NULL DEFAULT 0,
                            "FirstSeen" timestamp without time zone NOT NULL,
                            "LastSeen" timestamp without time zone NOT NULL,
                            "DecayedCount" double precision NOT NULL,
                            "DecayedAmount" double precision NOT NULL,
                            "DecayedAt" timestamp without time zone NOT NULL,
                            "Specificity" real NOT NULL DEFAULT 1,
                            "Confidence" real NOT NULL DEFAULT 1,
                            "Weight" real NOT NULL,
                            "FromHub" smallint NOT NULL DEFAULT 0,
                            "ToHub" smallint NOT NULL DEFAULT 0,
                            "Restricted" smallint NOT NULL DEFAULT 0
                        );
                        CREATE UNIQUE INDEX "IX_GraphEdge_Def_From_To" ON public."GraphEdge" ("TenantRegistryId", "GraphRegistryEdgeGuid", "FromNodeId", "ToNodeId");
                        CREATE INDEX "IX_GraphEdge_From" ON public."GraphEdge" ("TenantRegistryId", "FromNodeId", "KindId", "LastSeen")
                            INCLUDE ("ToNodeId", "Weight", "Count", "AmountSum", "ToHub", "GraphRegistryEdgeGuid", "Restricted");
                        CREATE INDEX "IX_GraphEdge_To" ON public."GraphEdge" ("TenantRegistryId", "ToNodeId", "KindId", "LastSeen")
                            INCLUDE ("FromNodeId", "Weight", "Count", "AmountSum", "FromHub", "GraphRegistryEdgeGuid", "Restricted");
                        CREATE INDEX "IX_GraphEdge_From_NoHub" ON public."GraphEdge" ("TenantRegistryId", "FromNodeId", "Weight" DESC)
                            INCLUDE ("ToNodeId", "KindId", "LastSeen") WHERE "ToHub" = 0;
                        CREATE INDEX "IX_GraphEdge_To_NoHub" ON public."GraphEdge" ("TenantRegistryId", "ToNodeId", "Weight" DESC)
                            INCLUDE ("FromNodeId", "KindId", "LastSeen") WHERE "FromHub" = 0;

                        CREATE TABLE public."GraphEdgeEvent" (
                            "Id" bigint GENERATED ALWAYS AS IDENTITY,
                            "TenantRegistryId" integer NOT NULL,
                            "GraphEdgeId" bigint NOT NULL,
                            "ArchiveId" bigint NOT NULL,
                            "EntityAnalysisModelInstanceEntryGuid" uuid NOT NULL,
                            "ArchiveKeyVersion" integer NULL,
                            "OccurredDate" timestamp without time zone NOT NULL,
                            "Amount" double precision NULL,
                            "Retracted" smallint NOT NULL DEFAULT 0,
                            PRIMARY KEY ("OccurredDate", "Id")
                        ) PARTITION BY RANGE ("OccurredDate");
                        CREATE INDEX "IX_GraphEdgeEvent_Edge_Time" ON public."GraphEdgeEvent" ("GraphEdgeId", "OccurredDate") INCLUDE ("Amount", "ArchiveId", "Retracted");
                        CREATE INDEX "IX_GraphEdgeEvent_Entry" ON public."GraphEdgeEvent" ("EntityAnalysisModelInstanceEntryGuid");
                        CREATE INDEX "IX_GraphEdgeEvent_Time" ON public."GraphEdgeEvent" USING brin ("OccurredDate");
                        CREATE TABLE public."GraphEdgeEventDefault" PARTITION OF public."GraphEdgeEvent" DEFAULT;
                        DO $partitions$
                        DECLARE
                            month_start date := date_trunc('month', now())::date - interval '13 months';
                            month_end date;
                            partition_name text;
                        BEGIN
                            WHILE month_start <= date_trunc('month', now())::date + interval '3 months' LOOP
                                month_end := (month_start + interval '1 month')::date;
                                partition_name := 'GraphEdgeEvent_' || to_char(month_start, 'YYYYMM');
                                EXECUTE format('CREATE TABLE IF NOT EXISTS public.%I PARTITION OF public."GraphEdgeEvent" FOR VALUES FROM (%L) TO (%L)', partition_name, month_start, month_end);
                                month_start := month_end;
                            END LOOP;
                        END
                        $partitions$;

                        CREATE TABLE public."GraphBuildState" (
                            "Id" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                            "TenantRegistryId" integer NOT NULL,
                            "GraphRegistryEdgeGuid" uuid NOT NULL,
                            "LastArchiveKeyId" bigint NOT NULL DEFAULT 0,
                            "Nodes" bigint NOT NULL DEFAULT 0,
                            "Edges" bigint NOT NULL DEFAULT 0,
                            "Events" bigint NOT NULL DEFAULT 0,
                            "LastRunDate" timestamp without time zone NULL,
                            "LastBatchMilliseconds" integer NULL,
                            "StateId" smallint NOT NULL DEFAULT 1,
                            "Error" text NULL
                        );
                        CREATE UNIQUE INDEX "IX_GraphBuildState_Def" ON public."GraphBuildState" ("TenantRegistryId", "GraphRegistryEdgeGuid");

                        DO $archivekey$
                        BEGIN
                            IF (SELECT reltuples FROM pg_class WHERE oid = 'public."ArchiveKey"'::regclass) < 1000000 THEN
                                CREATE INDEX IF NOT EXISTS "IX_ArchiveKey_EntryGuid" ON public."ArchiveKey" ("EntityAnalysisModelInstanceEntryGuid")
                                    INCLUDE ("Key", "KeyValueString", "KeyValueInteger", "KeyValueFloat", "KeyValueLong", "Version");
                            END IF;
                        END
                        $archivekey$;

                        COMMENT ON TABLE public."GraphNode" IS 'A node of the tenant-wide graph: one row per (report key name, value) seen on any archived transaction that an active GraphRegistryEdge pairs. Carries the cached graph features the nightly jobs compute (degree, hubs, components, communities, centralities, distance to confirmed true positives, propagation score). Derived; never edited by hand.';
                        COMMENT ON COLUMN public."GraphNode"."Key" IS 'The ArchiveKey.Key name, which is also the node type (AccountId, DeviceId, Ip).';
                        COMMENT ON COLUMN public."GraphNode"."Value" IS 'The key value as text, normalised from whichever typed ArchiveKey column was populated.';
                        COMMENT ON COLUMN public."GraphNode"."Hub" IS '1 when the node is above its edge definition''s hub threshold (a public IP, a payroll account): traversal does not pass through it unless asked.';
                        COMMENT ON COLUMN public."GraphNode"."HubOverrideId" IS 'Tenant override of hub detection, as GraphHubOverride: 0 none, 1 never a hub, 2 always a hub.';
                        COMMENT ON COLUMN public."GraphNode"."HubByEdgeGuids" IS 'The GraphRegistryEdge guids under which the node was found to be a hub.';
                        COMMENT ON COLUMN public."GraphNode"."Degree" IS 'Distinct neighbours over all edge kinds.';
                        COMMENT ON COLUMN public."GraphNode"."DegreeMoney" IS 'Distinct neighbours over money-flow edges.';
                        COMMENT ON COLUMN public."GraphNode"."DegreeIdentity" IS 'Distinct neighbours over identity-link edges.';
                        COMMENT ON COLUMN public."GraphNode"."HubNeighbourCount" IS 'How many of the node''s neighbours are hubs, kept because sitting behind a shared IP is itself a fact.';
                        COMMENT ON COLUMN public."GraphNode"."ResolvedEntityId" IS 'The GraphNode this node resolves to after entity resolution; null when not merged.';
                        COMMENT ON COLUMN public."GraphNode"."ComponentId" IS 'Connected component the node belongs to (the lowest node id in it), from the nightly union-find.';
                        COMMENT ON COLUMN public."GraphNode"."ComponentSize" IS 'Size of that component.';
                        COMMENT ON COLUMN public."GraphNode"."CommunityId" IS 'The GraphCommunity the node belongs to, from the nightly community detection.';
                        COMMENT ON COLUMN public."GraphNode"."CoreNumber" IS 'k-core number: the densest core the node survives in.';
                        COMMENT ON COLUMN public."GraphNode"."PageRank" IS 'PageRank on the hub-pruned tenant graph.';
                        COMMENT ON COLUMN public."GraphNode"."HubScore" IS 'HITS hub score.';
                        COMMENT ON COLUMN public."GraphNode"."AuthorityScore" IS 'HITS authority score.';
                        COMMENT ON COLUMN public."GraphNode"."ClusteringCoefficient" IS 'Local clustering coefficient: how many of the node''s neighbours are themselves connected.';
                        COMMENT ON COLUMN public."GraphNode"."DistanceToConfirmed" IS 'Hop distance to the nearest case key whose case closed as Confirmed, capped at 4; null when none within the cap.';
                        COMMENT ON COLUMN public."GraphNode"."ConfirmedWithin1" IS 'Distinct confirmed case keys within one hop.';
                        COMMENT ON COLUMN public."GraphNode"."ConfirmedWithin2" IS 'Distinct confirmed case keys within two hops.';
                        COMMENT ON COLUMN public."GraphNode"."ConfirmedWithin3" IS 'Distinct confirmed case keys within three hops.';
                        COMMENT ON COLUMN public."GraphNode"."DistanceToNotConfirmed" IS 'Hop distance to the nearest case key whose case closed as Not confirmed, the negative evidence.';
                        COMMENT ON COLUMN public."GraphNode"."PropagationScore" IS 'Seed propagation score from confirmed and not-confirmed seeds; a score, not a probability.';
                        COMMENT ON COLUMN public."GraphNode"."Statistics" IS 'Vertex statistics as JSON: sum, mean, min, max, median, variance, skew and kurtosis of inbound and outbound amounts and inter-arrival times, round-amount share, pass-through ratio and median hold.';
                        COMMENT ON COLUMN public."GraphNode"."FirstSeen" IS 'Earliest archived transaction the node appeared on (UTC).';
                        COMMENT ON COLUMN public."GraphNode"."LastSeen" IS 'Latest archived transaction the node appeared on (UTC).';
                        COMMENT ON COLUMN public."GraphNode"."ComputedDate" IS 'When the cached features were last computed (UTC); null until the first nightly run.';
                        COMMENT ON TABLE public."GraphEdge" IS 'One aggregate edge per edge definition, from node and to node: the count, amounts, first and last seen and the recency-decayed weight terms, maintained incrementally by the edge build. See docs/Frontend/GraphNetworkAnalysisDesign.md section 4.3 for the weight.';
                        COMMENT ON COLUMN public."GraphEdge"."GraphRegistryEdgeGuid" IS 'The GraphRegistryEdge definition that derived this edge.';
                        COMMENT ON COLUMN public."GraphEdge"."KindId" IS 'Edge kind copied from the definition, as GraphEdgeKind.';
                        COMMENT ON COLUMN public."GraphEdge"."Count" IS 'Archived transactions behind the edge.';
                        COMMENT ON COLUMN public."GraphEdge"."AmountSum" IS 'Sum of the amount key over those transactions, for money flow.';
                        COMMENT ON COLUMN public."GraphEdge"."DecayedCount" IS 'Sum over events of 2 to the power of minus (DecayedAt minus event time in days) over the half-life, as of DecayedAt.';
                        COMMENT ON COLUMN public."GraphEdge"."DecayedAmount" IS 'The same decayed sum of ln(1 + amount / reference amount), as of DecayedAt.';
                        COMMENT ON COLUMN public."GraphEdge"."DecayedAt" IS 'The instant the decayed terms are stated at; readers bring them to now with one multiplication.';
                        COMMENT ON COLUMN public."GraphEdge"."Specificity" IS '1 / ln(2 + k) for an identity edge through an identifier shared by k entities; 1 for money flow.';
                        COMMENT ON COLUMN public."GraphEdge"."Confidence" IS '1 for exact identifiers; the match posterior for resolution edges.';
                        COMMENT ON COLUMN public."GraphEdge"."Weight" IS 'KindPrior times Confidence times Specificity times (1 + ln(1 + DecayedCount)) times (1 + DecayedAmount).';
                        COMMENT ON COLUMN public."GraphEdge"."FromHub" IS 'Denormalised GraphNode.Hub of the from node, so traversal can exclude hubs from the index alone.';
                        COMMENT ON COLUMN public."GraphEdge"."ToHub" IS 'Denormalised GraphNode.Hub of the to node.';
                        COMMENT ON COLUMN public."GraphEdge"."Restricted" IS 'Copied from the definition: 1 when visible only to roles granted on the registry.';
                        COMMENT ON TABLE public."GraphEdgeEvent" IS 'One row per archived transaction that created or reinforced a GraphEdge: the evidence behind every edge, the source of flow tracing and playback, and the link from the picture to the Journal. Partitioned by month on OccurredDate.';
                        COMMENT ON COLUMN public."GraphEdgeEvent"."ArchiveId" IS 'The Archive row the event comes from.';
                        COMMENT ON COLUMN public."GraphEdgeEvent"."EntityAnalysisModelInstanceEntryGuid" IS 'The transaction entry guid, shared with Archive and ArchiveKey.';
                        COMMENT ON COLUMN public."GraphEdgeEvent"."ArchiveKeyVersion" IS 'ArchiveKey.Version the event was built from, so reprocessing can retract it.';
                        COMMENT ON COLUMN public."GraphEdgeEvent"."OccurredDate" IS 'Archive.ReferenceDate when present, else CreatedDate (UTC).';
                        COMMENT ON COLUMN public."GraphEdgeEvent"."Amount" IS 'The amount key value for money flow; null otherwise.';
                        COMMENT ON COLUMN public."GraphEdgeEvent"."Retracted" IS '1 when reprocessing replaced the transaction''s keys and this event no longer counts.';
                        COMMENT ON TABLE public."GraphBuildState" IS 'Watermark and counters of the edge build, one row per edge definition per tenant.';
                        COMMENT ON COLUMN public."GraphBuildState"."LastArchiveKeyId" IS 'The highest ArchiveKey.Id fully processed for this definition.';
                        COMMENT ON COLUMN public."GraphBuildState"."StateId" IS 'As GraphBuildStateKind: 1 Idle, 2 Running, 3 Failed, 4 WaitingForIndex.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
