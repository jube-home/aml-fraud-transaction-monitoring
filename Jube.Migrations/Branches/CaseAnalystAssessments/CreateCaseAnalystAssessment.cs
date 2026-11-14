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
namespace Jube.Migrations.Branches.CaseAnalystAssessments
{
    using FluentMigrator;

    [Migration(2026111411000000)]
    public class CreateCaseAnalystAssessment : Migration
    {
        public override void Up()
        {
            Create.Table("CaseAnalystAssessment")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable().Unique()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("LikelihoodId").AsInt16().NotNullable()
                .WithColumn("Reason").AsString(1000).NotNullable()
                .WithColumn("ComputedSequence").AsInt32().Nullable()
                .WithColumn("ComputedLikelihoodProbability").AsDouble().Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("WithdrawnUser").AsString().Nullable()
                .WithColumn("WithdrawnDate").AsDateTime().Nullable();

            Create.Index("IX_CaseAnalystAssessment_Case").OnTable("CaseAnalystAssessment")
                .OnColumn("CaseId").Ascending().OnColumn("Id").Descending();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseAnalystAssessment" IS 'An analyst''s own view of how likely a case is to be confirmed, kept beside the computed likelihood. It never replaces the computed value and never feeds the ranking. Append only; the latest row of a case that is not withdrawn is current.';
                        COMMENT ON COLUMN public."CaseAnalystAssessment"."TenantRegistryId" IS 'The tenant the case belongs to.';
                        COMMENT ON COLUMN public."CaseAnalystAssessment"."CaseId" IS 'The case assessed.';
                        COMMENT ON COLUMN public."CaseAnalystAssessment"."LikelihoodId" IS 'Enumeration (CaseAnalystLikelihood): the analyst''s view. 1 Low, 2 Medium, 3 High.';
                        COMMENT ON COLUMN public."CaseAnalystAssessment"."Reason" IS 'Why the analyst holds this view.';
                        COMMENT ON COLUMN public."CaseAnalystAssessment"."ComputedSequence" IS 'The Sequence of the CaseRiskAssessment in force when the view was recorded. Empty when the case had not been assessed.';
                        COMMENT ON COLUMN public."CaseAnalystAssessment"."ComputedLikelihoodProbability" IS 'The computed likelihood in force when the view was recorded, 0 to 1, so the two can be compared later.';
                        COMMENT ON COLUMN public."CaseAnalystAssessment"."CreatedUser" IS 'The analyst who recorded the view.';
                        COMMENT ON COLUMN public."CaseAnalystAssessment"."WithdrawnUser" IS 'The analyst who withdrew the view; always its author.';
                        COMMENT ON COLUMN public."CaseAnalystAssessment"."WithdrawnDate" IS 'When the view was withdrawn. Empty while it stands.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
