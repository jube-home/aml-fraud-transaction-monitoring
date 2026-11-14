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

namespace Jube.Migrations.Branches.CaseQaReviews
{
    using FluentMigrator;

    [Migration(2026111102000000)]
    public class CreateCaseQaReview : Migration
    {
        public override void Up()
        {
            Create.Table("CaseQaReview")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable().Unique()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("ReviewerUser").AsString().NotNullable()
                .WithColumn("AnalystVerdictId").AsInt16().NotNullable()
                .WithColumn("AgentVerdictId").AsInt16().Nullable()
                .WithColumn("CaseAgentRunGuid").AsGuid().Nullable()
                .WithColumn("Comment").AsString(1000).Nullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Index("IX_CaseQaReview_Case").OnTable("CaseQaReview")
                .OnColumn("CaseId").Ascending().OnColumn("Id").Descending();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseQaReview" IS 'A quality review of a closed case that was picked for sampling: a second person judges how the case was handled and, when the triage agent made a recommendation, how good that recommendation was. Append only; the latest review of a case is current.';
                        COMMENT ON COLUMN public."CaseQaReview"."TenantRegistryId" IS 'The tenant the case belongs to.';
                        COMMENT ON COLUMN public."CaseQaReview"."CaseId" IS 'The case reviewed.';
                        COMMENT ON COLUMN public."CaseQaReview"."ReviewerUser" IS 'The reviewer, who is never the user who closed the case.';
                        COMMENT ON COLUMN public."CaseQaReview"."AnalystVerdictId" IS 'Enumeration (CaseQaVerdict): how the analyst handled the case. 1 Pass, 2 Fail, 3 Not applicable.';
                        COMMENT ON COLUMN public."CaseQaReview"."AgentVerdictId" IS 'Enumeration (CaseQaVerdict): how good the triage agent recommendation was. Empty when there was none.';
                        COMMENT ON COLUMN public."CaseQaReview"."CaseAgentRunGuid" IS 'The triage recommendation judged, when AgentVerdictId is given. Joins CaseAgentRun on Guid.';
                        COMMENT ON COLUMN public."CaseQaReview"."Comment" IS 'Optional free text from the reviewer.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
