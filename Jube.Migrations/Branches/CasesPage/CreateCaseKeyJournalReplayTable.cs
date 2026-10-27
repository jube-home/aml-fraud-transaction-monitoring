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

    [Migration(20263108060000)]
    public class CreateCaseKeyJournalReplayTable : Migration
    {
        public override void Up()
        {
            Create.Table("CaseKeyJournalReplay")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Days").AsInt32().NotNullable()
                .WithColumn("MergeJson").AsString().Nullable()
                .WithColumn("FilterJson").AsString().Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime2().NotNullable()
                .WithColumn("Deleted").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable();

            Create.ForeignKey().FromTable("CaseKeyJournalReplay").ForeignColumn("CaseId")
                .ToTable("Case").PrimaryColumn("Id").OnDelete(System.Data.Rule.Cascade);

            Create.Index("IX_CaseKeyJournalReplay_Guid").OnTable("CaseKeyJournalReplay")
                .OnColumn("Guid").Ascending().WithOptions().Unique();

            Create.Index("IX_CaseKeyJournalReplay_CaseId").OnTable("CaseKeyJournalReplay")
                .OnColumn("CaseId").Ascending();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseKeyJournalReplay" IS
                        'Replays an analyst saved on a Case: the settings needed to draw the movement of the case key on a map again, not the movement itself. The window, the merged keys and the column filter are stored, and the replay is recreated from them on demand, so it always reflects the archive as it is then. They are shared by everyone working the case and are available to the case narrative, which reads the movement they compute. Deleting sets Deleted rather than removing the row.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."Id" IS 'Server-assigned identity of the saved replay.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."Guid" IS 'Stable identifier of the saved replay used by the API and agents.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."CaseId" IS 'The Case the replay was saved on; replays are removed with the case.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."Name" IS 'The name the analyst gave the replay, such as "Card used in two countries".';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."Days" IS 'How many days back from now the replay looks: 7, 30, 90 or 365.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."MergeJson" IS 'The other keys drawn as trails of their own, as a JSON array of objects with a key and a value. Null when there are none.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."FilterJson" IS 'The column filter that narrows which transactions are drawn, as a JSON object holding the rule group and the kind of each column used. Null when there is no filter.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."CreatedUser" IS 'The user who saved the replay.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."CreatedDate" IS 'When the replay was saved (UTC).';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."Deleted" IS '1 once the replay has been deleted and is no longer offered.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."DeletedUser" IS 'The user who deleted the replay.';
                        COMMENT ON COLUMN public."CaseKeyJournalReplay"."DeletedDate" IS 'When the replay was deleted (UTC).';
                        """);
        }

        public override void Down()
        {
        }
    }
}
