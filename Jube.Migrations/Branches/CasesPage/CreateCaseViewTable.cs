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

    [Migration(20263107870000)]
    public class CreateCaseViewTable : Migration
    {
        public override void Up()
        {
            Create.Table("CaseView")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("User").AsString().NotNullable()
                .WithColumn("LastViewedDate").AsDateTime2().NotNullable()
                .WithColumn("LastViewedCaseEventId").AsInt32().Nullable()
                .WithColumn("LastViewedRiskSequence").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseView").ForeignColumn("CaseId")
                .ToTable("Case").PrimaryColumn("Id").OnDelete(System.Data.Rule.Cascade);

            Create.Index("IX_CaseView_CaseId_User").OnTable("CaseView")
                .OnColumn("CaseId").Ascending().OnColumn("User").Ascending().WithOptions().Unique();

            Create.Index("IX_CaseView_User").OnTable("CaseView").OnColumn("User");

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseView" IS
                        'The last time each user looked at each Case, and what they saw: the newest case event and the risk assessment in force. It is what lets the Cases page say what has changed on a case since the user last looked, which is how a case that has come back to someone reads as new work and not noise. One row per user and case; peeking or opening a case refreshes it.';
                        COMMENT ON COLUMN public."CaseView"."Id" IS 'Server-assigned identity of the view record.';
                        COMMENT ON COLUMN public."CaseView"."CaseId" IS 'The Case that was viewed; the record is removed with the case.';
                        COMMENT ON COLUMN public."CaseView"."User" IS 'The user who viewed the case.';
                        COMMENT ON COLUMN public."CaseView"."LastViewedDate" IS 'When the user last looked at the case (UTC).';
                        COMMENT ON COLUMN public."CaseView"."LastViewedCaseEventId" IS 'Identity of the newest case event that existed when the user last looked; events with a higher identity are what is new to them. Null when the case had no events.';
                        COMMENT ON COLUMN public."CaseView"."LastViewedRiskSequence" IS 'Sequence of the risk assessment in force when the user last looked; a higher current sequence means the risk has been recomputed since. Null when the case had no assessment.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
