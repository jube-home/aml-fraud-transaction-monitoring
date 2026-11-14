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

    [Migration(20263108180000)]
    public class SimplifyCaseNoteForHumanResearch : Migration
    {
        public override void Up()
        {
            Rename.Column("Question").OnTable("CaseNote").To("Topic");
            Delete.Column("StateId").FromTable("CaseNote");

            Execute.Sql("""
                        COMMENT ON COLUMN public."CaseNote"."KindId" IS
                        'What the note is. 1 Note (ordinary analyst narrative), 2 Narrative (the scheduled case summary, one row per version), 3 Research (a topic with findings and the sources they rely on, written by a person or, through the chat, by an agent for a person), 4 Document (carries uploaded files).';
                        COMMENT ON COLUMN public."CaseNote"."Topic" IS
                        'The topic or question a Research note covers. Null for every other kind.';
                        COMMENT ON COLUMN public."CaseNote"."AskJoobyConversationId" IS
                        'The recorded AskJooby run a Narrative note was generated from, so the run can be audited and later linked from chat history. Null for every other note.';
                        COMMENT ON COLUMN public."CaseNote"."Citations" IS
                        'JSON list of {title, url, retrieved} for the sources a Research note relies on, at most 10, http and https addresses only. Null for every other kind.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
