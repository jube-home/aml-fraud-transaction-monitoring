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

    [Migration(20263108020000)]
    public class CreateCaseKeyJournalKeyIndexTable : Migration
    {
        public override void Up()
        {
            Create.Table("CaseKeyJournalKeyIndex")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("KeyName").AsString().NotNullable()
                .WithColumn("IndexName").AsString().NotNullable()
                .WithColumn("StateId").AsByte().NotNullable()
                .WithColumn("AttemptCount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Error").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().NotNullable()
                .WithColumn("UpdatedDate").AsDateTime2().NotNullable()
                .WithColumn("ReadyDate").AsDateTime2().Nullable();

            Create.Index("IX_CaseKeyJournalKeyIndex_KeyName").OnTable("CaseKeyJournalKeyIndex")
                .OnColumn("KeyName").Ascending().WithOptions().Unique();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseKeyJournalKeyIndex" IS
                        'The indexes on Archive that the jobs server has built, or tried to build, so that the Case Key Journal can find the transactions of one case key quickly. One row per payload field name. The index is on the model, the value of that payload field and the Archive identity, so that the newest transactions of a key are read in order and the read stops at the limit. The journal only filters on the field value where the index is ready, because without it the filter would scan the table.';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."Id" IS 'Server-assigned identity of the record.';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."KeyName" IS 'The payload field the index is on, such as AccountId, used in the index as the path payload then this name.';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."IndexName" IS 'The name of the index on Archive, derived from the key name.';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."StateId" IS '1 Building (being created, or abandoned part way), 2 Ready (valid and used by the journal), 3 Failed (the build did not succeed; see Error, retried on a later cycle).';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."AttemptCount" IS 'How many times a build has been started.';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."Error" IS 'Why the last build failed, null otherwise.';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."CreatedDate" IS 'When the first build was started (UTC).';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."UpdatedDate" IS 'When the state last changed (UTC).';
                        COMMENT ON COLUMN public."CaseKeyJournalKeyIndex"."ReadyDate" IS 'When the index became ready (UTC), null until it has.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
