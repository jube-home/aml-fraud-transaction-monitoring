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

namespace Jube.Migrations.Branches.CasesPage
{
    using FluentMigrator;

    [Migration(20263107860000)]
    public class CreateCaseRiskAssessmentTable : Migration
    {
        public override void Up()
        {
            Create.Table("CaseRiskAssessment")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("Sequence").AsInt32().NotNullable()
                .WithColumn("CreatedDate").AsDateTime2().NotNullable()
                .WithColumn("TriggerId").AsByte().NotNullable()
                .WithColumn("MethodId").AsByte().NotNullable()
                .WithColumn("MethodVersion").AsInt32().Nullable()
                .WithColumn("BaseRate").AsDouble().Nullable()
                .WithColumn("LikelihoodProbability").AsDouble().NotNullable()
                .WithColumn("LikelihoodBandId").AsByte().NotNullable()
                .WithColumn("EvidenceCount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("ContributionsJson").AsCustom("jsonb").Nullable()
                .WithColumn("ImpactId").AsByte().NotNullable()
                .WithColumn("ImpactComponentsJson").AsCustom("jsonb").Nullable()
                .WithColumn("LaneId").AsByte().NotNullable()
                .WithColumn("LaneReason").AsString().Nullable()
                .WithColumn("NextDeadlineDate").AsDateTime2().Nullable()
                .WithColumn("DeadlineBreached").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("ExpectedHarm").AsDouble().NotNullable()
                .WithColumn("Priority").AsInt32().NotNullable();

            Create.ForeignKey().FromTable("CaseRiskAssessment").ForeignColumn("CaseId")
                .ToTable("Case").PrimaryColumn("Id").OnDelete(System.Data.Rule.Cascade);

            Execute.Sql("""
                        CREATE UNIQUE INDEX "IX_CaseRiskAssessment_CaseId_Sequence_Desc"
                        ON public."CaseRiskAssessment" ("CaseId", "Sequence" DESC);
                        CREATE INDEX "IX_CaseRiskAssessment_CreatedDate" ON public."CaseRiskAssessment" ("CreatedDate");
                        """);

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseRiskAssessment" IS
                        'An immutable record of one computed risk assessment of a Case: how likely it is to be confirmed a true positive, how bad it would be if real, how urgent it is, and the Priority those three produce. A new row is written each time the risk is recomputed and no row is ever changed, so the latest row is what the Cases page shows and the earlier rows are the case risk history. Likelihood, impact and urgency are stored separately and Priority is never meaningful without them.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."Id" IS 'Server-assigned identity of the assessment.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."CaseId" IS 'The Case assessed; the assessments are removed with the case.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."Sequence" IS 'One-based position of this assessment in the case''s history. The highest sequence for a case is its current assessment.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."CreatedDate" IS 'When the assessment was computed (UTC).';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."TriggerId" IS 'What caused the recompute. 1 Initial, 2 Activation attached, 3 Linked key changed, 4 Neighbour tagged, 5 Diary or lane threshold, 6 Payment hold, 7 Finding recorded, 8 Typology changed, 9 Risk model changed, 10 Scheduled sweep.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."MethodId" IS 'How the likelihood was produced. 1 Cold start (calibrated evidence combination, not yet verified against outcomes), 2 Learned (a trained and promoted combiner).';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."MethodVersion" IS 'Version of the learned combiner that produced the likelihood; null for cold start.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."BaseRate" IS 'Share of closed cases in this Case Workflow confirmed true positive, the starting point the evidence moved the likelihood away from; null when the workflow has no closed cases yet.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."LikelihoodProbability" IS 'Probability, between 0 and 1, that the case will be confirmed a true positive.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."LikelihoodBandId" IS 'Band of the likelihood. 1 Unlikely (below 25 percent), 2 Possible (25 to below 50), 3 Likely (50 to below 75), 4 Very likely (75 percent or more).';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."EvidenceCount" IS 'How many comparable closed cases stand behind the likelihood; zero means the figure is a cold-start estimate that has not been checked against outcomes.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."ContributionsJson" IS 'Every piece of evidence that moved the likelihood, as a list with family, label, source, log-odds contribution after shrinkage and whether it was discounted for sharing inputs. The contributions sum exactly to the log-odds distance from the base rate.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."ImpactId" IS 'Severity if the case is real. 1 Minimal, 2 Low, 3 Moderate, 4 High, 5 Severe. Never depends on the likelihood.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."ImpactComponentsJson" IS 'The components that produced the impact: typology severity, money band, exposure uplift and breadth uplift, each with its value and source.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."LaneId" IS 'Urgency lane, which orders cases before Priority does. 1 Live money, 2 Mandatory, 3 Deadline near, 4 Standard.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."LaneReason" IS 'Plain statement of why the case is in its lane, for example which diary or rule put it there.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."NextDeadlineDate" IS 'The next obligation the case must meet (UTC), such as a regulatory diary; null when there is none.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."DeadlineBreached" IS '1 when the next deadline has already passed, which keeps the case in the Deadline near lane; otherwise 0.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."ExpectedHarm" IS 'Likelihood multiplied by the weight of the impact band; the quantity Priority is scaled from.';
                        COMMENT ON COLUMN public."CaseRiskAssessment"."Priority" IS 'Expected harm scaled to 0 to 100 for display and sorting within a lane. Always shown with its likelihood, impact and lane.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
