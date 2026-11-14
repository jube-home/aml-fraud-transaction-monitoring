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

    [Migration(20263108200000)]
    public class MoveResearchToItsOwnSlice : Migration
    {
        public override void Up()
        {
            Create.Table("CaseResearch")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("CaseKeyValue").AsString().Nullable()
                .WithColumn("Topic").AsString(500).Nullable()
                .WithColumn("Findings").AsString(int.MaxValue).Nullable()
                .WithColumn("SourceId").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable();

            Create.ForeignKey("FK_CaseResearch_CaseId_Case_Id").FromTable("CaseResearch").ForeignColumn("CaseId")
                .ToTable("Case").PrimaryColumn("Id");

            Create.Index("IX_CaseResearch_CaseKey_CaseKeyValue").OnTable("CaseResearch")
                .OnColumn("CaseKey").Ascending().OnColumn("CaseKeyValue").Ascending();

            Create.Table("CaseResearchCitation")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseResearchId").AsInt32().NotNullable()
                .WithColumn("Title").AsString(300).Nullable()
                .WithColumn("Url").AsString(2000).NotNullable()
                .WithColumn("RetrievedDate").AsDateTime2().NotNullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable();

            Create.ForeignKey("FK_CaseResearchCitation_CaseResearchId_CaseResearch_Id").FromTable("CaseResearchCitation")
                .ForeignColumn("CaseResearchId").ToTable("CaseResearch").PrimaryColumn("Id");

            Create.Index("IX_CaseResearchCitation_CaseResearchId").OnTable("CaseResearchCitation")
                .OnColumn("CaseResearchId").Ascending();

            Execute.Sql("""
                        CREATE TEMP TABLE "ResearchMove" ON COMMIT DROP AS
                        SELECT n."Id" AS "NoteId", n."CaseId", n."CaseKey", n."CaseKeyValue", n."Note", n."SourceId",
                               n."CreatedUser", n."CreatedDate"
                        FROM "CaseNote" n
                        WHERE n."CaseId" IS NOT NULL
                          AND EXISTS (SELECT 1 FROM "CaseNoteCitation" c WHERE c."CaseNoteId" = n."Id")
                          AND NOT EXISTS (SELECT 1 FROM "CaseFile" f WHERE f."CaseNoteId" = n."Id");

                        ALTER TABLE "ResearchMove" ADD COLUMN "ResearchId" int;

                        WITH moved AS (
                            INSERT INTO "CaseResearch" ("CaseId", "CaseKey", "CaseKeyValue", "Topic", "Findings", "SourceId",
                                                        "CreatedDate", "CreatedUser")
                            SELECT m."CaseId", m."CaseKey", m."CaseKeyValue", LEFT(split_part(m."Note", E'\n\n', 1), 500),
                                   COALESCE(NULLIF(substr(m."Note", length(split_part(m."Note", E'\n\n', 1)) + 3), ''),
                                            m."Note"),
                                   CASE WHEN m."SourceId" = 3 THEN 2 ELSE 1 END, m."CreatedDate", m."CreatedUser"
                            FROM "ResearchMove" m ORDER BY m."NoteId"
                            RETURNING "Id", "CreatedDate", "CreatedUser", "CaseId", "Topic")
                        UPDATE "ResearchMove" m SET "ResearchId" = moved."Id"
                        FROM moved WHERE moved."CaseId" = m."CaseId" AND moved."CreatedDate" IS NOT DISTINCT FROM m."CreatedDate"
                          AND moved."CreatedUser" IS NOT DISTINCT FROM m."CreatedUser";

                        INSERT INTO "CaseResearchCitation" ("CaseResearchId", "Title", "Url", "RetrievedDate", "CreatedDate",
                                                           "CreatedUser")
                        SELECT m."ResearchId", c."Title", c."Url", c."RetrievedDate", c."CreatedDate", c."CreatedUser"
                        FROM "ResearchMove" m JOIN "CaseNoteCitation" c ON c."CaseNoteId" = m."NoteId"
                        WHERE m."ResearchId" IS NOT NULL AND c."Deleted" IS DISTINCT FROM 1;

                        DELETE FROM "CaseNote" WHERE "Id" IN (SELECT "NoteId" FROM "ResearchMove" WHERE "ResearchId" IS NOT NULL);
                        """);

            Delete.Table("CaseNoteCitation");

            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseNote"."KindId" IS
                        'What the note is. 1 Note (a person''s note, which may carry files; the chat agent also writes these, flagged AskJooby), 2 Narrative (the AskJooby case narrative, one row per version), 4 Document (carries uploaded files). Research is its own record, CaseResearch.';
                        COMMENT ON TABLE public."CaseResearch" IS
                        'Research captured on a case: a topic, what was found, and the sources the findings rely on. Append-only and rolled up to the case key and value like notes, but a record of its own, not a kind of note.';
                        COMMENT ON COLUMN public."CaseResearch"."CaseId" IS 'The case the research was captured on; tenant and role visibility follow the case.';
                        COMMENT ON COLUMN public."CaseResearch"."CaseKey" IS 'Copied from the case so research is listed by Case Key and Value.';
                        COMMENT ON COLUMN public."CaseResearch"."CaseKeyValue" IS 'Copied from the case.';
                        COMMENT ON COLUMN public."CaseResearch"."Topic" IS 'The topic or question researched, up to 500 characters.';
                        COMMENT ON COLUMN public."CaseResearch"."Findings" IS 'What the research found, sanitised like a note.';
                        COMMENT ON COLUMN public."CaseResearch"."SourceId" IS 'Who captured it. 1 User (a person, through the Decide Research lightbox), 2 AskJooby (the chat agent, for the calling user). Only the server sets it.';
                        COMMENT ON COLUMN public."CaseResearch"."CreatedUser" IS 'The user the research was captured by or, for AskJooby, for.';
                        COMMENT ON TABLE public."CaseResearchCitation" IS 'A source a research record relies on. One research has many. The address is stored as written and never fetched.';
                        COMMENT ON COLUMN public."CaseResearchCitation"."CaseResearchId" IS 'The research this source belongs to.';
                        COMMENT ON COLUMN public."CaseResearchCitation"."Title" IS 'Optional title of the source, up to 300 characters.';
                        COMMENT ON COLUMN public."CaseResearchCitation"."Url" IS 'The http or https address of the source, up to 2000 characters, no credentials. Never fetched by Jube.';
                        COMMENT ON COLUMN public."CaseResearchCitation"."RetrievedDate" IS 'When the source was retrieved. Defaults to the time it was captured.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
