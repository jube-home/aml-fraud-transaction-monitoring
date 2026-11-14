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

    [Migration(20263108210000)]
    public class GiveFilesTwoPossibleParents : Migration
    {
        public override void Up()
        {
            Alter.Table("CaseFile").AddColumn("CaseResearchId").AsInt32().Nullable();

            Execute.Sql("""
                        ALTER TABLE "CaseFile" ALTER COLUMN "CaseNoteId" DROP NOT NULL;
                        UPDATE "CaseFile" SET "CaseNoteId" = NULL WHERE "CaseNoteId" = 0;
                        ALTER TABLE "CaseFile" ADD CONSTRAINT "CK_CaseFile_ExactlyOneParent"
                            CHECK (num_nonnulls("CaseNoteId", "CaseResearchId") = 1) NOT VALID;
                        ALTER TABLE "CaseFile" VALIDATE CONSTRAINT "CK_CaseFile_ExactlyOneParent";
                        ALTER TABLE "CaseFile" ADD CONSTRAINT "FK_CaseFile_CaseNoteId_CaseNote_Id"
                            FOREIGN KEY ("CaseNoteId") REFERENCES "CaseNote" ("Id");
                        ALTER TABLE "CaseFile" ADD CONSTRAINT "FK_CaseFile_CaseResearchId_CaseResearch_Id"
                            FOREIGN KEY ("CaseResearchId") REFERENCES "CaseResearch" ("Id");
                        COMMENT ON COLUMN public."CaseFile"."CaseNoteId" IS
                        'The CaseNote the file is attached to. Exactly one of CaseNoteId and CaseResearchId is set, enforced by a check constraint.';
                        COMMENT ON COLUMN public."CaseFile"."CaseResearchId" IS
                        'The CaseResearch the file is attached to. Exactly one of CaseNoteId and CaseResearchId is set, enforced by a check constraint.';
                        """);

            Create.Index("IX_CaseFile_CaseResearchId").OnTable("CaseFile").OnColumn("CaseResearchId").Ascending();
        }

        public override void Down()
        {
        }
    }
}
