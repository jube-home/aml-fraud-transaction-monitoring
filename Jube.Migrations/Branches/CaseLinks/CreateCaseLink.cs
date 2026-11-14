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

namespace Jube.Migrations.Branches.CaseLinks
{
    using FluentMigrator;

    [Migration(2026111022000000)]
    public class CreateCaseLink : Migration
    {
        public override void Up()
        {
            Create.Table("CaseLink")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseIdLow").AsInt32().NotNullable()
                .WithColumn("CaseIdHigh").AsInt32().NotNullable()
                .WithColumn("Reason").AsString(500).Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Index("IX_CaseLink_Pair")
                .OnTable("CaseLink")
                .OnColumn("CaseIdLow").Ascending()
                .OnColumn("CaseIdHigh").Ascending()
                .WithOptions().Unique();

            Create.Index("IX_CaseLink_High").OnTable("CaseLink").OnColumn("CaseIdHigh").Ascending();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseLink" IS 'An analyst-made link saying two cases are related, for example two accounts of one scam. Symmetrical: stored once with the lower case id first.';
                        COMMENT ON COLUMN public."CaseLink"."CaseIdLow" IS 'The lower of the two linked case ids.';
                        COMMENT ON COLUMN public."CaseLink"."CaseIdHigh" IS 'The higher of the two linked case ids.';
                        COMMENT ON COLUMN public."CaseLink"."Reason" IS 'Optional free text on why the cases are linked.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
