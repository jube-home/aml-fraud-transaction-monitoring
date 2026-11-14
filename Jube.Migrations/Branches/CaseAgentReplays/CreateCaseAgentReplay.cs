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

namespace Jube.Migrations.Branches.CaseAgentReplays
{
    using FluentMigrator;

    [Migration(2026111103000000)]
    public class CreateCaseAgentReplay : Migration
    {
        public override void Up()
        {
            Create.Table("CaseAgentReplay")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable().Unique()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("StatusId").AsInt16().NotNullable()
                .WithColumn("PromptText").AsString(4000).Nullable()
                .WithColumn("AgentDefinitionHash").AsString(64).NotNullable()
                .WithColumn("ModelName").AsString(200).Nullable()
                .WithColumn("FromDate").AsDateTime().NotNullable()
                .WithColumn("ToDate").AsDateTime().NotNullable()
                .WithColumn("MaximumCases").AsInt32().NotNullable()
                .WithColumn("AsOfMinutes").AsInt32().NotNullable()
                .WithColumn("CaseWorkflowGuid").AsGuid().Nullable()
                .WithColumn("CasesSelected").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("CasesScored").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Error").AsString(500).Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("StartedDate").AsDateTime().Nullable()
                .WithColumn("CompletedDate").AsDateTime().Nullable();

            Create.Table("CaseAgentReplayResult")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("ReplayGuid").AsGuid().NotNullable()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("HumanOutcomeId").AsInt16().NotNullable()
                .WithColumn("RecommendedOutcomeId").AsInt16().Nullable()
                .WithColumn("AsOfDate").AsDateTime().NotNullable();

            Create.Index("IX_CaseAgentReplayResult_Case").OnTable("CaseAgentReplayResult")
                .OnColumn("ReplayGuid").Ascending().OnColumn("CaseId").Ascending().WithOptions().Unique();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseAgentReplay" IS 'A replay of the triage prompt over closed cases: for each case the model is shown only the facts that existed shortly after the case was created, with no tools, and its recommendation is compared with the outcome people actually reached. It measures a prompt and model, not the live tool-using agent.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."TenantRegistryId" IS 'The tenant whose cases are replayed.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."StatusId" IS 'Enumeration (CaseAgentReplayStatus): 1 Pending, 2 Running, 3 Completed, 4 Failed.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."PromptText" IS 'The prompt under test, or empty for the built-in replay prompt.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."AgentDefinitionHash" IS 'SHA-256 of the prompt used, so replays can be compared by definition.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."ModelName" IS 'The model id the provider reported.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."FromDate" IS 'UTC. Cases closed at or after this instant are replayed.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."ToDate" IS 'UTC. Cases closed before this instant are replayed.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."MaximumCases" IS 'The most cases replayed, newest closed first.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."AsOfMinutes" IS 'How many minutes after a case was created the facts are cut off. Nothing later is shown to the model.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."CaseWorkflowGuid" IS 'Only cases of this workflow, when given.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."CasesSelected" IS 'How many closed cases the replay chose.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."CasesScored" IS 'How many of them the model answered usably.';
                        COMMENT ON COLUMN public."CaseAgentReplay"."Error" IS 'A plain reason a failed replay failed; never database text.';
                        COMMENT ON TABLE public."CaseAgentReplayResult" IS 'The model''s recommendation for one closed case in a replay, beside the outcome people reached.';
                        COMMENT ON COLUMN public."CaseAgentReplayResult"."ReplayGuid" IS 'The replay. Joins CaseAgentReplay on Guid.';
                        COMMENT ON COLUMN public."CaseAgentReplayResult"."CaseId" IS 'The closed case.';
                        COMMENT ON COLUMN public."CaseAgentReplayResult"."HumanOutcomeId" IS 'Enumeration (CaseStatusOutcome) of the status the case closed in: 1 Confirmed, 2 Not confirmed.';
                        COMMENT ON COLUMN public."CaseAgentReplayResult"."RecommendedOutcomeId" IS 'Enumeration (CaseStatusOutcome) the model recommended: 1 Confirmed, 2 Not confirmed, 0 inconclusive; empty when the answer was unusable.';
                        COMMENT ON COLUMN public."CaseAgentReplayResult"."AsOfDate" IS 'UTC. The cut-off of the facts shown to the model.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
