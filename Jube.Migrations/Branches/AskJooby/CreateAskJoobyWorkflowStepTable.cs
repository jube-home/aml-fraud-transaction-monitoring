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
    [Migration(20261003140000)]
    public class CreateAskJoobyWorkflowStepTable : Migration
    {
        public override void Up()
        {
            Create.Table("AskJoobyWorkflowStep")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("WorkflowId").AsString(255).Nullable()
                .WithColumn("NodeId").AsString(255).Nullable()
                .WithColumn("Completed").AsInt16().Nullable()
                .WithColumn("Note").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable();

            Create.Index().OnTable("AskJoobyWorkflowStep")
                .OnColumn("AskJoobyConversationId").Ascending()
                .OnColumn("WorkflowId").Ascending();

            Comment("AskJoobyWorkflowStep",
                "An agent's own statement about where a graph-directed workflow has got to, scoped to the "
                + "conversation rather than to a run, because a workflow outlives the turn that began it: a "
                + "build stopped at step four by a spent budget has to restart at step five, and a run ends "
                + "with the turn. Append only -- a correction is a later row, not an edit -- so the sequence "
                + "of what an agent believed, and when, survives for an audit.");

            Comment("AskJoobyWorkflowStep", "WorkflowId",
                "The graph this step belongs to, as GraphWorkflow.Id -- for example velocity-and-volume. Not a "
                + "foreign key: the workflows are compiled into the binary rather than stored, so the id is "
                + "resolved against RuleTaxonomyWorkflows and PlatformOpsWorkflows at read time and a row "
                + "naming a workflow that no longer exists is reported as such rather than silently dropped.");

            Comment("AskJoobyWorkflowStep", "NodeId",
                "The step within that graph, as GraphWorkflowNode.Id. Resolved the same way as WorkflowId and "
                + "with the same consequence when a graph is re-cut and a node is renamed: the row stays, and "
                + "says plainly that it refers to a step that is no longer in the graph.");

            Comment("AskJoobyWorkflowStep", "Completed",
                "1 where the agent states the step is done, 0 where it states it is not. The zero case is the "
                + "reason this column exists rather than the row's presence being the signal: completion is "
                + "also inferred, by matching the node's catalogued operations against the tool calls the "
                + "conversation actually made, and an agent must be able to overrule a wrong inference. A "
                + "stated value always wins over an inferred one, in either direction.");

            Comment("AskJoobyWorkflowStep", "Note",
                "Why the agent said what it said, in its own words. Carried because the interesting rows are "
                + "the corrections -- a step the tool calls suggest was done and the agent says was not -- and "
                + "a correction with no reason on it is indistinguishable from a mistake.");
        }

        public override void Down()
        {
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
    }
}