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

namespace Jube.Migrations.Branches.CaseNotes
{
    using FluentMigrator;

    [Migration(20263108190000)]
    public class MoveCitationsToACaseNoteChildTable : Migration
    {
        public override void Up()
        {
            Create.Table("CaseNoteCitation")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseNoteId").AsInt32().NotNullable()
                .WithColumn("Title").AsString(300).Nullable()
                .WithColumn("Url").AsString(2000).NotNullable()
                .WithColumn("RetrievedDate").AsDateTime2().NotNullable()
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("CaseKeyValue").AsString().Nullable()
                .WithColumn("CaseId").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable();

            Create.Index("IX_CaseNoteCitation_CaseNoteId").OnTable("CaseNoteCitation").OnColumn("CaseNoteId")
                .Ascending();

            Create.Index("IX_CaseNoteCitation_CaseKey_CaseKeyValue").OnTable("CaseNoteCitation")
                .OnColumn("CaseKey").Ascending().OnColumn("CaseKeyValue").Ascending();

            Create.ForeignKey("FK_CaseNoteCitation_CaseId_Case_Id").FromTable("CaseNoteCitation").ForeignColumn("CaseId")
                .ToTable("Case").PrimaryColumn("Id");

            Execute.Sql("""
                        UPDATE "CaseNote"
                        SET "Note" = "Topic" || E'\n\n' || COALESCE("Note", '')
                        WHERE "Topic" IS NOT NULL AND "Topic" <> '';

                        INSERT INTO "CaseNoteCitation" ("CaseNoteId", "Title", "Url", "RetrievedDate", "CaseKey",
                                                        "CaseKeyValue", "CaseId", "CreatedDate", "CreatedUser", "Deleted")
                        SELECT n."Id", LEFT(e ->> 'title', 300), e ->> 'url',
                               COALESCE((e ->> 'retrieved')::timestamp, n."CreatedDate", now()), n."CaseKey",
                               n."CaseKeyValue", n."CaseId", n."CreatedDate", COALESCE(n."CreatedUser", 'Administrator'), 0
                        FROM "CaseNote" n, jsonb_array_elements(n."Citations"::jsonb) e
                        WHERE n."Citations" IS NOT NULL AND (e ->> 'url') ~* '^https?://' AND length(e ->> 'url') <= 2000;

                        UPDATE "CaseNote" SET "KindId" = 1 WHERE "KindId" = 3;
                        """);

            Delete.Column("Citations").FromTable("CaseNote");
            Delete.Column("Topic").FromTable("CaseNote");

            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseNote"."KindId" IS
                        'What the note is. 1 Note (a person''s note, which may carry sources and files; the chat agent also writes these, flagged AskJooby), 2 Narrative (the AskJooby case narrative, one row per version), 4 Document (carries uploaded files).';
                        COMMENT ON TABLE public."CaseNoteCitation" IS
                        'A source a case note relies on, such as a web page researched for the case. One note carries many. The address is stored as written and never fetched.';
                        COMMENT ON COLUMN public."CaseNoteCitation"."CaseNoteId" IS 'The CaseNote this source belongs to.';
                        COMMENT ON COLUMN public."CaseNoteCitation"."Title" IS 'Optional title of the source, up to 300 characters.';
                        COMMENT ON COLUMN public."CaseNoteCitation"."Url" IS 'The http or https address of the source, up to 2000 characters, no credentials. Never fetched by Jube.';
                        COMMENT ON COLUMN public."CaseNoteCitation"."RetrievedDate" IS 'When the source was retrieved. Defaults to the time it was added.';
                        COMMENT ON COLUMN public."CaseNoteCitation"."CaseKey" IS 'Copied from the note so sources are listed by Case Key and Value as notes are.';
                        COMMENT ON COLUMN public."CaseNoteCitation"."CaseKeyValue" IS 'Copied from the note.';
                        COMMENT ON COLUMN public."CaseNoteCitation"."CaseId" IS 'Copied from the note.';
                        COMMENT ON COLUMN public."CaseNoteCitation"."Deleted" IS 'Soft delete: 1 once the source has been removed.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
