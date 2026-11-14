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

namespace Jube.Migrations.Branches.CaseAgentRuns
{
    using FluentMigrator;

    [Migration(2026111100000000)]
    public class CreateCaseAgentRun : Migration
    {
        public override void Up()
        {
            Create.Table("CaseAgentRun")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable().Unique()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("RecommendedOutcomeId").AsInt16().NotNullable()
                .WithColumn("Rationale").AsString(4000).NotNullable()
                .WithColumn("Evidence").AsString(8000).Nullable()
                .WithColumn("FactsHash").AsString(64).NotNullable()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("ModelName").AsString(200).Nullable()
                .WithColumn("AgentDefinitionHash").AsString(64).NotNullable()
                .WithColumn("StateId").AsInt16().NotNullable()
                .WithColumn("OverrideReason").AsString(500).Nullable()
                .WithColumn("DecidedUser").AsString().Nullable()
                .WithColumn("DecidedDate").AsDateTime().Nullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Index("IX_CaseAgentRun_Case").OnTable("CaseAgentRun")
                .OnColumn("CaseId").Ascending().OnColumn("Id").Descending();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseAgentRun" IS 'A recommendation made headlessly and read-only by the case triage agent for one open case. The agent only recommends; an analyst accepts it or overrides it with a reason, and nothing here closes a case.';
                        COMMENT ON COLUMN public."CaseAgentRun"."TenantRegistryId" IS 'The tenant the case belongs to.';
                        COMMENT ON COLUMN public."CaseAgentRun"."CaseId" IS 'The case the recommendation is about.';
                        COMMENT ON COLUMN public."CaseAgentRun"."RecommendedOutcomeId" IS 'Enumeration (CaseStatusOutcome) the agent recommends: 1 Confirmed, 2 Not confirmed, 0 inconclusive.';
                        COMMENT ON COLUMN public."CaseAgentRun"."Rationale" IS 'The agent''s reasoning in plain prose.';
                        COMMENT ON COLUMN public."CaseAgentRun"."Evidence" IS 'JSON array of short citations of the facts the agent relied on.';
                        COMMENT ON COLUMN public."CaseAgentRun"."FactsHash" IS 'Hash of the case facts at the time; a new run is only made when the facts change.';
                        COMMENT ON COLUMN public."CaseAgentRun"."AskJoobyConversationId" IS 'The AskJooby conversation that holds the agent''s full working, for audit.';
                        COMMENT ON COLUMN public."CaseAgentRun"."ModelName" IS 'The model id the provider reported for the run, kept as governance evidence.';
                        COMMENT ON COLUMN public."CaseAgentRun"."AgentDefinitionHash" IS 'SHA-256 of the triage prompt the run used, so runs can be compared by definition.';
                        COMMENT ON COLUMN public."CaseAgentRun"."StateId" IS 'Enumeration (CaseAgentRunState): 1 Pending, 2 Accepted, 3 Overridden, 4 Superseded by a newer run.';
                        COMMENT ON COLUMN public."CaseAgentRun"."OverrideReason" IS 'The analyst''s reason for overriding the recommendation.';
                        COMMENT ON COLUMN public."CaseAgentRun"."DecidedUser" IS 'The analyst who accepted or overrode the recommendation.';
                        COMMENT ON COLUMN public."CaseAgentRun"."DecidedDate" IS 'When the recommendation was accepted or overridden.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
