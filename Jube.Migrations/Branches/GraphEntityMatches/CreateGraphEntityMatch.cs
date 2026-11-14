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

namespace Jube.Migrations.Branches.GraphEntityMatches
{
    using FluentMigrator;

    [Migration(2026111104000000)]
    public class CreateGraphEntityMatch : Migration
    {
        public override void Up()
        {
            Create.Table("GraphEntityMatch")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("NodeIdA").AsInt64().NotNullable()
                .WithColumn("NodeIdB").AsInt64().NotNullable()
                .WithColumn("SourceId").AsInt16().NotNullable()
                .WithColumn("Posterior").AsDouble().NotNullable()
                .WithColumn("Evidence").AsCustom("jsonb").Nullable()
                .WithColumn("DecisionId").AsInt16().NotNullable()
                .WithColumn("DecidedUser").AsString().Nullable()
                .WithColumn("DecidedDate").AsDateTime().Nullable()
                .WithColumn("DecisionNote").AsString(500).Nullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("ResolvedEntityId").AsInt64().Nullable();

            Create.Index("IX_GraphEntityMatch_Pair").OnTable("GraphEntityMatch")
                .OnColumn("TenantRegistryId").Ascending().OnColumn("NodeIdA").Ascending()
                .OnColumn("NodeIdB").Ascending().WithOptions().Unique();

            Create.Index("IX_GraphEntityMatch_Queue").OnTable("GraphEntityMatch")
                .OnColumn("TenantRegistryId").Ascending().OnColumn("DecisionId").Ascending()
                .OnColumn("Posterior").Descending();

            Execute.Sql("""
                        COMMENT ON TABLE public."GraphEntityMatch" IS 'A candidate that two graph nodes are the same real-world entity, with the evidence for it. An analyst confirms or rejects it; confirmed matches are merged into one resolved entity by setting GraphNode.ResolvedEntityId, and undoing a decision takes the merge away again. Nothing is ever merged without a confirmation unless auto-confirm of hard identifiers is switched on.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."TenantRegistryId" IS 'The tenant both nodes belong to.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."NodeIdA" IS 'The lower of the two node ids. Joins GraphNode on Id.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."NodeIdB" IS 'The higher of the two node ids. Joins GraphNode on Id.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."SourceId" IS 'Enumeration (GraphEntityMatchSource): 1 shared hard identifier, 2 fuzzy comparison of values.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."Posterior" IS 'Estimated probability the nodes are the same entity, from the Fellegi-Sunter weights; a shared hard identifier is near certain. Evidence, not a decision.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."Evidence" IS 'JSON: what agreed and how much it weighed, for example the shared identifier and how many records share it, or the folded values, the similarity and the weights.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."DecisionId" IS 'Enumeration (GraphEntityMatchDecision): 1 Pending, 2 Confirmed, 3 Rejected.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."DecidedUser" IS 'Who confirmed or rejected it, or System when auto-confirmed.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."DecidedDate" IS 'UTC. When it was decided.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."DecisionNote" IS 'Optional reason given with the decision.';
                        COMMENT ON COLUMN public."GraphEntityMatch"."ResolvedEntityId" IS 'For a confirmed match, the id of the resolved entity both nodes now belong to (the lowest node id in the merged group); empty otherwise.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
