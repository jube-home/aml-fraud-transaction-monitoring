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
    [Migration(20263107660300)]
    public class CreateAskJoobyAgentGuardRuleTable : Migration
    {
        public override void Up()
        {
            Create.Table("AskJoobyAgentGuardRule")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("RuleId").AsByte().Nullable()
                .WithColumn("Enabled").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("GuidanceText").AsString(int.MaxValue).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString(255).Nullable();

            Create.Index("IX_AskJoobyAgentGuardRule_RuleId").OnTable("AskJoobyAgentGuardRule")
                .OnColumn("RuleId").Ascending()
                .WithOptions().Unique();

            CommentTable("AskJoobyAgentGuardRule",
                "One configurable row per AgentGuard rule, read by AskJoobyInjectionGuard.ResolveRule "
                + "to decide whether that rule is enabled (and, for the two topic-boundary rules, what "
                + "topics it should judge against). A rule with no row here is not an unconfigured gap "
                + "-- it means the platform's own shipped default applies, which is enabled with the "
                + "built-in topics for every rule except TopicBoundaryInput (see "
                + "AskJoobyInjectionGuardSettings.ChatInputTopicBoundaryEnabledByDefault, off by default: "
                + "enterprise on-prem deployments reported it rejecting ordinary, on-platform requests "
                + "often enough to be unworkable). A row only needs to exist here when a tenant wants "
                + "something other than that shipped default, so this table is expected to stay empty "
                + "on an otherwise-default deployment.");

            CommentColumn("AskJoobyAgentGuardRule", "RuleId",
                "See Jube.Data.Repository.Models.AskJoobyGuardRule. 1=TopicBoundaryInput (always the "
                + "Input phase, off by platform default), 2=TopicBoundaryOutput (always the Output "
                + "phase, on by platform default), 3=ToolContentScreening (injection/secrets screening "
                + "of retrieved tool content -- never runs the topic-boundary LLM judge, since a "
                + "decontextualised row of the platform's own data has no topic of its own to "
                + "misjudge), 4=ChatInputPromptInjection (the regex and Defender ONNX prompt-injection "
                + "screen over a caller's own typed chat message or attachment; secrets detection and "
                + "the topic boundary are governed separately and are not affected by this row).");

            CommentColumn("AskJoobyAgentGuardRule", "GuidanceText",
                "An operator-supplied replacement for the rule's own default allowed-topics text. Null "
                + "keeps the built-in topics.");
        }

        private void CommentTable(string table, string text)
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

        private void CommentColumn(string table, string column, string text)
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