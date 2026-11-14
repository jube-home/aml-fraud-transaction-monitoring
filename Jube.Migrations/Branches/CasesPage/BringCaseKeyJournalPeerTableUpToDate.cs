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

    [Migration(20263108030000)]
    public class BringCaseKeyJournalPeerTableUpToDate : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        ALTER TABLE public."CaseKeyJournalPeer" ADD COLUMN IF NOT EXISTS "CaseKey" text NOT NULL DEFAULT '';
                        ALTER TABLE public."CaseKeyJournalPeer" ALTER COLUMN "CaseKey" DROP DEFAULT;
                        ALTER TABLE public."CaseKeyJournalPeer" ADD COLUMN IF NOT EXISTS "Deleted" smallint NOT NULL DEFAULT 0;
                        ALTER TABLE public."CaseKeyJournalPeer" ADD COLUMN IF NOT EXISTS "DeletedDate" timestamp without time zone NULL;

                        DROP INDEX IF EXISTS public."IX_CaseKeyJournalPeer_Workflow_Field_Segment";
                        CREATE UNIQUE INDEX IF NOT EXISTS "IX_CaseKeyJournalPeer_Workflow_Field_Segment"
                        ON public."CaseKeyJournalPeer" ("CaseWorkflowGuid", "CaseKey", "Field", COALESCE("Segment", ''));

                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."CaseKey" IS 'The payload field whose values are the case keys being compared, such as AccountId. A workflow can have several, and each is compared only with the others of its own kind.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Deleted" IS '1 when the last rebuild did not produce this distribution, for example because fewer than 20 keys now have the field; the journal ignores it. A later rebuild that produces it again sets this back to 0.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."DeletedDate" IS 'When the distribution was marked deleted (UTC), null while it is in use.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
