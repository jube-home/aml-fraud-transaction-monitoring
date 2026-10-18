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
    [Migration(20263107710000)]
    public class CreateAskJoobyAgentGuardRejectionTable : Migration
    {
        public override void Up()
        {
            Create.Table("AskJoobyAgentGuardRejection")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("PhaseId").AsByte().Nullable()
                .WithColumn("PipelineName").AsString(255).Nullable()
                .WithColumn("RuleName").AsString(255).Nullable()
                .WithColumn("Reason").AsString().Nullable()
                .WithColumn("ActionId").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable();

            Create.Index().OnTable("AskJoobyAgentGuardRejection")
                .OnColumn("AskJoobyConversationId").Ascending();

            Comment("AskJoobyAgentGuardRejection",
                "One AgentGuard rule firing against this conversation, recorded whether the rule blocked the "
                + "turn outright or only redacted part of it: the chat widget and the thrown exception already "
                + "say so to whoever is in the conversation at the time, but neither persists anywhere, so a "
                + "deployment asking how often its guardrails actually fire, and on what, had no record to ask "
                + "it of. Deliberately holds no raw rejected text -- AgentGuard's own decision ledger is "
                + "hash-only by default for exactly this reason, and a bank platform follows that default "
                + "rather than inventing a reason to store what got blocked verbatim.");

            Comment("AskJoobyAgentGuardRejection", "PhaseId",
                "GuardrailContext.Phase at the moment the rule ran: Input for a rule screening what the caller "
                + "sent before any agent saw it, Output for one screening what an agent tried to persist, call, "
                + "or receive back.");

            Comment("AskJoobyAgentGuardRejection", "PipelineName",
                "The GuardrailPolicyBuilder name given at the call site that ran this rule -- "
                + "askjooby-input-injection, askjooby-memory-sensitive-data, askjooby-tool-call-guard, or "
                + "askjooby-tool-result-guard -- which says which of AskJooby's four guard chokepoints this "
                + "row came from.");

            Comment("AskJoobyAgentGuardRejection", "RuleName",
                "Which individual rule inside the pipeline actually fired, e.g. prompt-injection or "
                + "secrets-detection -- a pipeline runs several rules in order and only the one that matched "
                + "is named here.");

            Comment("AskJoobyAgentGuardRejection", "ActionId",
                "Blocked when the rule stopped the turn outright, Modified when it only redacted part of the "
                + "content and let the rest through -- AskJoobySensitiveDataGuard's PII rule is the only rule "
                + "wired up today that ever produces Modified rather than Blocked.");
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