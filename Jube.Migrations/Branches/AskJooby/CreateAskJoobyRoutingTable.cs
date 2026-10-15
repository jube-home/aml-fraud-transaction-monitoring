/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20261002180000)]
    public class CreateAskJoobyRoutingTable : Migration
    {
        public override void Up()
        {
            Create.Table("AskJoobyRouting")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("Task").AsString().Nullable()
                .WithColumn("Stage").AsInt32().Nullable()
                .WithColumn("AgentType").AsString(255).Nullable()
                .WithColumn("Score").AsDouble().Nullable()
                .WithColumn("EmbeddingRequests").AsInt32().Nullable()
                .WithColumn("EmbeddingValues").AsInt32().Nullable()
                .WithColumn("DurationMs").AsInt64().Nullable()
                .WithColumn("Error").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable();

            Create.Index().OnTable("AskJoobyRouting")
                .OnColumn("AskJoobyConversationId").Ascending();

            Alter.Table("AskJoobyRun")
                .AddColumn("AskJoobyRoutingId").AsInt32().Nullable();

            Create.Index().OnTable("AskJoobyRun")
                .OnColumn("AskJoobyRoutingId").Ascending();

            Comment("AskJoobyRouting",
                "One routing decision, which is the parent of the runs it produced: routing is what decides "
                + "which agent a turn is handed to, so it is written before any run exists. Stage says which of "
                + "the router's three tiers actually decided, and the two embedding counts say whether an "
                + "embedding provider was reached for on that call and in how many round trips.");

            Comment("AskJoobyRouting", "Stage",
                "Which tier of the router decided. 1 Pattern, a regular expression matched and nothing was "
                + "called. 2 Embedding, similarity against the routing exemplars settled it. 3 Model, both "
                + "cheaper tiers declined and a model was asked. 4 Default, nothing decided and the generalist "
                + "was used. 5 NoCandidates, the caller's permissions left no routable domain at all.");

            Comment("AskJoobyRouting", "EmbeddingRequests",
                "How many embedding round trips this routing decision made. The cost that matters is the number "
                + "of requests rather than the number of values: one request covering every exemplar and one "
                + "request per exemplar are identical work and nearly a minute apart.");

            Comment("AskJoobyRouting", "EmbeddingValues",
                "How many strings were embedded across those requests, being the live task plus any routing "
                + "exemplar not already cached in this process.");

            Comment("AskJoobyRouting", "Score",
                "The best cosine similarity found against any routing exemplar, where the embedding tier ran. "
                + "Null where it did not. Compared against RoutingRules.EmbeddingConfidenceThreshold to decide "
                + "whether the embedding tier was confident enough to settle it.");

            Comment("AskJoobyRouting", "Error",
                "Why the embedding tier was skipped, where it failed. The tier is an optimisation and its "
                + "failure falls through to the model tier rather than losing the turn, so a value here means "
                + "the turn succeeded but paid for the more expensive tier.");

            Comment("AskJoobyRun", "AskJoobyRoutingId",
                "The routing decision that produced this run. Set only on the top level run of a turn: a "
                + "delegated child reaches its route through ParentAskJoobyRunId, and stamping it on every "
                + "child would say each had been routed, which no child was.");
        }

        private void Comment(string table, string text)
        {
            var quoted = text.Replace("'", "''");

            Execute.Sql($"""
                         DO $$
                         BEGIN
                           IF EXISTS (SELECT 1 FROM information_schema.tables
                                      WHERE table_schema = 'public' AND table_name = '{table}') THEN
                             EXECUTE format('COMMENT ON TABLE public."{table}" IS %L', '{quoted}');
                           END IF;
                         END $$;
                         """);
        }

        private void Comment(string table, string column, string text)
        {
            var quoted = text.Replace("'", "''");

            Execute.Sql($"""
                         DO $$
                         BEGIN
                           IF EXISTS (SELECT 1 FROM information_schema.columns
                                      WHERE table_schema = 'public' AND table_name = '{table}'
                                        AND column_name = '{column}') THEN
                             EXECUTE format('COMMENT ON COLUMN public."{table}"."{column}" IS %L', '{quoted}');
                           END IF;
                         END $$;
                         """);
        }

        public override void Down()
        {
        }
    }
}