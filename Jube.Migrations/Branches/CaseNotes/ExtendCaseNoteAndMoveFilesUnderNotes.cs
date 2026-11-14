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

    [Migration(20263108170000)]
    public class ExtendCaseNoteAndMoveFilesUnderNotes : Migration
    {
        public override void Up()
        {
            Alter.Table("CaseNote")
                .AddColumn("SourceId").AsInt32().NotNullable().WithDefaultValue(1)
                .AddColumn("KindId").AsInt32().NotNullable().WithDefaultValue(1)
                .AddColumn("StateId").AsInt32().NotNullable().WithDefaultValue(2)
                .AddColumn("IsFinding").AsBoolean().NotNullable().WithDefaultValue(false)
                .AddColumn("Version").AsInt32().NotNullable().WithDefaultValue(1)
                .AddColumn("SupersedesNoteId").AsInt32().Nullable()
                .AddColumn("FactsHash").AsString(64).Nullable()
                .AddColumn("Citations").AsString(int.MaxValue).Nullable()
                .AddColumn("Question").AsString(2000).Nullable()
                .AddColumn("AskJoobyConversationId").AsInt32().Nullable()
                .AddColumn("MigratedCaseFileId").AsInt32().Nullable();

            Alter.Table("CaseFile").AddColumn("CaseNoteId").AsInt32().Nullable();

            Alter.Table("CaseWorkflow").AddColumn("EnableNarrative").AsByte().Nullable();
            Alter.Table("CaseWorkflowVersion").AddColumn("EnableNarrative").AsByte().Nullable();

            Execute.Sql("""
                        INSERT INTO "CaseNote" ("Note", "CaseKey", "CaseKeyValue", "CaseId", "CreatedUser", "CreatedDate",
                                                "SourceId", "KindId", "StateId", "IsFinding", "Version", "MigratedCaseFileId")
                        SELECT 'Migrated Document', f."CaseKey", f."CaseKeyValue", f."CaseId",
                               COALESCE(NULLIF(f."CreatedUser", ''), 'Administrator'), f."CreatedDate",
                               2, 4, 2, FALSE, 1, f."Id"
                        FROM "CaseFile" f
                        ORDER BY f."Id";

                        UPDATE "CaseFile" f SET "CaseNoteId" = n."Id"
                        FROM "CaseNote" n WHERE n."MigratedCaseFileId" = f."Id";
                        """);

            Delete.Column("MigratedCaseFileId").FromTable("CaseNote");

            Execute.Sql("""ALTER TABLE "CaseFile" ALTER COLUMN "CaseNoteId" SET NOT NULL;""");

            Create.Index("IX_CaseFile_CaseNoteId").OnTable("CaseFile").OnColumn("CaseNoteId").Ascending();

            Create.Index("IX_CaseNote_CaseKey_CaseKeyValue_KindId").OnTable("CaseNote")
                .OnColumn("CaseKey").Ascending().OnColumn("CaseKeyValue").Ascending().OnColumn("KindId").Ascending();

            Execute.Sql("""
                        CREATE UNIQUE INDEX "UX_CaseNote_Narrative_CaseId_Version" ON "CaseNote" ("CaseId", "Version")
                        WHERE "KindId" = 2;

                        COMMENT ON COLUMN public."CaseNote"."SourceId" IS
                        'Who wrote the note. 1 User (a person, through the Decide note or an upload), 2 System (written by Jube itself, for example the note created for each migrated document), 3 AskJooby (the hourly narrative or a research question). Only the server sets 2 and 3, never a caller.';
                        COMMENT ON COLUMN public."CaseNote"."KindId" IS
                        'What the note is. 1 Note (ordinary analyst narrative), 2 Narrative (the AskJooby case narrative, one row per version), 3 Research (an AskJooby web research answer with citations), 4 Document (carries uploaded files).';
                        COMMENT ON COLUMN public."CaseNote"."StateId" IS
                        'Where the note is in its life. 1 Pending (a research question waiting for AskJooby), 2 Complete, 3 Failed (the research could not be answered). Notes written by a person are always 2.';
                        COMMENT ON COLUMN public."CaseNote"."IsFinding" IS
                        'True when the author marked their own note as a finding, which later feeds report narratives as an analyst-verified fact.';
                        COMMENT ON COLUMN public."CaseNote"."Version" IS
                        'Revision number within a Case for a Narrative note, starting at 1. Always 1 for any other kind. Unique per Case among Narrative notes.';
                        COMMENT ON COLUMN public."CaseNote"."SupersedesNoteId" IS
                        'The Id of the earlier Narrative note this revision replaces, forming the revision chain. Null for the first version and for every other kind.';
                        COMMENT ON COLUMN public."CaseNote"."FactsHash" IS
                        'SHA-256 of the case facts (activations, journal transactions, notes other than those written by AskJooby, and status) a Narrative note was written from. A new Narrative is written only when the current hash differs from this one.';
                        COMMENT ON COLUMN public."CaseNote"."Citations" IS
                        'JSON list of {title, url, retrieved} for the sources a Research note relied on. Null for every other kind.';
                        COMMENT ON COLUMN public."CaseNote"."Question" IS
                        'The question a user asked AskJooby to research. Null for every other kind.';
                        COMMENT ON COLUMN public."CaseNote"."AskJoobyConversationId" IS
                        'The AskJooby conversation that produced a Narrative or Research note, so the run behind it can be audited. Null for every other source.';
                        COMMENT ON COLUMN public."CaseFile"."CaseNoteId" IS
                        'The CaseNote the file is attached to. One note carries many files. Every file belongs to a note.';
                        COMMENT ON COLUMN public."CaseWorkflow"."EnableNarrative" IS
                        'When 1, the background job writes an AskJooby narrative note for each open Case in this workflow, about hourly, whenever the case facts have changed since the last one.';
                        COMMENT ON COLUMN public."CaseWorkflowVersion"."EnableNarrative" IS
                        'Version history copy of CaseWorkflow.EnableNarrative.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
